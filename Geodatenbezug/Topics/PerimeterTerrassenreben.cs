using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// The PerimeterTerrassenreben topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PerimeterTerrassenreben"/> class.
/// </remarks>
public class PerimeterTerrassenreben(string inputFilePath) : GdalTopic(inputFilePath)
{
    /// <inheritdoc/>
    protected override void ProcessLayers()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "t_id", FieldType.OFTInteger },
            { "bezugsjahr", FieldType.OFTDateTime },
            { "aenderungsdatum", FieldType.OFTDateTime },
        };
        var perimeterTerrassenrebenLayer = CreateGdalLayer("perimeter_terrassenreben", fieldTypeConversions);
        perimeterTerrassenrebenLayer.CopyFeatures();
        perimeterTerrassenrebenLayer.ConvertMultiPartToSinglePartGeometry();
    }
}
