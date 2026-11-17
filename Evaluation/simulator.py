"""Step-based simulation of one household-year under one policy, plus cost accounting.

    from simulator import Config, simulate
    summary = simulate(Config(house=1, policy="mpc_sn"))

Policies: naive, night, mpc_sn (published prices + seasonal naive), mpc_k24 (actual prices 24 h ahead),
pf (perfect foresight: actual prices for the whole year; the benchmark for the possible saving).
The MPC calls Optimisation/optimiser.py::solve unchanged.
"""
import json
import sys
import time
from collections import deque
from dataclasses import asdict, dataclass

import numpy as np
import pandas as pd

from common import APPS, DT, HERE, HORIZON, N_STEPS, P_EV_MAX, RUNS, SCENARIOS, T0, TAIL, TZ, WARMUP, step_times, to_step
from prices import PriceOracle, base_forecast

sys.path.insert(0, str(HERE.parent / "Optimisation"))
from models import SolverLoad, SolverReq          # noqa: E402
from optimiser import InfeasibleError, solve      # noqa: E402

NOMINAL_FUSE = 15.0


@dataclass(frozen=True)
class Config:
    house: int
    policy: str                      # naive | night | mpc_sn | mpc_k24 | pf
    deadline_h: int = 8              # appliance deadline = ready + deadline_h
    fuse_kw: float | None = 15.0     # None = no fuse limit in the MPC
    profile: str = "actual"          # cycle profile the MPC plans with: actual (the app knows the
                                     # programme) | typical (one average profile per house and appliance)
    n_steps: int = N_STEPS           # shorter for tests

    @property
    def name(self):
        fuse = "none" if self.fuse_kw is None else f"{self.fuse_kw:g}"
        extra = ("_typical" if self.profile == "typical" else "") + (f"_n{self.n_steps}" if self.n_steps != N_STEPS else "")
        return f"hh{self.house:02d}_{self.policy}_d{self.deadline_h}_f{fuse}{extra}"


def load_scenario(house):
    return json.loads((SCENARIOS / f"hh{house:02d}.json").read_text())


def next_midnights():
    """nm[k] = first step >= k that is 00:00 local time."""
    local = step_times(0, N_STEPS + 2 * HORIZON).tz_convert(TZ)
    is_mid = (local.hour == 0) & (local.minute == 0)
    nm = np.full(len(local) + 1, len(local))
    for k in range(len(local) - 1, -1, -1):
        nm[k] = k if is_mid[k] else nm[k + 1]
    return nm


# ------------------------------------------------------------------ policies
# decide(k, new, ev, committed) -> ({cycle id: start step}, EV power in kW for step k)
#   new:       cycles that became ready at k: {id, app, plan (kW profile the policy may use), D, window}
#   ev:        None or {id, plug, dep, goal, E}
#   committed: [(start step, plan profile)] of cycles already started or advised
class Naive:
    def decide(self, k, new, ev, committed):
        return {c["id"]: k for c in new}, (P_EV_MAX if ev else 0.0)


class NightTimer:
    def __init__(self):
        self.nm = next_midnights()
        self.ev_start = {}

    def decide(self, k, new, ev, committed):
        starts = {}
        for c in new:
            m = int(self.nm[k])
            starts[c["id"]] = m if m + c["D"] <= k + c["window"] else k       # only if the deadline allows
        p = 0.0
        if ev:
            if ev["id"] not in self.ev_start:
                m = int(self.nm[ev["plug"]])
                ok = m < ev["dep"] and (ev["dep"] - m) * P_EV_MAX * DT >= ev["goal"] - 1e-9
                self.ev_start[ev["id"]] = m if ok else ev["plug"]
            p = P_EV_MAX if k >= self.ev_start[ev["id"]] else 0.0
        return starts, p


