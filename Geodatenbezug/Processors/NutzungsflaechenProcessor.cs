using System.Xml;
using System.Xml.Serialization;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Nutzungsflächen".
/// </summary>
public class NutzungsflaechenProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    private const string NutzungsflaechenLayerName = "nutzungsflaechen";
    private const string NutzungsflaechenJoinedLayerName = $"{NutzungsflaechenLayerName}_joined";
    private const string NutzungsartLayerName = "nutzungsart";
    private const string BewirtschaftungseinheitLayerName = "bewirtschaftungseinheit";

    private const string CatalogUrl = "https://models.geo.admin.ch/BLW/LWB_Nutzungsflaechen_Kataloge_V2_0.xml";

    /// <summary>
    /// The path to the data of the topic "Bewirtschaftungseinheit".
    /// </summary>
    protected internal string BewirtschaftungseinheitDataPath { get; set; } = string.Empty;

    /// <inheritdoc />
    protected internal override async Task PrepareDataAsync()
    {
        Logger.LogInformation($"Bereite Daten für die Prozessierung von {Topic.TopicTitle} ({Topic.Canton}) vor...");

        var exportInputTopic = PrepareTopic(Topic);

        var bewirtschaftungseinheitTopic = new Topic()
        {
            TopicTitle = BaseTopic.lwb_bewirtschaftungseinheit.GetDescription(),
            Canton = Topic.Canton,
            BaseTopic = BaseTopic.lwb_bewirtschaftungseinheit,
        };
        var exportBewirtschaftungseinheitTopic = PrepareTopic(bewirtschaftungseinheitTopic);

        var downloadUrls = await Task.WhenAll(exportInputTopic, exportBewirtschaftungseinheitTopic).ConfigureAwait(false);

        InputDataPath = downloadUrls[0];
        BewirtschaftungseinheitDataPath = downloadUrls[1];
    }

    private async Task<string> PrepareTopic(Topic topic)
    {
        var downloadUrl = await ExportTopicAsync(topic).ConfigureAwait(false);
        return await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected async override Task ProcessTopicAsync()
    {
        // Load the input data and apply first processing steps to reduce the data
        using var bezugsJahrFieldDefinition = new FieldDefn("bezugsjahr", FieldType.OFTDateTime);
        var fieldTypeConversions = new Dictionary<string, FieldDefn>
        {
            { bezugsJahrFieldDefinition.GetName(), bezugsJahrFieldDefinition },
        };
        var fieldsToDrop = new List<string>
        {
            "nutzung",
            "ist_ueberlagernd",
            "anzahl_baeume",
            "beitragsberechtigt",
            "nutzung_im_beitragsjahr",
            "nhg",
            "ist_definitiv",
            "verpflichtung_von",
            "verpflichtung_bis",
            "schnittzeitpunkt",
        };

        var nutzungsflaechenTempLayer = CreateGdalLayer(NutzungsflaechenLayerName, fieldTypeConversions, fieldsToDrop);
        nutzungsflaechenTempLayer.CopyFeatures();
        nutzungsflaechenTempLayer.FilterLnfCodes();

        // Create a temporary layer with data from the nutzungsart catalog
        await CreateNutzungsartLayerAsync().ConfigureAwait(false);

        // Join the nutzungsflaechen layer with the nutzungsart layer and the bewirtschaftungseinheit layer, then add the new joined layer to the processing data source
        var joinQuery = @$"
            SELECT {NutzungsflaechenLayerName}.*, {NutzungsartLayerName}.*, {BewirtschaftungseinheitLayerName}.betriebsnummer
            FROM {NutzungsflaechenLayerName}
            LEFT JOIN {NutzungsartLayerName} ON {NutzungsflaechenLayerName}.lnf_code = {NutzungsartLayerName}.lnf_code
            LEFT JOIN '{BewirtschaftungseinheitDataPath}'.{BewirtschaftungseinheitLayerName} ON {NutzungsflaechenLayerName}.identifikator_be = {BewirtschaftungseinheitLayerName}.identifikator_be";
        var tmpLayer = ProcessingDataSource.ExecuteSQL(joinQuery, null, "OGRSQL");
        ProcessingDataSource.CopyLayer(tmpLayer, NutzungsflaechenJoinedLayerName, null);

        // Delete the initial nutzungsflaechen layer because we want to create a new one with the joined data
        ProcessingDataSource.DeleteLayer(0);

        // Create a new nutzungsflaechen layer with the desired fields
        var nutzungsflaechenJoinedLayer = ProcessingDataSource.GetLayerByName(NutzungsflaechenJoinedLayerName);
        var nutzungsflaechenLayer = ProcessingDataSource.CreateLayer(NutzungsflaechenLayerName, tmpLayer.GetSpatialRef(), nutzungsflaechenJoinedLayer.GetGeomType(), null);
        var fieldNameMapping = new Dictionary<string, string>();
        for (var i = 0; i < nutzungsflaechenJoinedLayer.GetLayerDefn().GetFieldCount(); i++)
        {
            var fieldDefn = nutzungsflaechenJoinedLayer.GetLayerDefn().GetFieldDefn(i);
            var originalFieldName = fieldDefn.GetName();
            string fieldName = originalFieldName;

            // Drop the fields that where only needed for the join
            if (originalFieldName == $"{NutzungsartLayerName}_lnf_code" || originalFieldName == $"{NutzungsflaechenLayerName}_identifikator_be")
            {
                continue;
            }

            // Rename the fields to remove the layer name prefix
            if (originalFieldName.Contains(NutzungsflaechenLayerName, StringComparison.InvariantCulture))
            {
                fieldName = originalFieldName.Replace($"{NutzungsflaechenLayerName}_", string.Empty, StringComparison.InvariantCulture);
            }

            if (originalFieldName.Contains(NutzungsartLayerName, StringComparison.InvariantCulture))
            {
                fieldName = originalFieldName.Replace($"{NutzungsartLayerName}_", string.Empty, StringComparison.InvariantCulture);
            }

            if (originalFieldName == $"{BewirtschaftungseinheitLayerName}_betriebsnummer")
            {
                fieldName = "bewe_betriebsnummer";
            }

            fieldNameMapping[fieldName] = originalFieldName;

            // Booleans are represented as an Int16 FieldSubTypes, so we have to apply the subtype to the field definition if it's available
            using var newFieldDefinition = new FieldDefn(fieldName, fieldDefn.GetFieldType());
            if (fieldDefn.GetSubType() != FieldSubType.OFSTNone)
            {
                newFieldDefinition.SetSubType(fieldDefn.GetSubType());
            }

            newFieldDefinition.SetWidth(fieldDefn.GetWidth());
            newFieldDefinition.SetPrecision(fieldDefn.GetPrecision());
            nutzungsflaechenLayer.CreateField(newFieldDefinition, 1);
        }

        // Copy the features from the joined layer to the new nutzungsflaechen layer
        nutzungsflaechenJoinedLayer.ResetReading();
        for (var i = 0; i < nutzungsflaechenJoinedLayer.GetFeatureCount(1); i++)
        {
            var feature = nutzungsflaechenJoinedLayer.GetNextFeature();
            using var newFeature = new Feature(nutzungsflaechenLayer.GetLayerDefn());
            newFeature.SetGeometry(feature.GetGeometryRef());

            for (var j = 0; j < nutzungsflaechenLayer.GetLayerDefn().GetFieldCount(); j++)
            {
                var fieldDefn = nutzungsflaechenLayer.GetLayerDefn().GetFieldDefn(j);
                var fieldName = fieldDefn.GetName();
                var fieldType = fieldDefn.GetFieldType();
                var originalFieldName = fieldNameMapping[fieldName];

                if (feature.IsFieldNull(originalFieldName))
                {
                    continue;
                }

                if (fieldType == FieldType.OFTInteger)
                {
                    newFeature.SetField(fieldName, feature.GetFieldAsInteger(originalFieldName));
                }
                else if (fieldType == FieldType.OFTReal)
                {
                    newFeature.SetField(fieldName, feature.GetFieldAsDouble(originalFieldName));
                }
                else if (fieldType == FieldType.OFTDateTime)
                {
                    feature.GetFieldAsDateTime(originalFieldName, out var year, out var month, out var day, out var hour, out var minute, out var second, out var tzFlag);
                    newFeature.SetField(fieldName, year, month, day, hour, minute, second, tzFlag);
                }
                else
                {
                    newFeature.SetField(fieldName, feature.GetFieldAsString(originalFieldName));
                }
            }

            nutzungsflaechenLayer.CreateFeature(newFeature);
        }

        nutzungsflaechenLayer.ConvertMultiPartToSinglePartGeometry();

        // Delete the temporary work layers
        ProcessingDataSource.ExecuteSQL($"DROP TABLE {NutzungsflaechenJoinedLayerName}", null, "OGRSQL");
        ProcessingDataSource.ExecuteSQL($"DROP TABLE {NutzungsartLayerName}", null, "OGRSQL");
    }

    private async Task CreateNutzungsartLayerAsync()
    {
        var catalogData = await GetLnfKatalogNutzungsartAsync().ConfigureAwait(false);

        var nutzungsartLayer = ProcessingDataSource.CreateLayer(NutzungsartLayerName, null, wkbGeometryType.wkbNone, null);

        var lnfCodeName = "lnf_code";
        using var lnfCode = new FieldDefn(lnfCodeName, FieldType.OFTInteger);
        nutzungsartLayer.CreateField(lnfCode, 1);

        var istBffQiName = "ist_bff_qi";
        using var istBffQi = new FieldDefn(istBffQiName, FieldType.OFTInteger);
        istBffQi.SetSubType(FieldSubType.OFSTInt16);
        nutzungsartLayer.CreateField(istBffQi, 1);

        var hauptkategorieDeName = "hauptkategorie_de";
        using var hauptkategorieDe = new FieldDefn(hauptkategorieDeName, FieldType.OFTString);
        nutzungsartLayer.CreateField(hauptkategorieDe, 1);

        var hauptkategorieFrName = "hauptkategorie_fr";
        using var hauptkategorieFr = new FieldDefn(hauptkategorieFrName, FieldType.OFTString);
        nutzungsartLayer.CreateField(hauptkategorieFr, 1);

        var hauptkategorieItName = "hauptkategorie_it";
        using var hauptkategorieIt = new FieldDefn(hauptkategorieItName, FieldType.OFTString);
        nutzungsartLayer.CreateField(hauptkategorieIt, 1);

        var nutzungDeName = "nutzung_de";
        using var nutzungDe = new FieldDefn(nutzungDeName, FieldType.OFTString);
        nutzungsartLayer.CreateField(nutzungDe, 1);

        var nutzungFrName = "nutzung_fr";
        using var nutzungFr = new FieldDefn(nutzungFrName, FieldType.OFTString);
        nutzungsartLayer.CreateField(nutzungFr, 1);

        var nutzungItName = "nutzung_it";
        using var nutzungIt = new FieldDefn(nutzungItName, FieldType.OFTString);
        nutzungsartLayer.CreateField(nutzungIt, 1);

        catalogData.ForEach(entry =>
        {
            using var feature = new Feature(nutzungsartLayer.GetLayerDefn());
            feature.SetField(lnfCodeName, entry.LnfCode);
            feature.SetField(istBffQiName, entry.IstBFFQI ? 1 : 0);
            feature.SetField(hauptkategorieDeName, entry.Hauptkategorie.LocalisationCHV1MultilingualText.LocalisedText.LocalisationCHV1LocalisedText.FirstOrDefault(t => t.Language == "de").Text);
            feature.SetField(hauptkategorieFrName, entry.Hauptkategorie.LocalisationCHV1MultilingualText.LocalisedText.LocalisationCHV1LocalisedText.FirstOrDefault(t => t.Language == "fr").Text);
            feature.SetField(hauptkategorieItName, entry.Hauptkategorie.LocalisationCHV1MultilingualText.LocalisedText.LocalisationCHV1LocalisedText.FirstOrDefault(t => t.Language == "it").Text);
            feature.SetField(nutzungDeName, entry.Nutzung.LocalisationCHV1MultilingualText.LocalisedText.LocalisationCHV1LocalisedText.FirstOrDefault(t => t.Language == "de").Text);
            feature.SetField(nutzungFrName, entry.Nutzung.LocalisationCHV1MultilingualText.LocalisedText.LocalisationCHV1LocalisedText.FirstOrDefault(t => t.Language == "fr").Text);
            feature.SetField(nutzungItName, entry.Nutzung.LocalisationCHV1MultilingualText.LocalisedText.LocalisationCHV1LocalisedText.FirstOrDefault(t => t.Language == "it").Text);
            nutzungsartLayer.CreateFeature(feature);
        });
    }

    private async Task<List<LnfKatalogNutzungsart>> GetLnfKatalogNutzungsartAsync()
    {
        Logger.LogInformation($"Lade Nutzungsart-Katalog von {CatalogUrl}...");

        using var httpClient = new HttpClient();
        var xmlData = await httpClient.GetStringAsync(CatalogUrl).ConfigureAwait(false);

        using var stringReader = new StringReader(xmlData);
        using var xmlReader = XmlReader.Create(stringReader);
        var serializer = new XmlSerializer(typeof(Transfer));
        var deserializedObject = serializer.Deserialize(xmlReader);

        if (deserializedObject is Transfer catalog)
        {
            return catalog.DataSection.LnfKataloge.LnfKatalogNutzungsart;
        }
        else
        {
            Logger.LogError("Deserialisierung von Nutzungsart-Katalog fehlgeschlagen.");
            throw new InvalidOperationException("Deserialization failed or returned null.");
        }
    }
}
