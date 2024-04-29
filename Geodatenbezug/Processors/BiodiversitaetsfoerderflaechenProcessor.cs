using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Biodiversitätsförderflächen".
/// </summary>
public class BiodiversitaetsfoerderflaechenProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopicAsync()
    {
        using var bezugsjahrFieldDefinition = new FieldDefn("bezugsjahr", FieldType.OFTDateTime);
        using var schnittzeitpunktFieldDefinition = new FieldDefn("schnittzeitpunkt", FieldType.OFTDateTime);
        using var verpflichtungVonFieldDefinition = new FieldDefn("verpflichtung_von", FieldType.OFTDateTime);
        using var verpflichtungBisFieldDefinition = new FieldDefn("verpflichtung_bis", FieldType.OFTDateTime);
        using var istDefinitivFieldDefinition = new FieldDefn("ist_definitiv", FieldType.OFTInteger);
        istDefinitivFieldDefinition.SetSubType(FieldSubType.OFSTInt16);
        using var beitragsberechtigtFieldDefinition = new FieldDefn("beitragsberechtigt", FieldType.OFTInteger);
        beitragsberechtigtFieldDefinition.SetSubType(FieldSubType.OFSTInt16);
        var fieldTypeConversions = new Dictionary<string, FieldDefn>
        {
            { bezugsjahrFieldDefinition.GetName(), bezugsjahrFieldDefinition },
            { schnittzeitpunktFieldDefinition.GetName(), schnittzeitpunktFieldDefinition },
            { verpflichtungVonFieldDefinition.GetName(), verpflichtungVonFieldDefinition },
            { verpflichtungBisFieldDefinition.GetName(), verpflichtungBisFieldDefinition },
            { istDefinitivFieldDefinition.GetName(), istDefinitivFieldDefinition },
            { beitragsberechtigtFieldDefinition.GetName(), beitragsberechtigtFieldDefinition },
        };

        var bffVernetzungFlaechenLayer = CreateGdalLayer("bff_vernetzung_flaechen", fieldTypeConversions);
        bffVernetzungFlaechenLayer.CopyFeatures();
        bffVernetzungFlaechenLayer.FilterLnfCodes();
        bffVernetzungFlaechenLayer.ConvertMultiPartToSinglePartGeometry();

        using var nhgFieldDefinition = new FieldDefn("nhg", FieldType.OFTInteger);
        nhgFieldDefinition.SetSubType(FieldSubType.OFSTInt16);
        fieldTypeConversions.Add(nhgFieldDefinition.GetName(), nhgFieldDefinition);
        var bffQualitaet2FlaechenLayer = CreateGdalLayer("bff_qualitaet_2_flaechen", fieldTypeConversions);
        bffQualitaet2FlaechenLayer.CopyFeatures();
        bffQualitaet2FlaechenLayer.FilterLnfCodes();
        bffQualitaet2FlaechenLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