class PerfectForesight:
    """Benchmark: knows the actual prices for the whole year, and each session's departure and energy at plug-in.

    Each cycle starts at its cheapest allowed start, and each EV session charges at full power in the
    cheapest quarter-hours before departure. This is the cost-optimal schedule when the devices do not
    interact, i.e. when the fuse limit does not bind (it never binds at 15 kW). Fuse violations are still
    counted, so the assumption can be checked in the results.
    """

    def __init__(self, price):
        self.price = np.asarray(price)
        self.plan = {}                                   # EV session id -> {step: kW}

    def decide(self, k, new, ev, committed):
        starts = {}
        for c in new:
            prof = np.asarray(c["profile"])
            cand = np.arange(k, k + c["window"] - len(prof) + 1)
            cost = [float(self.price[t:t + len(prof)] @ prof) for t in cand]
            starts[c["id"]] = int(cand[int(np.argmin(cost))])      # ties: earliest start
        p = 0.0
        if ev:
            if ev["id"] not in self.plan:
                idx = np.arange(max(k, ev["plug"]), ev["dep"])
                need, plan = ev["goal"] - ev["E"], {}
                for t in idx[np.argsort(self.price[idx], kind="stable")]:
                    if need <= 1e-9:
                        break
                    e = min(P_EV_MAX * DT, need)
                    plan[int(t)] = e / DT
                    need -= e
                self.plan = {ev["id"]: plan}
            p = self.plan[ev["id"]].get(k, 0.0)
        return starts, p


class MPC:
    """Economic MPC with the production solver: one joint solve of appliances and EV per decision.
    The EV is sent with ReadyStep = plug-in (0, as it is plugged in) and DeadlineStep = departure; the
    solver enforces the goal at departure, or no goal when departure is beyond the 24 h horizon."""

    def __init__(self, cfg, prices, base_full):
        self.cfg, self.prices = cfg, prices
        self.mode = "k24" if cfg.policy == "mpc_k24" else "sn"
        self.fc = base_forecast(base_full)
        self.fuse = cfg.fuse_kw if cfg.fuse_kw is not None else 1e6
        self.solves = []             # (step, kind, seconds, status)

    def _solve(self, k, kind, prices, base, fixed, new=(), ev=None, fuse=None):
        loads = [SolverLoad(type="appliance", name=f"fixed{i}", power_profile=[x * DT for x in prof], fixed_start=s - k)
                 for i, (s, prof) in enumerate(fixed)]
        loads += [SolverLoad(type="appliance", name=str(c["id"]), power_profile=[x * DT for x in c["plan"]],
                             ready_step=0, deadline_step=c["window"]) for c in new]
        if ev:
            loads.append(SolverLoad(type="ev", name="ev", initial_energy=round(ev["E"], 6), energy_goal=ev["goal"],
                                    max_energy=ev["goal"], max_charging_power=P_EV_MAX,
                                    ready_step=0, deadline_step=ev["dep"] - k))
        req = SolverReq(start_time=(T0 + k * pd.Timedelta("15min")).to_pydatetime(), prices=[float(x) for x in prices],
                        base_load=[float(x) for x in base], fuse_limit=fuse or self.fuse, loads=loads)
        t = time.perf_counter()
        try:
            res = solve(req)
            self.solves.append((k, kind, time.perf_counter() - t, "Optimal"))
            return res
        except InfeasibleError:
            self.solves.append((k, kind, time.perf_counter() - t, "Infeasible"))
            return None

    def decide(self, k, new, ev, committed):
        need_ev = bool(ev) and ev["E"] < ev["goal"] - 1e-6
        if not new and not need_ev:
            return {}, 0.0
        c = self.prices.visible(k, self.mode)
        if need_ev and ev["dep"] - k > HORIZON and c.min() >= 0:
            need_ev = False          # no goal inside the horizon and no negative price: not charging is optimal
            if not new:
                return {}, 0.0
        used = np.zeros(HORIZON)                               # committed appliance load, kW
        for s, prof in committed:
            for i, x in enumerate(prof):
                if 0 <= s - k + i < HORIZON:
                    used[s - k + i] += x
        base = np.clip(np.minimum(self.fc[k:k + HORIZON], self.fuse - used - 1e-6), 0, None)   # keep it feasible
        kind = "MILP" if new else "LP"
        e = ev if need_ev else None
        res = (self._solve(k, kind, c, base, committed, new=new, ev=e)
               or self._solve(k, kind, c, base, committed, new=new, ev=e, fuse=1e6))
        starts = {int(a["name"]): k + a["start_step"] for a in res["appliances"] if not a["fixed"]}
        return starts, (float(res["evs"][0]["power_kw"][0]) if e else 0.0)


