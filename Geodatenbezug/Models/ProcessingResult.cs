using System.Net;
using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// The result of processing a topic.
/// </summary>
public record ProcessingResult
{
    /// <summary>
    /// The status code of the response.
    /// </summary>
    [JsonPropertyName("code")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    required public HttpStatusCode Code { get; set; }

    /// <summary>
    /// The response reason.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Information about the response.
    /// </summary>
    [JsonPropertyName("info")]
    public string? Info { get; set; }

    /// <summary>
    /// The title of the processed topic.
    /// </summary>
    [JsonPropertyName("topic_title")]
    required public string TopicTitle { get; set; }

    /// <summary>
    /// The canton of the processed topic.
    /// </summary>
    [JsonPropertyName("canton")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    required public Canton Canton { get; set; }

    /// <summary>
    /// The url to download the processed data.
    /// </summary>
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}
