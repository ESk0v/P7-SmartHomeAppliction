"""
Example of the HEMS model in PuLP 4. The (n) labels are the equation numbers
in the report (Section III).

One realistic evening: the plan is made at 16:00 and covers the next 24 hours
in 96 steps of 15 minutes. All numbers are made up to look like a Danish house.

Install:  pip install -r requirements.txt
"""
from pulp import LpProblem, LpMinimize, HiGHS, lpSum, value

# ---- time (1)
dt = 0.25                       # h
N = 96
T = range(N)
START_HOUR = 16                 # step 0 is 16:00, so step 12 is 19:00

def clock(t):
    minutes = (START_HOUR * 60 + t * 15) % (24 * 60)
    return f"{minutes // 60:02d}:{minutes % 60:02d}"

# ---- household, one value per hour from 16:00
hourly_prices = [               # DKK/kWh
    1.6, 2.4, 2.9, 2.6, 2.0, 1.6, 1.3, 1.1,     # 16:00 - 23:00
    0.9, 0.8, 0.7, 0.7, 0.8, 1.0, 1.7, 2.2,     # 00:00 - 07:00
    2.1, 1.7, 1.4, 1.2, 1.1, 1.2, 1.3, 1.4,     # 08:00 - 15:00
]
hourly_base_load = [            # kW
    1.5, 2.8, 3.2, 2.4, 1.8, 1.5, 1.2, 1.0,     # 16:00 - 23:00
    1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.5, 2.0,     # 00:00 - 07:00
    1.5, 1.0, 1.0, 1.2, 1.2, 1.0, 1.0, 1.2,     # 08:00 - 15:00
]
c = [price for price in hourly_prices for _ in range(4)]            # c_t
P_base = [load for load in hourly_base_load for _ in range(4)]      # P_base_t
P_fuse = 16.5                   # kW, 3 * 230 V * 25 A = 17.25 kW, reduced slightly

# ---- appliances, with power in kW for each 15-minute step of the cycle
appliances = [
    # Already started 30 minutes ago, so it is a known load
    {"name": "towel_wash", "p": [2.2, 2.2, 2.2, 0.5, 0.5, 0.5, 0.5, 0.5],
     "fixed_start": -2},
    # Ready from 19:00, done by 07:00
    {"name": "dishwasher", "p": [2.0, 2.0, 0.2, 0.2, 0.2, 0.2, 1.8, 1.8],
     "t_ready": 12, "t_deadline": 60},
    # Ready from 19:00, done by 02:00
    {"name": "clothes_wash", "p": [2.0, 2.0, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5],
     "t_ready": 12, "t_deadline": 40},
    # Ready from 02:00 (after the clothes wash), done by 07:00
    {"name": "dryer", "p": [3.0, 3.0, 3.0, 3.0, 3.0, 3.0],
     "t_ready": 40, "t_deadline": 60},
]

# ---- EV: home from 17:00 until 07:00
P_ev_max, eta = 11, 1.0         # kW, -
E_init, E_goal, E_max = 20, 50, 64             # kWh
t_plug, t_dep = 4, 60
t_end = min(t_dep, N)
T_ev = range(t_plug, t_end)                    # (5) steps the car can charge
lam = 10                        # DKK per missing kWh
v_ev = 0                        # DKK per kWh left in the battery, not used for now

def p_at(p, j):
    return p[j] if 0 <= j < len(p) else 0

### alt ovenover er basically hardcoded eksempel, med alt det data vi skal bruge til vores model
### modellen + random print er nedenunder 

# Problem and variables
prob = LpProblem("HEMS", LpMinimize)

s = {}                          # s[name][tau] = 1 if the appliance starts in step tau
P_app = {}                      # P_app[name][t] = power of the appliance in step t

for a in appliances:
    name, p = a["name"], a["p"]
    if "fixed_start" in a:
        P_app[name] = {t: p_at(p, t - a["fixed_start"]) for t in T}     # known load
    else:
        S = range(a["t_ready"], a["t_deadline"] - len(p) + 1)           # (2) allowed starts
        s[name] = {tau: prob.add_variable(f"s_{name}_{tau}", cat="Binary") for tau in S}
        prob += lpSum(s[name].values()) == 1                            # (3) one start
        P_app[name] = {t: lpSum(p_at(p, t - tau) * s[name][tau] for tau in S) for t in T}  # (4)

P_ev = {t: prob.add_variable(f"P_ev_{t}", 0, P_ev_max) for t in T}  # (8) as bounds
E = {t: prob.add_variable(f"E_{t}", 0) for t in range(t_plug, t_end + 1)}
u = prob.add_variable("u", 0)

P_grid = {t: P_base[t] + lpSum(P_app[name][t] for name in P_app) + P_ev[t] for t in T}  # (12)

# Optimization objective (11)
prob += lpSum(c[t] * P_grid[t] * dt for t in T) + lam * u - v_ev * E[t_end]

# Constraints 
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

stats = prob.solve(HiGHS(msg=False))
print("Status:", stats.status_str)

print("\nAdvised start times:")
for a in appliances:
    name = a["name"]
    if name in s:
        start = next(tau for tau in s[name] if value(s[name][tau]) > 0.5)
        print(f"  {name:13} {clock(start)} - {clock(start + len(a['p']))}")
    else:
        print(f"  {name:13} already running")

print("\nHourly plan (average kW, battery at the end of the hour):")
print("Time   Price  Base  Appliances    EV   Grid  Battery")
for hour in range(0, N, 4):
    steps = range(hour, hour + 4)
    base = sum(P_base[t] for t in steps) / 4
    appliance = sum(value(P_app[name][t]) for name in P_app for t in steps) / 4
    ev = sum(value(P_ev[t]) for t in steps) / 4
    grid = sum(value(P_grid[t]) for t in steps) / 4
    battery = f"{value(E[hour + 4]):7.1f}" if hour + 4 in E else "      -"
    print(f"{clock(hour)}  {c[hour]:5.2f}  {base:4.1f}  {appliance:10.1f}  {ev:4.1f}  {grid:5.1f}  {battery}")

peak = max(T, key=lambda t: value(P_grid[t]))
print(f"\nBattery at departure: {value(E[t_end]):.1f} kWh (goal {E_goal} kWh, missing {value(u):.1f} kWh)")
print(f"Peak grid power: {value(P_grid[peak]):.1f} kW at {clock(peak)} (fuse limit {P_fuse} kW)")

def cost_of(power):
    return sum(c[t] * value(power[t]) * dt for t in T)

load_costs = {name: cost_of(P_app[name]) for name in P_app}
load_costs["ev"] = cost_of(P_ev)
total_excl_base = sum(load_costs.values())
base_cost = cost_of(P_base)

print("\nCost per load:")
for name, cost in load_costs.items():
    print(f"  {name:22} {cost:7.2f} DKK")
print(f"  {'Total excl. base load':22} {total_excl_base:7.2f} DKK")
print(f"  {'Base load':22} {base_cost:7.2f} DKK")
print(f"  {'Total':22} {total_excl_base + base_cost:7.2f} DKK")