def make_policy(cfg, prices, base_full):
    if cfg.policy == "naive":
        return Naive()
    if cfg.policy == "night":
        return NightTimer()
    if cfg.policy == "pf":
        return PerfectForesight(prices.actual())
    if cfg.policy in ("mpc_sn", "mpc_k24"):
        return MPC(cfg, prices, base_full)
    raise ValueError(cfg.policy)


# ------------------------------------------------------------------ simulation
def simulate(cfg, prices=None, save=True, return_logs=False):
    scn = load_scenario(cfg.house)
    prices = prices or PriceOracle()
    base_full = np.asarray(scn["base"])
    policy = make_policy(cfg, prices, base_full)
    n, window = cfg.n_steps, cfg.deadline_h * 4
    price = prices.actual()
    power = {d: np.zeros(N_STEPS + 2 * HORIZON) for d in APPS + ["ev"]}

    queue = {app: deque(c for c in scn["cycles"] if c["app"] == app and c["ready"] < n - TAIL) for app in APPS}
    sessions = deque(s for s in scn["sessions"] if s["dep"] <= n)
    free = {app: 0 for app in APPS}                # first step the machine is free again
    committed, cyc_log, ses_log, cur = [], [], [], None

    for k in range(n):
        if cur and k >= cur["dep"]:
            ses_log.append(cur)
            cur = None
        if cur is None and sessions and sessions[0]["plug"] <= k:
            cur = dict(sessions.popleft(), E=0.0, cost=0.0)

        new = []
        for app in APPS:
            while queue[app] and max(queue[app][0]["ready"], free[app]) <= k:
                c = queue[app].popleft()
                plan = scn["typical"][app] if cfg.profile == "typical" else c["profile"]
                D = len(c["profile"])
                rec = {"id": c["id"], "app": app, "ready": c["ready"], "eff_ready": k, "D": D, "window": window,
                       "flexible": max(D, len(plan)) <= window, "plan": plan, "profile": c["profile"]}
                if rec["flexible"]:
                    new.append(rec)
                    break                          # the next cycle on this machine waits for this decision
                rec["start"] = k                   # does not fit the window: starts at once under every policy
                free[app] = k + D
                cyc_log.append(rec)

        ev_state = {x: cur[x] for x in ("id", "plug", "dep", "goal", "E")} if cur else None
        committed = [(s, p) for s, p in committed if s + len(p) > k]
        starts, p_ev = policy.decide(k, new, ev_state, committed)

        for rec in new:
            s = int(starts[rec["id"]])
            assert k <= s <= k + window, f"start {s} outside [{k}, {k + window}]"
            rec["start"] = s
            free[rec["app"]] = s + rec["D"]
            committed.append((s, rec["plan"]))
            cyc_log.append(rec)
        if cur:
            p = max(0.0, min(p_ev, P_EV_MAX, (cur["goal"] - cur["E"]) / DT))
            power["ev"][k] = p
            cur["E"] += p * DT
            cur["cost"] += price[k] * p * DT
    if cur:
        ses_log.append(cur)

    for rec in cyc_log:
        s, prof = rec["start"], np.asarray(rec.pop("profile"))
        rec.pop("plan")
        power[rec["app"]][s:s + len(prof)] += prof
        rec["kwh"] = float(prof.sum() * DT)
        rec["cost"] = float((price[s:s + len(prof)] * prof).sum() * DT)
        rec["late"] = bool(rec["flexible"] and s + rec["D"] > rec["eff_ready"] + window)

    summary = account(cfg, scn, price, base_full[WARMUP:], power, cyc_log, ses_log, getattr(policy, "solves", []))
    if save:
        out = RUNS / cfg.name
        out.mkdir(parents=True, exist_ok=True)
        pd.DataFrame(cyc_log).to_csv(out / "cycles.csv", index=False)
        pd.DataFrame(ses_log).to_csv(out / "sessions.csv", index=False)
        pd.DataFrame(getattr(policy, "solves", []), columns=["step", "kind", "seconds", "status"]).to_csv(out / "solves.csv", index=False)
        np.savez_compressed(out / "steps.npz", **{d: power[d][:n].astype("float32") for d in power})
        (out / "summary.json").write_text(json.dumps(summary, indent=1))     # written last: marks the run as done
    if return_logs:
        return summary, pd.DataFrame(cyc_log), pd.DataFrame(ses_log)
    return summary


