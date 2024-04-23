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

    /// <summary>
    /// Gets the token for the given topic and canton from the environment variables.
    /// </summary>
    public string GetToken(BaseTopic baseTopic, Canton canton);
}
