using System.ComponentModel.DataAnnotations;

namespace SmartHomeApplicationAPI.DTOs.Optimization
{
    // Input for one solver run: 96 steps of 15 minutes, where step 0 is StartTime.
    public class SolverReqDTO
    {
        public DateTime StartTime { get; set; }

        [Length(96, 96)]
        public List<double> Prices { get; set; } = [];    // c_t, DKK/kWh

        [Length(96, 96)]
        public List<double> BaseLoad { get; set; } = [];  // P_base_t, kW

        [Range(0.0, double.MaxValue)]
        public double FuseLimit { get; set; }             // P_fuse, kW

        public List<SolverLoadDTO> Loads { get; set; } = [];
    }

    // One controllable load. Type says which of the fields below are used.
    public class SolverLoadDTO
    {
        [Required]
        public string Type { get; set; } = string.Empty;       // "appliance" or "ev"

        public string? Name { get; set; }

        // --- Appliance only ---
        // Set ReadyStep and DeadlineStep to plan the appliance,
        // or FixedStart if the user already has a start time.
        [MinLength(1)]
        public List<double>? PowerProfile { get; set; }        // p_j, kW per step

        public int? ReadyStep { get; set; }                    // t_ready

        public int? DeadlineStep { get; set; }                 // t_deadline

        public int? FixedStart { get; set; }                   // tau, negative once started

        // --- EV only ---
        [Range(0, int.MaxValue)]
        public int? PlugInStep { get; set; }                   // t_plug, 0 if plugged in

        [Range(0, int.MaxValue)]
        public int? DepartureStep { get; set; }                // t_dep, can be after step 96

        [Range(0.0, double.MaxValue)]
        public double? InitialEnergy { get; set; }             // E_init, kWh

        [Range(0.0, double.MaxValue)]
        public double? EnergyGoal { get; set; }                // E_goal, kWh

        [Range(0.0, double.MaxValue)]
        public double? MaxEnergy { get; set; }                 // E_max, kWh

        [Range(0.0, double.MaxValue)]
        public double? MaxChargingPower { get; set; }          // P_ev_max, kW

        [Range(0.0, 1.0, MinimumIsExclusive = true)]
        public double? ChargingEfficiency { get; set; }        // eta

        public double? StoredEnergyValue { get; set; }         // v_ev, DKK/kWh
    }
}