# ------------------------------------------------------------------ accounting
def account(cfg, scn, price, base, power, cycles, sessions, solves):
    """All metrics, from the simulation log, actual prices and actual base load only."""
    n = cfg.n_steps
    month = step_times(0, n).tz_convert(TZ).strftime("%Y-%m")
    dev = {d: power[d][:N_STEPS] for d in power}
    out = {"config": asdict(cfg), "name": cfg.name, "ev_user": scn["meta"]["ev_user"]}
    out["kwh"] = {d: float(p.sum() * DT) for d, p in dev.items()}
    out["cost"] = {d: float((p * price).sum() * DT) for d, p in dev.items()}
    out["cost"]["total"] = sum(out["cost"].values())
    out["kwh"]["total"] = sum(out["kwh"].values())
    out["base_cost"] = float((base[:n] * price[:n]).sum() * DT)
    out["base_kwh"] = float(base[:n].sum() * DT)
    out["monthly_cost"] = {d: pd.Series(p[:n] * price[:n] * DT).groupby(month).sum().round(4).to_dict() for d, p in dev.items()}

    total = base[:n] + sum(p[:n] for p in dev.values())

    def over(limit):
        """Steps above the limit, split by cause: base load alone, or pushed over by EV/appliances."""
        b, t = base[:n] > limit + 1e-6, total > limit + 1e-6
        return {"base_alone": int(b.sum()), "caused_by_loads": int((t & ~b).sum())}

    out["fuse"] = {"max_kw": float(total.max()), "above_15kw": over(NOMINAL_FUSE),
                   "above_own_limit": over(cfg.fuse_kw) if cfg.fuse_kw else None}

    goal = sum(s["goal"] for s in sessions)
    unmet = [s["goal"] - s["E"] for s in sessions]
    long = [s for s in sessions if s["dep"] - s["plug"] > HORIZON]
    out["ev"] = {"sessions": len(sessions), "goal_kwh": goal, "unmet_kwh": float(sum(unmet)),
                 "sessions_unmet": int(sum(u > 0.01 for u in unmet)),
                 "sessions_over_24h": len(long), "kwh_over_24h": float(sum(s["E"] for s in long)),
                 "cost_over_24h": float(sum(s["cost"] for s in long)),
                 "cost_up_to_24h": float(sum(s["cost"] for s in sessions if s["dep"] - s["plug"] <= HORIZON)),
                 "kwh_up_to_24h": float(sum(s["E"] for s in sessions if s["dep"] - s["plug"] <= HORIZON))}
    flex = [c for c in cycles if c["flexible"]]
    out["cycles"] = {"total": len(cycles), "flexible": len(flex), "late": int(sum(c["late"] for c in cycles))}

    s = pd.DataFrame(solves, columns=["step", "kind", "seconds", "status"])
    out["solves"] = {"count": len(s), "infeasible": int((s["status"] != "Optimal").sum())}
    for kind, g in s.groupby("kind"):
        out["solves"][kind] = {"count": len(g), "median_s": float(g.seconds.median()),
                               "p95_s": float(g.seconds.quantile(0.95)), "max_s": float(g.seconds.max())}
    return out

