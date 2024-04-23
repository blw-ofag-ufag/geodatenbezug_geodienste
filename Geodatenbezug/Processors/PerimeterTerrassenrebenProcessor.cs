using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Perimeter Terrassenreben".
/// </summary>
public class PerimeterTerrassenrebenProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopic()
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

        return Task.CompletedTask;
    }
}
