using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the topic information from geodienste.ch.
/// </summary>
public record Topic
{
    [JsonPropertyName("base_topic")]
    public required string BaseTopic { get; set; }

    [JsonPropertyName("canton")]
    public required string Canton { get; set; }

    [JsonPropertyName("topic")]
    public required string TopicName { get; set; }

    [JsonPropertyName("topic_title")]
    public required string TopicTitle { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
