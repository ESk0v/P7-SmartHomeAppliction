"""Prices and base-load forecast as the MPC is allowed to see them.

PriceOracle.visible() is the only way a policy reads prices. At step k the day-ahead
prices are published until the end of today (before 13:00 local) or the end of tomorrow
(from 13:00). Unpublished steps get the price 96 steps (24 h) earlier: seasonal naive.
"""
import numpy as np
import pandas as pd

from common import DATA, HORIZON, N_STEPS, T0, TZ, WARMUP, step_times, to_step

LEAD = 96          # the price file starts one day before step 0


def load_prices():
    df = pd.read_csv(DATA / "prices_dk1.csv", parse_dates=["time_utc"])
    assert df["time_utc"].iloc[0] == T0 - pd.Timedelta("1D") and len(df) == N_STEPS + LEAD, "unexpected price file"
    assert (df["time_utc"].diff().dropna() == pd.Timedelta("15min")).all()
    return df["price_dkk_kwh"].to_numpy()


def publication_frontier():
    """frontier[k] = first step whose price is NOT yet published at step k."""
    local = step_times(0, N_STEPS).tz_convert(TZ)
    midnight = local.tz_localize(None).normalize()
    end = midnight + pd.to_timedelta(np.where(local.hour >= 13, 2, 1), unit="D")
    end_utc = end.tz_localize(TZ, ambiguous=True, nonexistent="shift_forward").tz_convert("UTC")
    return np.asarray(to_step(end_utc)).round().astype(int)


class PriceOracle:
    def __init__(self, series=None):
        self._p = load_prices() if series is None else np.asarray(series, float)
        self.frontier = publication_frontier()

    def actual(self, first=0, last=N_STEPS):
        """Actual prices; for cost accounting only."""
        return self._p[first + LEAD:last + LEAD]

    def visible(self, k, mode):
        """Prices for steps k..k+95 as known at step k. mode: 'sn' (published + seasonal naive)
        or 'k24' (actual prices for the whole horizon)."""
        j = np.arange(k, k + HORIZON)
        known_until = N_STEPS if mode == "k24" else min(self.frontier[k], N_STEPS)
        j = np.where(j >= known_until, j - 96, j)
        return self._p[j + LEAD]


def base_forecast(base_full):
    """Forecast for steps 0..N+95: max of the same weekday and quarter-hour in the previous 4 weeks.
    base_full starts WARMUP steps before step 0. The value for step j only uses steps <= j - 672,
    so any forecast for the next 24 h uses the past only."""
    j = np.arange(N_STEPS + HORIZON) + WARMUP
    return np.max([base_full[j - 672 * w] for w in (1, 2, 3, 4)], axis=0)
