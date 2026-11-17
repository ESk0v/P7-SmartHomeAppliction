"""Shared constants for the evaluation. Time is an integer step index:
step 0 = 1 Nov 2025 00:00 Danish time, one step = 15 minutes."""
from pathlib import Path

import pandas as pd

HERE = Path(__file__).resolve().parent
DATA = HERE / "data"
SCENARIOS = HERE / "scenarios"
RUNS = HERE / "runs"
RESULTS = HERE / "results"

TZ = "Europe/Copenhagen"
T0 = pd.Timestamp("2025-10-31 23:00", tz="UTC")     # step 0
N_STEPS = 365 * 96                                   # 1 Nov 2025 - 31 Oct 2026
WARMUP = 28 * 96                                     # 4 weeks of base load before step 0
HORIZON = 96                                         # 24 h
DT = 0.25                                            # h per step
P_EV_MAX = 7.2                                       # kW
TAIL = 3 * 96                                        # no new appliance cycles in the last 3 days

HOUSES = [1, 2, 3, 5, 6, 7, 10, 13, 16, 20]
APPS = ["washing_machine", "dishwasher"]
PAIRING_SEED = 0


def step_times(first=-WARMUP, last=N_STEPS):
    """UTC timestamps of steps first..last-1."""
    return pd.date_range(T0 + first * pd.Timedelta("15min"), periods=last - first, freq="15min")


def to_step(ts_utc):
    return (ts_utc - T0) / pd.Timedelta("15min")
