using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// The Rebbaukataster topic.
/// </summary>
public class Rebbaukataster : GdalTopic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Rebbaukataster"/> class.
    /// </summary>
    public Rebbaukataster(string inputFilePath)
        : base(inputFilePath)
    {
    }

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
