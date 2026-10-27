// DTOs/EnergiDataService/EnergiDataResponse.cs
using System.Text.Json.Serialization;

namespace SmartHomeApplicationAPI.DTOs.EnergiDataService;

/// <summary>Envelope shared by all Energi Data Service datasets.</summary>
public sealed class EnergiDataResponse<TRecord>
{
    [JsonPropertyName("records")]
    public List<TRecord> Records { get; init; } = [];
}