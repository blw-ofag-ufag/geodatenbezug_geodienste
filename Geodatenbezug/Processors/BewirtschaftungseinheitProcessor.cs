﻿using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Bewirtschaftungseinheit".
/// </summary>
public class BewirtschaftungseinheitProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopic()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "t_id", FieldType.OFTInteger },
            { "bezugsjahr", FieldType.OFTDateTime },
        };

        var betriebLayer = CreateGdalLayer("betrieb", fieldTypeConversions);
        betriebLayer.CopyFeatures();
        betriebLayer.ConvertMultiPartToSinglePartGeometry();

        var bewirtschaftungseinheitLayer = CreateGdalLayer("bewirtschaftungseinheit", fieldTypeConversions);
        bewirtschaftungseinheitLayer.RemoveField("identifikator_be");
        bewirtschaftungseinheitLayer.CopyFeatures();
        bewirtschaftungseinheitLayer.ConvertMultiPartToSinglePartGeometry();

        var produktionsstaetteLayer = CreateGdalLayer("produktionsstaette", fieldTypeConversions);
        produktionsstaetteLayer.CopyFeatures();
        produktionsstaetteLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
