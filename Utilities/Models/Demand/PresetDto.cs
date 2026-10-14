namespace Utilities.Models;

public record PresetDto(
    int id,
    string presetName,
    decimal consumption,
    int duration
);