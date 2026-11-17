# Evaluation

Simulates ten virtual households for one year (1 Nov 2025 - 31 Oct 2026, DK1 spot prices) under four
policies and builds the tables and the figure for the paper. Nothing outside this folder is changed;
the MPC calls `Optimisation/optimiser.py::solve` as it is.

## Run

```
cd Evaluation
pip install -r requirements.txt
python prepare_data.py ...        # already done: fills data/
python build_scenarios.py         # 20 s: scenarios/hhNN.json + scenarios/households.csv
python -m pytest tests -q         # leak and consistency checks
python run_all.py                 # 150 runs, about 10 minutes; finished runs are skipped
python report.py                  # results/results.md, results/*.csv, results/fig_monthly_saving.pdf
```

After a change to the solver or the scenarios, rerun with `python run_all.py --force`.

## Files

| File | Purpose |
| --- | --- |
| `common.py` | Constants. Step 0 = 1 Nov 2025 00:00 Danish time, one step = 15 min |
| `build_scenarios.py` | Maps REFIT and the EV sessions onto the evaluation year (ISO week + weekday, local clock), pairs houses and EV users (seed 0) |
| `prices.py` | `PriceOracle` (13:00 publication rule, seasonal naive) and the base-load forecast |
| `simulator.py` | `Config`, the policies (naive, night timer, MPC), the step loop and the cost accounting |
| `run_all.py` | The experiment matrix |
| `report.py` | Tables and figure |
| `tests/test_eval.py` | No price or base-load leakage, equal energy across policies, MPC-K24 never worse than a baseline, solver EV departure |

## Policies

- `naive`: EV at full power from plug-in; appliances start when ready.
- `night`: EV and appliances start at the next 00:00 if the goal or the deadline still allows it.
- `mpc_sn`: MPC with published prices plus seasonal naive for unpublished hours (main).
- `mpc_k24`: MPC with the actual prices for the next 24 h.
- `pf`: perfect foresight, the benchmark for the possible saving. Knows the actual prices for the whole year
  and each session's departure and energy; each cycle and session is scheduled at its cheapest. Ignores the fuse limit.

## Things to know

- **Solver EV model.** The MPC makes one joint solve per decision. The EV is sent with `ReadyStep` = plug-in and
  `DeadlineStep` = departure, the same fields as an appliance's window (branch `solver-ev-departure`).
- **Cycle profile.** The MPC plans each cycle with its real power profile, because the app knows the
  programme. `Config(profile="typical")` plans with one average profile per house and appliance instead.
- **EV users.** The ten users are listed in `build_scenarios.py` with the rule behind the choice.
- **Fuse violations** are split into "base load alone above the limit" (data) and "caused by EV/appliances".
- **Costs** always use actual prices and actual load. Base load is the same for every policy.
- **Last 3 days.** No new appliance cycles start in the last 3 days, so every cycle finishes inside the year.
- `LAM` is 10 DKK/kWh in the solver (the maximum spot price in the year is 5.88).
