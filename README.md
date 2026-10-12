# P7-SmartHomeAppliction

Hej# CI/CD

## Optimization approach (MILP)

The smart-home scheduler is modeled as a **Mixed-Integer Linear Program (MILP)** where each controllable device/preset is assigned a start time inside an allowed window.

- **Decision variables**
  - Binary start/on variables per device and timeslot.
  - Optional linking binaries for split schedules (`next_schedule_id`) when a task is executed in multiple periods.
- **Objective**
  - Minimize total expected operating cost based on imported day-ahead prices (`DayAheadPrice.Price`).
  - Optionally include a weighted CO₂ term (`estimated_co2_emissions_in_percent`) to trade cost vs. sustainability.
- **Core constraints**
  - Each selected task must run for its required duration (`duration_in_minutes`).
  - Device energy use follows preset/device demand (`consumption_in_kwh`).
  - Start/end times must stay within user-defined flexibility windows.
  - No invalid overlaps for the same device/task chain.

The optimized result is persisted as schedule rows (`start_time`, `end_time`, `estimated_price_in_kroner`, `color`) and exposed through the plan endpoint.

## MPC strategy for this project

For this system, MPC is used as a **rolling re-optimization loop** on top of the MILP model:

1. Build a prediction horizon from available/forecasted prices (`DayAheadPrice`, including `IsPredicted` when needed).
2. Solve the MILP for the horizon and apply only the first control action(s) (next schedule decisions).
3. When new data arrives (updated prices, changed user preferences, device state changes), shift the horizon forward and solve again.
4. Repeat this loop at fixed intervals (or on events) so plans stay feasible and cost-efficient under changing conditions.

In practice, this means the platform can keep a day-ahead plan while still adapting during the day as uncertainty is reduced.
