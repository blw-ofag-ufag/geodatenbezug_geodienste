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
        using var zoneAuslandFieldDefinition = new FieldDefn("zone_ausland", FieldType.OFTString);
        zoneAuslandFieldDefinition.SetWidth(254);
        var fieldTypeConversions = new Dictionary<string, FieldDefn>
        {
            { bezugsjahrFieldDefinition.GetName(), bezugsjahrFieldDefinition },
            { zoneAuslandFieldDefinition.GetName(), zoneAuslandFieldDefinition },
        };

        var betriebLayer = CreateGdalLayer("betrieb", fieldTypeConversions, false, true);
        betriebLayer.CopyFeatures();

        var produktionsstaetteLayer = CreateGdalLayer("produktionsstaette", fieldTypeConversions, false, true);
        produktionsstaetteLayer.CopyFeatures();

        using var isDefinitivFieldDefinition = new FieldDefn("ist_definitiv", FieldType.OFTInteger);
        isDefinitivFieldDefinition.SetSubType(FieldSubType.OFSTInt16);
        fieldTypeConversions.Add(isDefinitivFieldDefinition.GetName(), isDefinitivFieldDefinition);
        var bewirtschaftungseinheitLayer = CreateGdalLayer("bewirtschaftungseinheit", fieldTypeConversions, ["identifikator_be"], false, true);
        bewirtschaftungseinheitLayer.CopyFeatures();

        return Task.CompletedTask;
    }
}
