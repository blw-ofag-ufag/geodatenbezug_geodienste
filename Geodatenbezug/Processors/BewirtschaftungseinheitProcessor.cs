using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Bewirtschaftungseinheit".
/// </summary>
public class BewirtschaftungseinheitProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopicAsync()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "bezugsjahr", FieldType.OFTDateTime },
        };

        var betriebLayer = CreateGdalLayer("betrieb", fieldTypeConversions);
        betriebLayer.CopyFeatures();
        betriebLayer.ConvertMultiPartToSinglePartGeometry();
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
        var bewirtschaftungseinheitLayer = CreateGdalLayer("bewirtschaftungseinheit", fieldTypeConversions, ["identifikator_be"]);
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
        bewirtschaftungseinheitLayer.CopyFeatures();
        bewirtschaftungseinheitLayer.ConvertMultiPartToSinglePartGeometry();

        var produktionsstaetteLayer = CreateGdalLayer("produktionsstaette", fieldTypeConversions);
        produktionsstaetteLayer.CopyFeatures();
        produktionsstaetteLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
