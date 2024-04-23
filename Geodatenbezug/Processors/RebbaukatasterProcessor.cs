using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Rebbaukataster".
/// </summary>
public class RebbaukatasterProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopic()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "t_id", FieldType.OFTInteger },
            { "aenderungsdatum", FieldType.OFTDateTime },
        };
        var rebbaukatasterLayer = CreateGdalLayer("rebbaukataster", fieldTypeConversions);
        rebbaukatasterLayer.CopyFeatures();
        rebbaukatasterLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
