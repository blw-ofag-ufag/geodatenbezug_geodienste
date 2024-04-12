using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the success response for info/services.json.
/// </summary>
public record GeodiensteStatusSuccess
{
    /// <summary>
    /// The status of the geodata export.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    required public GeodiensteStatus Status { get; set; }

    /// <summary>
    /// Information about the export.
    /// </summary>
    [JsonPropertyName("info")]
    required public string Info { get; set; }

    /// <summary>
    /// The download URL for the exported geodata.
    /// </summary>
    [JsonPropertyName("download_url")]
    required public string DownloadUrl { get; set; }

    /// <summary>
    /// The date and time when the geodata was exported.
    /// </summary>
    [JsonPropertyName("exported_at")]
    required public DateTime ExportedAt { get; set; }
}
