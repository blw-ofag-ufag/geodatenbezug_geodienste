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
    /// Message if only one data export per topic is allowed every 24 h.
    /// </summary>
    public static readonly string OnlyOneExport = "Only one data export per topic allowed every 24 h";

    /// <summary>
    /// Message if unexpected error occurred.
    /// </summary>
    public static readonly string Unexpected = "An unexpected error occurred. Please try again by starting a new data export";

    /// <summary>
    /// The error message.
    /// </summary>
    [JsonPropertyName("error")]
    public required string Error { get; set; }
}
