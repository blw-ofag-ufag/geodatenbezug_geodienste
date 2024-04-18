using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// The PerimeterTerrassenreben topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PerimeterTerrassenreben"/> class.
/// </remarks>
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
public class PerimeterTerrassenreben(string inputFilePath) : GdalTopic(inputFilePath)
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
{
    /// <inheritdoc/>
    protected override void ProcessLayers()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "t_id", FieldType.OFTInteger },
            { "aenderungsdatum", FieldType.OFTDateTime },
        };
        var perimeterTerrassenrebenLayer = CreateGdalLayer("perimeter_terrassenreben", fieldTypeConversions);
        perimeterTerrassenrebenLayer.CopyFeatures();
        perimeterTerrassenrebenLayer.ConvertMultiPartToSinglePartGeometry();
    }
}
