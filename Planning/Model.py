"""
Simple example of the HEMS model in PuLP 4. The (n) labels are the equation
numbers in the report (Section III).

A 4-step example that can be checked by hand:
  - prices 1, 2, 3, 4 DKK/kWh, base load 1 kW, fuse 9 kW, dt = 0.25 h
  - washing machine: 2 kW then 1 kW, may start in step 0, 1 or 2
  - EV: plugged in for steps 1 and 2, up to 8 kW, goal 3 kWh, max 3.5 kWh (eta = 1)
Expected plan: wash in step 0, EV charges 7 kW in steps 1 and 2 and ends at
3.5 kWh, grid power 3, 9, 8, 1 kW, cost excl. base load 9.75 DKK.

Install:  pip install -r requirements.txt
"""
from pulp import LpProblem, LpMinimize, HiGHS, lpSum, value

# ---- data
dt = 0.25                       # h
c = [1, 2, 3, 4]                # DKK/kWh
P_base = [1, 1, 1, 1]           # kW
P_fuse = 9                      # kW
N = len(c)
T = range(N)                    # (1)

p = [2, 1]                      # washing machine profile (kW)
D = len(p)
t_ready, t_deadline = 0, 4
S = range(t_ready, t_deadline - D + 1)         # (2) allowed start steps

P_ev_max, eta = 8, 1.0          # kW, -
E_init, E_goal, E_max = 0, 3, 3.5              # kWh
t_plug, t_dep = 1, 3            # car arrives in step 1, leaves at step 3
t_end = min(t_dep, N)
T_ev = range(t_plug, t_end)                    # (5) steps the car can charge
lam = 100                       # DKK per missing kWh
v_ev = 3.5                      # DKK per kWh left in the battery

def p_at(j):
    return p[j] if 0 <= j < len(p) else 0

# ---- problem and variables
prob = LpProblem("HEMS", LpMinimize)

s = {tau: prob.add_variable(f"s_{tau}", cat="Binary") for tau in S}
P_ev = {t: prob.add_variable(f"P_ev_{t}", 0, P_ev_max) for t in T}  # (8) as bounds
E = {t: prob.add_variable(f"E_{t}", 0) for t in range(t_plug, t_end + 1)}
u = prob.add_variable("u", 0)

P_app = {t: lpSum(p_at(t - tau) * s[tau] for tau in S) for t in T}  # (4)
P_grid = {t: P_base[t] + P_app[t] + P_ev[t] for t in T}             # (12)

# ---- objective (11)
prob += lpSum(c[t] * P_grid[t] * dt for t in T) + lam * u - v_ev * E[t_end]

# ---- constraints
prob += lpSum(s.values()) == 1                     # (3) one start
prob += E[t_plug] == E_init                        # (6) start energy
for t in T_ev:
    prob += E[t + 1] == E[t] + eta * P_ev[t] * dt  # (7) battery
    prob += E[t + 1] <= E_max                      # (9) max energy
if t_dep <= N:
    prob += E[t_dep] + u >= E_goal                 # (10) soft goal
for t in T:
    prob += P_grid[t] <= P_fuse                    # (13) fuse
    if t not in T_ev:
        prob += P_ev[t] == 0                       # (14) car not available

# ---- solve
stats = prob.solve(HiGHS(msg=False))
print("Status:", stats.status_str)

start = next(tau for tau in S if value(s[tau]) > 0.5)
base_cost = sum(c[t] * P_base[t] * dt for t in T)
print("Wash starts in step", start)
print("EV power:  ", [round(value(P_ev[t]), 2) for t in T])
print("Battery:   ", [round(value(E[t]), 2) for t in E], "kWh, from step", t_plug)
print("Grid power:", [round(value(P_grid[t]), 2) for t in T])
print("Missing from goal:", round(value(u), 2), "kWh")
print("Cost excl. base load:",
      round(value(lpSum(c[t] * P_grid[t] * dt for t in T)) - base_cost, 2), "DKK")
