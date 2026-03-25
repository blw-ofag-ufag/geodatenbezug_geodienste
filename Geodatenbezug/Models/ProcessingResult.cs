using System.Net;

namespace Geodatenbezug.Models;

/// <summary>
/// The result of processing a topic.
/// </summary>
public record ProcessingResult
{
    /// <summary>
    /// The status code of the response.
    /// </summary>
    public required HttpStatusCode Code { get; set; }

    /// <summary>
    /// The response reason.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Information about the response.
    /// </summary>
    public string? Info { get; set; }

    /// <summary>
    /// The title of the processed topic.
    /// </summary>
    public required string TopicTitle { get; set; }

    /// <summary>
    /// The canton of the processed topic.
    /// </summary>
    public required Canton Canton { get; set; }

    /// <summary>
    /// The date and time the data was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// The url to download the processed data.
    /// </summary>
    public string? DownloadUrl { get; set; }
}
