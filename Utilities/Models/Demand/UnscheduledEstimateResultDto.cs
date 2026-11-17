namespace Utilities.Models;

public record UnscheduledEstimateResultDto(
    UnscheduledEstimateDto? Estimate,
    string? Error
);
