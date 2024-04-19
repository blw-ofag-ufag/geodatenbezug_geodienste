using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// The PerimeterLnSf topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PerimeterLnSf"/> class.
/// </remarks>
public class PerimeterLnSf(string inputFilePath) : GdalTopic(inputFilePath)
{
    /// <inheritdoc/>
    protected override void ProcessLayers()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "t_id", FieldType.OFTInteger },
            { "bezugsjahr", FieldType.OFTDateTime },
        };
        var perimeterLnSfLayer = CreateGdalLayer("perimeter_ln_sf", fieldTypeConversions);

        // TODO: Fields do not exist in the input data
        perimeterLnSfLayer.RemoveField("_part_number");
        perimeterLnSfLayer.RemoveField("_geometry_name");
        perimeterLnSfLayer.CopyFeatures();
        perimeterLnSfLayer.ConvertMultiPartToSinglePartGeometry();
    }
}
