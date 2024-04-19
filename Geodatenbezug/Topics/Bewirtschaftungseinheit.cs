using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// The Bewirtschaftungseinheit topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Bewirtschaftungseinheit"/> class.
/// </remarks>
public class Bewirtschaftungseinheit(string inputFilePath) : GdalTopic(inputFilePath)
{
    /// <inheritdoc/>
    protected override void ProcessLayers()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "t_id", FieldType.OFTInteger },
            { "bezugsjahr", FieldType.OFTDateTime },
        };

        var betriebLayer = CreateGdalLayer("betrieb", fieldTypeConversions);
        betriebLayer.CopyFeatures();
        betriebLayer.ConvertMultiPartToSinglePartGeometry();

        var bewirtschaftungseinheitLayer = CreateGdalLayer("bewirtschaftungseinheit", fieldTypeConversions);
        bewirtschaftungseinheitLayer.RemoveField("identifikator_be");
        bewirtschaftungseinheitLayer.CopyFeatures();
        bewirtschaftungseinheitLayer.ConvertMultiPartToSinglePartGeometry();

        var produktionsstaetteLayer = CreateGdalLayer("produktionsstaette", fieldTypeConversions);
        produktionsstaetteLayer.CopyFeatures();
        produktionsstaetteLayer.ConvertMultiPartToSinglePartGeometry();
    }
}
