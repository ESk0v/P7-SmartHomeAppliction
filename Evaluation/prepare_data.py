"""
Download and prepare the evaluation data for the HEMS project.

Run the steps in order (from any folder; data goes to Evaluation/data/):

    pip install pandas requests
    python Evaluation/prepare_data.py prices          # DK1 15-min day-ahead prices -> data/prices_dk1.csv
    python Evaluation/prepare_data.py refit-download  # checks the manually downloaded REFIT files
    python Evaluation/prepare_data.py refit-check     # quality check of 12 houses, picks 10 (or: refit-check <n> / refit-check select)
    python Evaluation/prepare_data.py ev-check        # after downloading the EV CSV manually (see below)

EV data (manual download, the site has no stable file links):
    https://data.mendeley.com/datasets/jbks2rcwyj  -> download Dataset 1 (EV charging reports, CSV)
    and save it as Evaluation/data/ev/sessions.csv
"""
import sys
from pathlib import Path

import pandas as pd
import requests

DATA = Path(__file__).resolve().parent / "data"   # Evaluation/data, ignored by git
HEADERS = {"User-Agent": "aau-sw7-hems-evaluation/1.0"}

# ------------------------------------------------------------------ REFIT channel map
# From the NILMTK REFIT metadata (github.com/nilmtk/nilmtk, dataset_converters/refit/metadata).
# Column "ApplianceN" in House_X.csv, checked against the official REFIT ReadMe.
# Excluded: 9 and 18 (washer-dryer as well as a washing machine, so laundry is split);
# 4, 8, 12, 17, 19 (no dishwasher); there is no house 14. House 1 has a separate tumble dryer (fine).
# Data: REFIT "Processed_Data_CSV" (Strathclyde portal, CC BY 4.0, downloaded manually). Its values
# are identical to the cleaned version, which only adds an "Issues" column (checked for houses 1-3, 5, 11).
REFIT_CHANNELS = {
    1:  {"washing_machine": "Appliance5", "dishwasher": "Appliance6"},
    2:  {"washing_machine": "Appliance2", "dishwasher": "Appliance3"},
    3:  {"washing_machine": "Appliance6", "dishwasher": "Appliance5"},
    5:  {"washing_machine": "Appliance3", "dishwasher": "Appliance4"},
    6:  {"washing_machine": "Appliance2", "dishwasher": "Appliance3"},
    7:  {"washing_machine": "Appliance5", "dishwasher": "Appliance6"},
    10: {"washing_machine": "Appliance5", "dishwasher": "Appliance6"},
    11: {"washing_machine": "Appliance3", "dishwasher": "Appliance4"},
    13: {"washing_machine": "Appliance3", "dishwasher": "Appliance4"},
    15: {"washing_machine": "Appliance3", "dishwasher": "Appliance4"},
    16: {"washing_machine": "Appliance5", "dishwasher": "Appliance6"},
    20: {"washing_machine": "Appliance4", "dishwasher": "Appliance5"},
    21: {"washing_machine": "Appliance3", "dishwasher": "Appliance4"},
}
# Aggregate includes solar generation (REFIT ReadMe): base load would contain PV, so not used.
SOLAR_HOUSES = {11, 21}
REFIT_DIR = DATA / "refit" / "raw" / "Processed_Data_CSV"

ON_W = 10            # appliance counts as running above this power
GAP_MIN = 10         # pauses shorter than this (minutes) belong to the same cycle
MIN_CYCLE_MIN = 15   # shorter runs are ignored (standby blips)
MIN_PEAK_W = 500     # a real wash/dishwasher cycle heats water (~2 kW); flat low-power runs are standby
MIN_KWH = 0.1        # ... and uses at least this much energy


