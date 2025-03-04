﻿using System.Text.Json.Serialization;

namespace Geodatenbezug.Models;

/// <summary>
/// Represents the topic information from geodienste.ch.
/// </summary>
public record Topic
{
    /// <summary>
    /// Base topic name.
    /// </summary>
    [JsonPropertyName("base_topic")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required BaseTopic BaseTopic { get; set; }

    /// <summary>
    /// Canton the data is from.
    /// </summary>
    [JsonPropertyName("canton")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Canton Canton { get; set; }

    /// <summary>
    /// Topic title.
    /// </summary>
    [JsonPropertyName("topic_title")]
    public required string TopicTitle { get; set; }

    /// <summary>
    /// Date and time the data was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Topic title.
    /// </summary>
    [JsonPropertyName("publication_data")]
    public string? PublicationData { get; set; }
}
