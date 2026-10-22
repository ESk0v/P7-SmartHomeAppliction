namespace Utilities.Models;

public record OptimizationResultDTO(	  
	DateTime StartTime,
	DateTime EndTime,
	decimal EstimatedPrice,
	decimal EstimatedCo2Emissions

);
