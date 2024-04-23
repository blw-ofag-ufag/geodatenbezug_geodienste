using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Perimeter Ln Sf".
/// </summary>
public class PerimeterLnSfProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopic()
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

        return Task.CompletedTask;
    }
}
