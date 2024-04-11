using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the success response for info/services.json.
/// </summary>
public record GeodiensteInfoData
{
    [JsonPropertyName("services")]
    required public List<Topic> Services { get; set; }
}
