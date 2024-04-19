namespace Geodatenbezug.Topics;

/// <summary>
/// The Nutzungsflaechen topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Nutzungsflaechen"/> class.
/// </remarks>
public class Nutzungsflaechen(string inputFilePath) : GdalTopic(inputFilePath)
{
    /// <inheritdoc/>
    protected override void ProcessLayers()
    {
        // TODO: Implement the ProcessLayers method.
    }
}
