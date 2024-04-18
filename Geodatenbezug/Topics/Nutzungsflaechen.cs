namespace Geodatenbezug.Topics;

/// <summary>
/// The Nutzungsflaechen topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Nutzungsflaechen"/> class.
/// </remarks>
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
public class Nutzungsflaechen(string inputFilePath) : GdalTopic(inputFilePath)
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
{
    /// <inheritdoc/>
    protected override void ProcessLayers()
    {
        // TODO: Implement the ProcessLayers method.
    }
}
