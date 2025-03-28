﻿using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Perimeter Ln Sf".
/// </summary>
public class PerimeterLnSfProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopicAsync()
    {
        using var bezugsJahrFieldDefinition = new FieldDefn("bezugsjahr", FieldType.OFTDateTime);
        using var typFieldDefinition = new FieldDefn("typ", FieldType.OFTString);
        typFieldDefinition.SetWidth(999);
        var fieldTypeConversions = new Dictionary<string, FieldDefn>
        {
            { bezugsJahrFieldDefinition.GetName(), bezugsJahrFieldDefinition },
            { typFieldDefinition.GetName(), typFieldDefinition },
        };
        var perimeterLnSfLayer = CreateGdalLayer("perimeter_ln_sf", fieldTypeConversions, false, true);
        perimeterLnSfLayer.CopyFeatures();

        return Task.CompletedTask;
    }
}
