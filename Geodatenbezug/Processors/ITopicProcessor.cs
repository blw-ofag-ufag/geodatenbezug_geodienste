using Geodatenbezug.Models;

namespace Geodatenbezug.Processors;

/// <summary>
/// The interface for a topic processor.
/// </summary>
public interface ITopicProcessor
{
    /// <summary>
    /// Runs the complete processing steps for the topic from downloading the data to publishing the output.
    /// </summary>
    public Task<ProcessingResult> ProcessAsync();
}
