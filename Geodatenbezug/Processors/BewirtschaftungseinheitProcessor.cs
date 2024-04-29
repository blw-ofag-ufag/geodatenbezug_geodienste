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
        using var bezugsjahrFieldDefinition = new FieldDefn("bezugsjahr", FieldType.OFTDateTime);
        var fieldTypeConversions = new Dictionary<string, FieldDefn>
        {
            { bezugsjahrFieldDefinition.GetName(), bezugsjahrFieldDefinition },
        };

        var betriebLayer = CreateGdalLayer("betrieb", fieldTypeConversions);
        betriebLayer.CopyFeatures();
        betriebLayer.ConvertMultiPartToSinglePartGeometry();

        var produktionsstaetteLayer = CreateGdalLayer("produktionsstaette", fieldTypeConversions);
        produktionsstaetteLayer.CopyFeatures();
        produktionsstaetteLayer.ConvertMultiPartToSinglePartGeometry();

        using var isDefinitivFieldDefinition = new FieldDefn("ist_definitiv", FieldType.OFTInteger);
        isDefinitivFieldDefinition.SetSubType(FieldSubType.OFSTInt16);
        fieldTypeConversions.Add(isDefinitivFieldDefinition.GetName(), isDefinitivFieldDefinition);
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
        var bewirtschaftungseinheitLayer = CreateGdalLayer("bewirtschaftungseinheit", fieldTypeConversions, ["identifikator_be"]);
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
        bewirtschaftungseinheitLayer.CopyFeatures();
        bewirtschaftungseinheitLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
