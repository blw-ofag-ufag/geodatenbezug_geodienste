using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Rebbaukataster".
/// </summary>
public class RebbaukatasterProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopic()
    {
        using var aenderungsdatumFieldDefinition = new FieldDefn("aenderungsdatum", FieldType.OFTDateTime);
        var fieldTypeConversions = new Dictionary<string, FieldDefn>
        {
            { aenderungsdatumFieldDefinition.GetName(), aenderungsdatumFieldDefinition },
        };

        var rebbaukatasterLayer = CreateGdalLayer("rebbaukataster", fieldTypeConversions);
        rebbaukatasterLayer.CopyFeatures();
        rebbaukatasterLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