# ------------------------------------------------------------------ prices
def prices():
    """DK1 15-min day-ahead prices, 31 Oct 2025 - 1 Nov 2026 (one extra day for the seasonal naive)."""
    url = "https://api.energidataservice.dk/dataset/DayAheadPrices"
    params = {"start": "2025-10-31T00:00", "end": "2026-11-01T00:00",
              "filter": '{"PriceArea":["DK1"]}', "sort": "TimeUTC asc", "limit": 0}
    r = requests.get(url, params=params, headers=HEADERS, timeout=120)
    r.raise_for_status()
    df = pd.DataFrame(r.json()["records"])
    print("Columns returned:", list(df.columns))   # check the names below match
    df = df.rename(columns={"TimeUTC": "time_utc", "DayAheadPriceDKK": "price_dkk_mwh"})
    df["time_utc"] = pd.to_datetime(df["time_utc"], utc=True)
    df["price_dkk_kwh"] = df["price_dkk_mwh"] / 1000
    df = df[["time_utc", "price_dkk_kwh"]].sort_values("time_utc")
    steps = df["time_utc"].diff().dropna().value_counts()
    print(f"{len(df)} rows, {df.time_utc.min()} -> {df.time_utc.max()}")
    print("Step sizes:", dict(steps.head(3)))       # expect 15 min
    print(f"Max price: {df.price_dkk_kwh.max():.2f} DKK/kWh (lambda = 100 must exceed this)")
    DATA.mkdir(exist_ok=True)
    df.to_csv(DATA / "prices_dk1.csv", index=False)


# ------------------------------------------------------------------ REFIT
def refit_download():
    """The REFIT portal blocks scripted downloads; this only checks the manually downloaded files."""
    missing = [n for n in REFIT_CHANNELS if not refit_path(n).exists()]
    if missing:
        sys.exit(f"Missing REFIT files for houses {missing}. Download Processed_Data_CSV.7z from\n"
                 "https://pureportal.strath.ac.uk/en/datasets/refit-electrical-load-measurements-cleaned/\n"
                 f"and extract it to {REFIT_DIR}")
    print("All candidate houses present.")


def refit_path(n):
    return REFIT_DIR / f"House_{n}.csv"


def load_house(n):
    cols = ["Unix", "Aggregate"] + list(REFIT_CHANNELS[n].values())
    df = pd.read_csv(refit_path(n), usecols=cols)
    df.index = pd.to_datetime(df.pop("Unix"), unit="s", utc=True)
    df.index.name = "time_utc"
    return df.rename(columns={v: k for k, v in REFIT_CHANNELS[n].items()})


def detect_cycles(power_1min):
    """Return a list of (start, end) for appliance cycles in a 1-min power series (W)."""
    on = power_1min > ON_W
    cycles, start, last_on = [], None, None
    for ts, is_on in on.items():
        if is_on:
            if start is None:
                start = ts
            elif (ts - last_on).total_seconds() / 60 > GAP_MIN:
                cycles.append((start, last_on))
                start = ts
            last_on = ts
    if start is not None:
        cycles.append((start, last_on))
    return [(s, e) for s, e in cycles if (e - s).total_seconds() / 60 + 1 >= MIN_CYCLE_MIN]


def refit_check_house(n):
    """Quality stats, cycles and 15-min file for one house (about a minute each)."""
    import json
    out = DATA / "refit" / "prepared"
    out.mkdir(parents=True, exist_ok=True)
    print(f"House {n}: reading ...", flush=True)
    df = load_house(n)
    minute = df.resample("1min").mean()
    days = minute["Aggregate"].resample("1D").count()
    good_days = int((days >= 0.9 * 1440).sum())                 # days with >= 90% coverage
    zero_share = float((minute["Aggregate"] <= 0).mean())
    stats = {"house": n, "good_days": good_days, "aggregate_zero_share": round(zero_share, 3),
             "first_day": str(minute.index.min().date()), "last_day": str(minute.index.max().date())}
    for app in ("washing_machine", "dishwasher"):
        p_app = minute[app].fillna(0)
        cyc = [(s, e) for s, e in detect_cycles(p_app)
               if p_app[s:e].max() >= MIN_PEAK_W and p_app[s:e].sum() / 60 / 1000 >= MIN_KWH]
        lengths = pd.Series([(e - s).total_seconds() / 60 + 1 for s, e in cyc])
        energy = [minute[app][s:e].sum() / 60 / 1000 for s, e in cyc]   # kWh per cycle
        stats[f"{app}_cycles"] = len(cyc)
        stats[f"{app}_per_week"] = round(len(cyc) / max(good_days, 1) * 7, 2)
        stats[f"{app}_median_min"] = int(lengths.median()) if cyc else 0
        stats[f"{app}_median_kwh"] = round(float(pd.Series(energy).median()), 2) if cyc else 0
        pd.DataFrame(cyc, columns=["start_utc", "end_utc"]).to_csv(out / f"house{n}_{app}_cycles.csv", index=False)
    q = minute.resample("15min").mean() / 1000
    q.index.name = "time_utc"
    q.to_csv(out / f"house{n}_15min_kw.csv")
    (out / f"house{n}_stats.json").write_text(json.dumps(stats, indent=1))
    print("   ", stats)


