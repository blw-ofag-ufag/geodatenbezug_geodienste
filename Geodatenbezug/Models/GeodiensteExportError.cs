using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the error response for downloads/export.json.
/// </summary>
public record GeodiensteExportError
{
    /// <summary>
    /// Message if token is invalid.
    /// </summary>
    public static readonly string InvalidToken = "Data export information not found. Invalid token?";

    /// <summary>
    /// Message if other data export is pending.
    /// </summary>
    public static readonly string Pending = "Cannot start data export because there is another data export pending";

    /// <summary>
    /// Message if unexpected error occurred.
    /// </summary>
    public static readonly string Unexpected = "An unexpected error occurred. Please try again by starting a new data export";

    /// <summary>
    /// The error message.
    /// </summary>
    [JsonPropertyName("error")]
    required public string Error { get; set; }
}
