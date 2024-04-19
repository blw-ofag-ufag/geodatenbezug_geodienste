using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// The Rebbaukataster topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Rebbaukataster"/> class.
/// </remarks>
public class Rebbaukataster(string inputFilePath) : GdalTopic(inputFilePath)
{
    /// <inheritdoc/>
    protected override void ProcessLayers()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "t_id", FieldType.OFTInteger },
            { "aenderungsdatum", FieldType.OFTDateTime },
        };
        var rebbaukatasterLayer = CreateGdalLayer("rebbaukataster", fieldTypeConversions);
        rebbaukatasterLayer.CopyFeatures();
        rebbaukatasterLayer.ConvertMultiPartToSinglePartGeometry();
    }
}
