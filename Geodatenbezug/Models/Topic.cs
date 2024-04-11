using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the topic information from geodienste.ch.
/// </summary>
public record Topic
{
    [JsonPropertyName("base_topic")]
    required public string BaseTopic { get; set; }

    [JsonPropertyName("canton")]
    required public string Canton { get; set; }

    [JsonPropertyName("topic")]
    required public string TopicName { get; set; }

    [JsonPropertyName("topic_title")]
    required public string TopicTitle { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
