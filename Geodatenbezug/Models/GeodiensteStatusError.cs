using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the success response downloads/status.json.
/// </summary>
public record GeodiensteStatusError
{
    /// <summary>
    /// The error message.
    /// </summary>
    [JsonPropertyName("error")]
    required public string Error { get; set; }
}
