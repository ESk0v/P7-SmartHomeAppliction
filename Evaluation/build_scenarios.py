"""Build the ten household scenarios (base load, appliance cycles, EV sessions).

REFIT (2013-15) and the EV sessions (2019) are mapped onto 4 Oct 2025 - 31 Oct 2026
by ISO week number + weekday, on the local wall clock. Run once:

    python build_scenarios.py
"""
import json
from datetime import date, timedelta

import numpy as np
import pandas as pd

from common import (APPS, DATA, DT, HOUSES, N_STEPS, P_EV_MAX, PAIRING_SEED, SCENARIOS, TAIL, TZ,
                    WARMUP, step_times, to_step)

Q = pd.Timedelta("15min")
MIN_ACTIVE_MONTHS = 9          # EV users must be active in at least this many months of 2019


def target_days():
    """Local dates of all steps (warm-up + evaluation year), and the local wall time per step."""
    wall = step_times().tz_convert(TZ).tz_localize(None)
    return sorted(set(wall.date)), wall


# ------------------------------------------------------------------ REFIT
def load_house(h):
    """Base load (aggregate minus detected cycles) and the cycles with their 15-min profiles."""
    q = pd.read_csv(DATA / "refit" / "prepared" / f"house{h}_15min_kw.csv", index_col=0, parse_dates=True)
    cyc_power = pd.DataFrame(0.0, index=q.index, columns=APPS)
    cycles, dropped = [], 0
    for app in APPS:
        taken = pd.Series(False, index=q.index)
        c = pd.read_csv(DATA / "refit" / "prepared" / f"house{h}_{app}_cycles.csv", parse_dates=["start_utc", "end_utc"])
        for s, e in zip(c.start_utc, c.end_utc):
            slots = pd.date_range(s.floor("15min"), (e + pd.Timedelta("1min")).ceil("15min"), freq="15min", inclusive="left")
            slots = slots[slots.isin(q.index)]
            slots = slots[~taken[slots].values]          # a slot shared by two cycles goes to the first
            vals = q.loc[slots, app]
            if len(slots) == 0 or vals.isna().any() or (slots[-1] - slots[0]) != (len(slots) - 1) * Q:
                dropped += 1
                continue
            taken[slots] = True
            cyc_power.loc[slots, app] = vals.values
            cycles.append({"app": app, "start": slots[0], "profile": vals.round(4).tolist()})
    base = (q["Aggregate"] - cyc_power.sum(axis=1)).clip(lower=0)
    return base, cycles, dropped


def map_house(h, days, wall):
    base, cycles, dropped = load_house(h)
    local = base.index.tz_convert("Europe/London")
    per_day = base.notna().groupby(local.date).agg(["all", "size"])
    clean = set(per_day.index[per_day["all"] & (per_day["size"] >= 92)])

    def src(sy, w, wd):
        try:
            return date.fromisocalendar(sy, w, wd)
        except ValueError:
            return None

    # one source year per target ISO week: the one with most clean days, preferring the previous choice
    weeks = sorted({d.isocalendar()[:2] for d in days})
    year_of, prev = {}, None
    for (y, w) in weeks:
        counts = {sy: sum(src(sy, w, wd) in clean for wd in range(1, 8)) for sy in (2013, 2014, 2015)}
        best = max(counts.values())
        prev = prev if prev is not None and counts[prev] == best else min(sy for sy, c in counts.items() if c == best)
        year_of[(y, w)] = prev

    day_map, n_other_week, n_other_year = {}, 0, 0
    for d in days:
        y, w, wd = d.isocalendar()
        years = [year_of[(y, w)]] + [sy for sy in (2013, 2014, 2015) if sy != year_of[(y, w)]]
        found = None
        for shift in [0] + [s * k for k in range(1, 27) for s in (-1, 1)]:
            for sy in years:
                c = src(sy, w, wd)
                c = c + timedelta(days=7 * shift) if c else None
                if c in clean:
                    found = (c, shift != 0, sy != years[0])
                    break
            if found:
                break
        if not found:
            raise RuntimeError(f"house {h}: no clean source day for {d}")
        day_map[d] = found[0]
        n_other_week += found[1]
        n_other_year += found[2] and not found[1]

    # base load per target step, read at the same local wall-clock time on the source day
    offs = pd.to_timedelta([(day_map[d] - d).days for d in wall.date], unit="D")
    src_utc = (wall + offs).tz_localize("Europe/London", ambiguous=np.ones(len(wall), bool),
                                        nonexistent=pd.Timedelta(hours=-1)).tz_convert("UTC")
    target_base = base.reindex(src_utc).to_numpy()
    assert not np.isnan(target_base).any(), f"house {h}: gap in stitched base load"

    # cycles follow the source day they start on
    by_day = {}
    for c in cycles:
        by_day.setdefault(c["start"].tz_convert("Europe/London").date(), []).append(c)
    out = []
    for d in days:
        for c in by_day.get(day_map[d], []):
            w_src = c["start"].tz_convert("Europe/London").tz_localize(None)
            w_tgt = pd.Timestamp.combine(d, w_src.time())
            utc = w_tgt.tz_localize(TZ, ambiguous=True, nonexistent=pd.Timedelta(hours=1)).tz_convert("UTC")
            out.append({"app": c["app"], "ready": int(round(to_step(utc))), "profile": c["profile"]})
    out.sort(key=lambda c: c["ready"])

    typical, typical_n = {}, {}
    for app in APPS:
        mine = [c for c in out if c["app"] == app]
        hist = [c for c in mine if c["ready"] < 0]
        if len(hist) < 3:                                  # too few in the warm-up: take the first cycles
            hist = mine[:3]
        L = int(round(float(np.median([len(c["profile"]) for c in hist]))))
        mat = np.zeros((len(hist), L))
        for i, c in enumerate(hist):
            p = c["profile"][:L]
            mat[i, :len(p)] = p
        typical[app] = mat.mean(axis=0).round(4).tolist()
        typical_n[app] = len(hist)

    ev_cycles = [dict(c, id=i) for i, c in enumerate(c for c in out if 0 <= c["ready"] < N_STEPS - TAIL)]
    meta = {"house": h, "days_from_other_week": n_other_week, "days_from_other_year": n_other_year,
            "days_total": len(days), "source_cycles_dropped": dropped, "typical_from_n_cycles": typical_n,
            "cycles": {app: sum(c["app"] == app for c in ev_cycles) for app in APPS}}
    return target_base, ev_cycles, typical, meta


