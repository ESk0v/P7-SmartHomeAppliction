namespace Utilities.Models;

public record PresetDto(
    int id,
    string presetName,
    int consumption,
    int duration
);