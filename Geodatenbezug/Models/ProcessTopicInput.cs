namespace Geodatenbezug.Models;

/// <summary>
/// Input for the ProcessTopic activity function.
/// </summary>
public record ProcessTopicInput
{
    /// <summary>
    /// The topic to process.
    /// </summary>
    public required Topic Topic { get; set; }

    /// <summary>
    /// Whether to keep the downloaded data after processing. Default is false.
    /// </summary>
    public bool KeepDownload { get; set; }
}
