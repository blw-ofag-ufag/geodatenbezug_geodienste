using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the success response for info/services.json.
/// </summary>
public record GeodiensteInfoData
{
    /// <summary>
    /// All services (topics) available for the given query parameters.
    /// </summary>
    [JsonPropertyName("services")]
    required public List<Topic> Services { get; set; }
}
