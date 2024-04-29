using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Perimeter Terrassenreben".
/// </summary>
public class PerimeterTerrassenrebenProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopicAsync()
    {
        using var bezugsjahrFieldDefinition = new FieldDefn("bezugsjahr", FieldType.OFTDateTime);
        using var aenderungsdatumFieldDefinition = new FieldDefn("aenderungsdatum", FieldType.OFTDateTime);
        var fieldTypeConversions = new Dictionary<string, FieldDefn>
        {
            { bezugsjahrFieldDefinition.GetName(), bezugsjahrFieldDefinition },
            { aenderungsdatumFieldDefinition.GetName(), aenderungsdatumFieldDefinition },
        };

        var perimeterTerrassenrebenLayer = CreateGdalLayer("perimeter_terrassenreben", fieldTypeConversions);
        perimeterTerrassenrebenLayer.CopyFeatures();
        perimeterTerrassenrebenLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