# ------------------------------------------------------------------ EV
def load_sessions():
    df = pd.read_csv(DATA / "ev" / "sessions.csv", sep=";", decimal=",")
    df = df[df["User_type"] == "Private"].copy()
    for c in ("Start_plugin", "End_plugout"):
        df[c] = pd.to_datetime(df[c], format="%d.%m.%Y %H:%M", errors="coerce")
    df["El_kWh"] = pd.to_numeric(df["El_kWh"], errors="coerce")
    df = df.dropna(subset=["Start_plugin", "End_plugout", "El_kWh"])
    loc = lambda s: s.dt.tz_localize("Europe/Oslo", ambiguous=True, nonexistent="shift_forward")
    df["duration"] = loc(df["End_plugout"]) - loc(df["Start_plugin"])
    iso = df["Start_plugin"].dt.isocalendar()
    return df[iso["year"] == 2019].assign(week=iso["week"], weekday=iso["day"])


# The ten EV users. Rule, fixed before looking at any savings: private users active in at least
# 9 months of 2019 (13 users), minus Bl2-6 (58 % of sessions over 24 h: a parked car), AdO3-4
# (32 sessions, a 69-day gap) and Bl2-1 (same garage and pattern as Bl2-2).
EV_USERS = ["Bl2-5", "Bl2-2", "AsO2-1", "AdO1-3", "AdO1-2", "Bl2-3", "AdO3-2", "AdO1-1", "AdO3-3", "Bl2-4"]


def choose_users(df):
    months = df.groupby("User_ID")["Start_plugin"].agg(lambda s: s.dt.month.nunique())
    assert all(months.get(u, 0) >= MIN_ACTIVE_MONTHS for u in EV_USERS)
    return EV_USERS, {u: int(months[u]) for u in EV_USERS}


def map_sessions(df, user):
    out = []
    for r in df[df["User_ID"] == user].itertuples():
        for ty, lo, hi in ((2025, date(2025, 11, 1), date(2025, 12, 28)), (2026, date(2025, 12, 29), date(2026, 10, 31))):
            try:
                d = date.fromisocalendar(ty, int(r.week), int(r.weekday))
            except ValueError:
                continue
            if not lo <= d <= hi:
                continue
            plug_utc = pd.Timestamp.combine(d, r.Start_plugin.time()).tz_localize(
                TZ, ambiguous=True, nonexistent=pd.Timedelta(hours=1)).tz_convert("UTC")
            plug = int(np.ceil(to_step(plug_utc) - 1e-9))
            dep = int(np.floor(to_step(plug_utc + r.duration) + 1e-9))
            if plug < 0 or dep <= plug or dep > N_STEPS:
                continue
            goal = round(min(r.El_kWh, P_EV_MAX * DT * (dep - plug)), 4)
            if goal > 0:
                out.append({"plug": plug, "dep": dep, "goal": goal})
    out.sort(key=lambda s: s["plug"])
    kept = []
    for s in out:                                         # overlapping sessions: keep the earlier one
        if not kept or s["plug"] >= kept[-1]["dep"]:
            kept.append(dict(s, id=len(kept)))
    return kept, len(out) - len(kept)


def main():
    SCENARIOS.mkdir(exist_ok=True)
    days, wall = target_days()
    ev = load_sessions()
    users, active = choose_users(ev)
    perm = np.random.default_rng(PAIRING_SEED).permutation(len(HOUSES))
    rows = []
    for i, h in enumerate(HOUSES):
        user = users[perm[i]]
        base, cycles, typical, meta = map_house(h, days, wall)
        sessions, overlap = map_sessions(ev, user)
        meta.update({"ev_user": user, "ev_active_months_2019": active[user], "sessions": len(sessions),
                     "sessions_dropped_overlap": overlap, "ev_kwh": round(sum(s["goal"] for s in sessions), 1),
                     "sessions_over_24h": sum(s["dep"] - s["plug"] > 96 for s in sessions)})
        scn = {"meta": meta, "base": np.round(base, 4).tolist(), "cycles": cycles, "typical": typical,
               "sessions": sessions}
        (SCENARIOS / f"hh{h:02d}.json").write_text(json.dumps(scn))
        rows.append(meta)
        print(meta, flush=True)
    t = pd.json_normalize(rows)
    t.to_csv(SCENARIOS / "households.csv", index=False)


main()
