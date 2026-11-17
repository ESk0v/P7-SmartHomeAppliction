"""Checks that the evaluation is honest: no leakage, equal energy, sane optimiser behaviour.

    python -m pytest tests -q
"""
import sys
from pathlib import Path

import numpy as np
import pandas as pd
import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from common import HORIZON, N_STEPS, TZ, WARMUP, step_times          # noqa: E402
from prices import PriceOracle, base_forecast, load_prices          # noqa: E402
from simulator import Config, load_scenario, simulate               # noqa: E402

DAYS = 28
RNG = np.random.default_rng(1)
STEPS = list(RNG.integers(0, N_STEPS, 300)) + [0, 51, 52, N_STEPS - 1]



@pytest.fixture(scope="module")
def logs():
    """Cycle and session logs for house 3, no fuse, MPC planning with the actual profile."""
    out = {}
    for p in ("naive", "night", "mpc_sn", "mpc_k24", "pf"):
        cfg = Config(3, p, fuse_kw=None, profile="actual", n_steps=DAYS * 96)
        out[p] = simulate(cfg, save=False, return_logs=True)
    return out


def test_frontier_follows_1300_rule():
    o = PriceOracle()
    local = step_times(0, N_STEPS).tz_convert(TZ)
    for k in STEPS:
        end = step_times(o.frontier[k], o.frontier[k] + 1).tz_convert(TZ)[0]
        assert end.hour == 0 and end.minute == 0
        days_ahead = (end.tz_localize(None).normalize() - local[k].tz_localize(None).normalize()).days
        assert days_ahead == (2 if local[k].hour >= 13 else 1)


def test_no_price_leak():
    """Changing every unpublished price must not change what MPC-SN sees."""
    p = load_prices()
    o = PriceOracle(p)
    for k in STEPS:
        q = p.copy()
        cut = min(o.frontier[k] + 96, len(q))       # index of the first unpublished price
        q[cut:] = RNG.uniform(-5, 50, len(q) - cut)
        assert np.array_equal(o.visible(k, "sn"), PriceOracle(q).visible(k, "sn"))


def test_k24_sees_actual_prices():
    o = PriceOracle()
    for k in [x for x in STEPS if x < N_STEPS - HORIZON]:
        assert np.array_equal(o.visible(k, "k24"), o.actual(k, k + HORIZON))


def test_no_base_load_leak():
    """Changing base load from step k onwards must not change the forecast for k..k+95."""
    base = np.asarray(load_scenario(1)["base"])
    fc = base_forecast(base)
    for k in STEPS:
        b = base.copy()
        b[WARMUP + k:] = RNG.uniform(0, 20, len(b) - WARMUP - k)
        assert np.array_equal(fc[k:k + HORIZON], base_forecast(b)[k:k + HORIZON])


def test_equal_energy_across_policies(logs):
    ref = logs["naive"][0]
    for p, (s, _, _) in logs.items():
        for app in ("washing_machine", "dishwasher"):
            assert s["kwh"][app] == pytest.approx(ref["kwh"][app], abs=1e-6), p
        assert s["kwh"]["ev"] + s["ev"]["unmet_kwh"] == pytest.approx(s["ev"]["goal_kwh"], abs=1e-6), p
        assert s["ev"]["goal_kwh"] == pytest.approx(ref["ev"]["goal_kwh"]), p


def test_k24_never_worse_than_baselines(logs):
    """With actual prices, no fuse and the actual profile, the optimiser must match or beat both rules."""
    _, cyc, ses = logs["mpc_k24"]
    for b in ("naive", "night"):
        _, bc, bs = logs[b]
        m = ses.merge(bs, on="id", suffixes=("", "_b"))
        m = m[(m.dep - m.plug <= HORIZON) & (m.E > m.goal - 1e-3) & (m.E_b > m.goal_b - 1e-3)]
        assert len(m) > 5 and (m.cost <= m.cost_b + 1e-3).all(), b
        c = cyc.merge(bc, on="id", suffixes=("", "_b"))
        c = c[c.flexible & (c.eff_ready == c.eff_ready_b)]
        assert len(c) > 5 and (c.cost <= c.cost_b + 1e-6).all(), b


def test_forecast_does_not_change_appliance_starts_at_8h(logs):
    a, b = logs["mpc_sn"][1], logs["mpc_k24"][1]
    assert (a.start.values == b.start.values).all()


def test_night_timer_respects_deadline(logs):
    c = logs["night"][1]
    assert not c.late.any()
    assert (c.start + c.D <= c.eff_ready + c.window)[c.flexible].all()


def test_solver_ev_departure():
    """Solver EV model: ReadyStep = plug-in, DeadlineStep = departure. No charging outside, goal at departure,
    no goal when departure is beyond the horizon."""
    from simulator import SolverLoad, SolverReq, solve
    prices = [2.0] * 8 + [0.1] * 8                       # cheap only after departure
    ev = dict(type="ev", name="ev", initial_energy=0, energy_goal=3.6, max_energy=10, max_charging_power=7.2)
    req = lambda **kw: SolverReq(start_time="2026-01-01T00:00:00", prices=prices, base_load=[0.0] * 16, fuse_limit=15,
                                 loads=[SolverLoad(**ev, **kw)])
    r = solve(req(deadline_step=8))["evs"][0]
    assert sum(r["power_kw"][8:]) == 0 and r["energy_kwh"][8] == pytest.approx(3.6) and r["missing_kwh"] == 0
    r = solve(req(ready_step=6, deadline_step=8))["evs"][0]
    assert sum(r["power_kw"][:6]) == 0 and r["energy_kwh"][8] == pytest.approx(3.6)
    assert sum(solve(req(deadline_step=40))["evs"][0]["power_kw"]) == 0           # leaves after the horizon: no goal
    assert solve(req())["evs"][0]["final_energy_kwh"] == pytest.approx(3.6)       # no window: the whole horizon


def test_perfect_foresight_is_cheapest(logs):
    """Perfect foresight must cost no more than any policy, per cycle, per complete session and in total."""
    _, cyc, ses = logs["pf"]
    for p in ("naive", "night", "mpc_sn", "mpc_k24"):
        s, bc, bs = logs[p]
        assert logs["pf"][0]["cost"]["total"] <= s["cost"]["total"] + 1e-6, p
        m = ses.merge(bs, on="id", suffixes=("", "_b"))
        m = m[(m.E > m.goal - 1e-3) & (m.E_b > m.goal_b - 1e-3)]
        assert len(m) > 5 and (m.cost <= m.cost_b + 1e-6).all(), p
        c = cyc.merge(bc, on="id", suffixes=("", "_b"))
        c = c[c.flexible & (c.eff_ready == c.eff_ready_b)]
        assert len(c) > 5 and (c.cost <= c.cost_b + 1e-6).all(), p
