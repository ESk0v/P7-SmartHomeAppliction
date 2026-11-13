from datetime import timedelta
from pulp import LpProblem, LpMinimize, HiGHS, lpSum, value

from models import SolverReq

DT = 0.25        # h per step
ETA = 1.0        # charging efficiency
LAM = 10.0       # DKK per missing kWh at departure


class InfeasibleError(Exception):
    pass


def p_at(p, j):
    return p[j] if 0 <= j < len(p) else 0


def solve(req: SolverReq) -> dict:
    N = len(req.prices)
    T = range(N)
    c = req.prices
    P_base = req.base_load
    P_fuse = req.fuse_limit

    if N == 0:
        raise ValueError("Prices is empty")
    if len(P_base) != N:
        raise ValueError(f"BaseLoad has {len(P_base)} steps, Prices has {N}")

    appliances = [l for l in req.loads if l.type == "appliance"]
    evs = [l for l in req.loads if l.type == "ev"]

    prob = LpProblem("HEMS", LpMinimize)

    # ---- appliances
    s, P_app, profiles = {}, {}, {}
    for i, a in enumerate(appliances):
        if not a.power_profile:
            raise ValueError(f"Appliance '{a.name}' has no PowerProfile")
        p = [x / DT for x in a.power_profile]        # kWh per step -> kW
        profiles[i] = p

        if a.fixed_start is not None:
            P_app[i] = {t: p_at(p, t - a.fixed_start) for t in T}
        else:
            if a.ready_step is None or a.deadline_step is None:
                raise ValueError(f"Appliance '{a.name}' needs ReadyStep+DeadlineStep or FixedStart")
            ready = max(0, a.ready_step)
            deadline = min(N, a.deadline_step)
            S = range(ready, deadline - len(p) + 1)
            if len(S) == 0:
                raise InfeasibleError(f"'{a.name}' does not fit inside its time window")
            s[i] = {tau: prob.add_variable(f"s_{i}_{tau}", cat="Binary") for tau in S}
            prob += lpSum(s[i].values()) == 1
            P_app[i] = {t: lpSum(p_at(p, t - tau) * s[i][tau] for tau in S) for t in T}

    # ---- EVs (plugged in from step 0, leaves at the end of the horizon)
    P_ev, E, u = {}, {}, {}
    for j, ev in enumerate(evs):
        needed = (ev.initial_energy, ev.energy_goal, ev.max_energy, ev.max_charging_power)
        if any(x is None for x in needed):
            raise ValueError(f"EV '{ev.name}' is missing energy/power fields")
        P_ev[j] = {t: prob.add_variable(f"P_ev_{j}_{t}", 0, ev.max_charging_power) for t in T}
        E[j] = {t: prob.add_variable(f"E_{j}_{t}", 0) for t in range(N + 1)}
        u[j] = prob.add_variable(f"u_{j}", 0)

        prob += E[j][0] == ev.initial_energy
        for t in T:
            prob += E[j][t + 1] == E[j][t] + ETA * P_ev[j][t] * DT
            prob += E[j][t + 1] <= ev.max_energy
        prob += E[j][N] + u[j] >= ev.energy_goal

    # ---- grid, objective, fuse
    P_grid = {
        t: P_base[t]
        + lpSum(P_app[i][t] for i in P_app)
        + lpSum(P_ev[j][t] for j in P_ev)
        for t in T
    }
    prob += lpSum(c[t] * P_grid[t] * DT for t in T) + LAM * lpSum(u.values())
    for t in T:
        prob += P_grid[t] <= P_fuse

    status = prob.solve(HiGHS(msg=False))
    if status.status_str != "Optimal":
        raise InfeasibleError(f"Solver status: {status.status_str}")

    # ---- build result
    start = req.start_time

    def ts(step):
        return (start + timedelta(minutes=15 * step)).isoformat()

    def cost_of(power):
        return sum(c[t] * value(power[t]) * DT for t in T)

    app_out = []
    for i, a in enumerate(appliances):
        if i in s:
            st = next(tau for tau in s[i] if value(s[i][tau]) > 0.5)
            fixed = False
        else:
            st, fixed = a.fixed_start, True
        app_out.append({
            "name": a.name,
            "fixed": fixed,
            "start_step": st,
            "end_step": st + len(profiles[i]),
            "start_time": ts(st),
            "end_time": ts(st + len(profiles[i])),
            "cost": round(cost_of(P_app[i]), 4),
        })

    ev_out = []
    for j, ev in enumerate(evs):
        final = value(E[j][N])
        ev_out.append({
            "name": ev.name,
            "power_kw": [round(value(P_ev[j][t]), 4) for t in T],
            "energy_kwh": [round(value(E[j][t]), 4) for t in range(N + 1)],
            "final_energy_kwh": round(final, 4),
            "missing_kwh": round(value(u[j]), 4),
            "cost": round(cost_of(P_ev[j]), 4),
        })

    grid = [value(P_grid[t]) for t in T]
    peak = max(T, key=lambda t: grid[t])
    base_cost = sum(c[t] * P_base[t] * DT for t in T)
    load_cost = sum(x["cost"] for x in app_out) + sum(x["cost"] for x in ev_out)

    return {
        "status": status.status_str,
        "start_time": start.isoformat(),
        "steps": N,
        "total_cost": round(load_cost + base_cost, 4),
        "load_cost": round(load_cost, 4),
        "base_cost": round(base_cost, 4),
        "peak_grid_kw": round(grid[peak], 3),
        "peak_step": peak,
        "appliances": app_out,
        "evs": ev_out,
        "grid_kw": [round(g, 4) for g in grid],
    }