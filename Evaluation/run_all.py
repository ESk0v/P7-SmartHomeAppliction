"""Run the whole experiment matrix. Finished runs are skipped, so it can be stopped and restarted.

    python run_all.py              # everything
    python run_all.py --force      # recompute
"""
import argparse
import time
from multiprocessing import Pool, cpu_count

from common import HOUSES, RUNS
from simulator import Config, simulate

POLICIES = ["naive", "night", "mpc_sn", "mpc_k24", "pf"]
TIGHT_FUSE = 9.6            # 3 x 16 A = 11.04 kW nominal, with the same margin as 15 of 17.25 kW


def matrix():
    cfgs = []
    for h in HOUSES:
        cfgs += [Config(h, p) for p in POLICIES]                                              # main
        cfgs += [Config(h, p, deadline_h=d) for d in (4, 12) for p in ("night", "mpc_sn", "mpc_k24", "pf")]
        cfgs += [Config(h, "mpc_sn", fuse_kw=f) for f in (None, TIGHT_FUSE)]                  # fuse
    return cfgs


def run(cfg):
    t = time.time()
    simulate(cfg)
    return cfg.name, time.time() - t


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--workers", type=int, default=max(1, cpu_count() - 1))
    a = ap.parse_args()
    todo = [c for c in matrix() if a.force or not (RUNS / c.name / "summary.json").exists()]
    print(f"{len(todo)} runs to do, {len(matrix()) - len(todo)} already done")
    with Pool(a.workers) as pool:
        for i, (name, s) in enumerate(pool.imap_unordered(run, todo), 1):
            print(f"[{i}/{len(todo)}] {name}  {s:.0f} s", flush=True)
