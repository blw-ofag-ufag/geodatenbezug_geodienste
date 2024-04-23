using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Biodiversitätsförderflächen".
/// </summary>
public class BiodiversitaetsfoerderflaechenProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
    /// <inheritdoc/>
    protected override Task ProcessTopic()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "t_id", FieldType.OFTInteger },
            { "bezugsjahr", FieldType.OFTDateTime },
            { "schnittzeitpunkt", FieldType.OFTDateTime },
            { "verpflichtung_von", FieldType.OFTDateTime },
            { "verpflichtung_bis", FieldType.OFTDateTime },
        };

        var bffQualitaet2FlaechenLayer = CreateGdalLayer("bff_qualitaet_2_flaechen", fieldTypeConversions);
        bffQualitaet2FlaechenLayer.CopyFeatures();
        bffQualitaet2FlaechenLayer.FilterLnfCodes();
        bffQualitaet2FlaechenLayer.ConvertMultiPartToSinglePartGeometry();

        var bffVernetzungFlaechenLayer = CreateGdalLayer("bff_vernetzung_flaechen", fieldTypeConversions);
        bffVernetzungFlaechenLayer.CopyFeatures();
        bffVernetzungFlaechenLayer.FilterLnfCodes();
        bffVernetzungFlaechenLayer.ConvertMultiPartToSinglePartGeometry();

        return Task.CompletedTask;
    }
}
