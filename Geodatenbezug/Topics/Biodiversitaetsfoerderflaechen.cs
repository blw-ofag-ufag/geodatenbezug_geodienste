using OSGeo.OGR;

namespace Geodatenbezug.Topics;

/// <summary>
/// The Biodiversitaetsfoerderflaechen topic.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Biodiversitaetsfoerderflaechen"/> class.
/// </remarks>
public class Biodiversitaetsfoerderflaechen(string inputFilePath) : GdalTopic(inputFilePath)
{
    /// <inheritdoc/>
    protected override void ProcessLayers()
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
    }
}
