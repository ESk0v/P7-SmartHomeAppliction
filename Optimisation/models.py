from datetime import datetime
from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_pascal


class _Base(BaseModel):
    # C# sends PascalCase (StartTime, FuseLimit, ...)
    model_config = ConfigDict(alias_generator=to_pascal, populate_by_name=True)


class SolverLoad(_Base):
    type: str                                   # "appliance" | "ev"
    name: str | None = None

    # appliance
    power_profile: list[float] | None = None    # kWh per 15-min step
    ready_step: int | None = None
    deadline_step: int | None = None
    fixed_start: int | None = None

    # ev
    initial_energy: float | None = None         # kWh
    energy_goal: float | None = None            # kWh
    max_energy: float | None = None             # kWh
    max_charging_power: float | None = None     # kW


class SolverReq(_Base):
    start_time: datetime
    prices: list[float]                         # DKK/kWh per step
    base_load: list[float]                      # kW per step
    fuse_limit: float                           # kW
    loads: list[SolverLoad] = Field(default_factory=list)