def refit_check(house=None):
    """refit-check            -> all houses (slow), then select
       refit-check <n>        -> one house
       refit-check select     -> combine existing per-house stats and pick 10"""
    import json
    if house not in (None, "select"):
        return refit_check_house(int(house))
    if house is None:
        for n in REFIT_CHANNELS:
            refit_check_house(n)
    out = DATA / "refit" / "prepared"
    rows = [json.loads((out / f"house{n}_stats.json").read_text())
            for n in REFIT_CHANNELS if (out / f"house{n}_stats.json").exists()]
    t = pd.DataFrame(rows)
    # Houses must use both appliances at least once a week on average, or they add little to scheduling
    t["eligible"] = ((t["washing_machine_per_week"] >= 1) & (t["dishwasher_per_week"] >= 1)
                     & ~t["house"].isin(SOLAR_HOUSES))
    # Score: plenty of clean days and regular use of both appliances
    t["score"] = (t["good_days"].rank() + t["washing_machine_per_week"].rank()
                  + t["dishwasher_per_week"].rank() - t["aggregate_zero_share"].rank())
    t = t.sort_values(["eligible", "score"], ascending=False)
    t["selected"] = False
    t.iloc[:10, t.columns.get_loc("selected")] = True
    t.to_csv(DATA / "refit" / "house_quality.csv", index=False)
    print("\n", t.to_string(index=False))
    print("\nSelected houses:", sorted(t[t.selected].house.tolist()))


# ------------------------------------------------------------------ EV
def ev_check():
    path = DATA / "ev" / "sessions.csv"
    df = pd.read_csv(path, sep=None, engine="python", decimal=",")
    print("Columns:", list(df.columns))
    for c in ("Start_plugin", "End_plugout"):
        df[c] = pd.to_datetime(df[c], dayfirst=True)
    df["El_kWh"] = pd.to_numeric(df["El_kWh"], errors="coerce")
    df["hours"] = (df["End_plugout"] - df["Start_plugin"]).dt.total_seconds() / 3600
    df["deliverable_kwh"] = 7.2 * df["hours"]
    df["capped"] = df["El_kWh"] > df["deliverable_kwh"]
    print(f"{len(df)} sessions, {df.User_ID.nunique()} users")
    print(f"Sessions longer than 24 h: {(df.hours > 24).mean():.1%}")
    print(f"Sessions capped at 7.2 kW: {df.capped.sum()} "
          f"({(df.El_kWh - df.deliverable_kwh).clip(lower=0).sum() / df.El_kWh.sum():.2%} of the energy)")

    # Only private chargers: "Shared" chargers are used by several people, so not one household's car
    private = df[df["User_type"] == "Private"]
    print(f"Private users: {private.User_ID.nunique()} ({len(private)} sessions); shared excluded")
    per_user = private.groupby("User_ID").size().sort_values(ascending=False)
    top, bottom = per_user.iloc[: len(per_user) // 2], per_user.iloc[len(per_user) // 2:]
    # 5 from each half, spread evenly; users with fewer than 20 sessions are skipped
    if (bottom >= 20).sum() >= 5:
        bottom = bottom[bottom >= 20]

    def pick(s, k=5):
        if len(s) <= k:
            return s
        return s.iloc[sorted({round(i * (len(s) - 1) / (k - 1)) for i in range(k)})]
    chosen = pd.concat([pick(top), pick(bottom)])
    print("\nChosen users (sessions):")
    print(chosen.to_string())
    (DATA / "ev").mkdir(exist_ok=True)
    chosen.rename("sessions").to_csv(DATA / "ev" / "chosen_users.csv")


steps = {"prices": prices, "refit-download": refit_download,
         "refit-check": refit_check, "ev-check": ev_check}
if len(sys.argv) < 2 or sys.argv[1] not in steps:
    print(__doc__)
    sys.exit(1)
steps[sys.argv[1]](*sys.argv[2:])
