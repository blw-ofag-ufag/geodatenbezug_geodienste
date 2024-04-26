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
    private const string CatalogUrl = "https://models.geo.admin.ch/BLW/LWB_Nutzungsflaechen_Kataloge_V2_0.xml";
    private string bewirtschaftungseinheitDataPath = string.Empty;

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
        bewirtschaftungseinheitDataPath = downloadUrls[1];
    }

    private async Task<string> PrepareTopic(Topic topic)
    {
        var downloadUrl = await ExportTopicAsync(topic).ConfigureAwait(false);
        return await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected async override Task ProcessTopic()
    {
        var fieldTypeConversions = new Dictionary<string, FieldType>
        {
            { "bezugsjahr", FieldType.OFTDateTime },
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

        var nutzungsflaechenTempLayer = CreateGdalLayer("nutzungsflaechen", fieldTypeConversions, fieldsToDrop);
        nutzungsflaechenTempLayer.CopyFeatures();
        nutzungsflaechenTempLayer.FilterLnfCodes();

        await CreateNutzungsartLayerAsync().ConfigureAwait(false);

        var joinQuery = @$"
            SELECT nutzungsflaechen.*, nutzungsart.*, bewirtschaftungseinheit.betriebsnummer
            FROM nutzungsflaechen
            LEFT JOIN nutzungsart ON nutzungsflaechen.lnf_code = nutzungsart.lnf_code
            LEFT JOIN '{bewirtschaftungseinheitDataPath}'.bewirtschaftungseinheit ON nutzungsflaechen.identifikator_be = bewirtschaftungseinheit.identifikator_be";
        var tmpLayer = ProcessingDataSource.ExecuteSQL(joinQuery, null, "OGRSQL");
        ProcessingDataSource.CopyLayer(tmpLayer, "nutzungsflaechen_joined", null);

        ProcessingDataSource.DeleteLayer(0);

        var nutzungsflaechenJoinedLayer = ProcessingDataSource.GetLayerByName("nutzungsflaechen_joined");
        var nutzungsflaechenLayer = ProcessingDataSource.CreateLayer("nutzungsflaechen", null, nutzungsflaechenJoinedLayer.GetGeomType(), null);
        var fieldNameMapping = new Dictionary<string, string>();
        for (var i = 0; i < nutzungsflaechenJoinedLayer.GetLayerDefn().GetFieldCount(); i++)
        {
            var fieldDefn = nutzungsflaechenJoinedLayer.GetLayerDefn().GetFieldDefn(i);
            var originalFieldName = fieldDefn.GetName();
            string fieldName = originalFieldName;
            if (originalFieldName == "nutzungsart_lnf_code" || originalFieldName == "nutzungsflaechen_identifikator_be")
            {
                continue;
            }

            if (originalFieldName.Contains("nutzungsflaechen", StringComparison.CurrentCulture))
            {
                fieldName = originalFieldName.Replace("nutzungsflaechen_", string.Empty, StringComparison.CurrentCulture);
            }

            if (originalFieldName.Contains("nutzungsart", StringComparison.CurrentCulture))
            {
                fieldName = originalFieldName.Replace("nutzungsart_", string.Empty, StringComparison.CurrentCulture);
            }

            if (originalFieldName == "bewirtschaftungseinheit_betriebsnummer")
            {
                fieldName = "bewe_betriebsnummer";
            }

            fieldNameMapping[fieldName] = originalFieldName;

            var newFieldDefinition = new FieldDefn(fieldName, fieldDefn.GetFieldType());
            newFieldDefinition.SetWidth(fieldDefn.GetWidth());
            newFieldDefinition.SetPrecision(fieldDefn.GetPrecision());
            nutzungsflaechenLayer.CreateField(newFieldDefinition, 1);
            newFieldDefinition.Dispose();
        }

        nutzungsflaechenJoinedLayer.ResetReading();
        for (var i = 0; i < nutzungsflaechenJoinedLayer.GetFeatureCount(1); i++)
        {
            var feature = nutzungsflaechenJoinedLayer.GetNextFeature();
            var newFeature = new Feature(nutzungsflaechenLayer.GetLayerDefn());
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
            newFeature.Dispose();
        }

        nutzungsflaechenLayer.ConvertMultiPartToSinglePartGeometry();

        ProcessingDataSource.ExecuteSQL("DROP TABLE nutzungsflaechen_joined", null, "OGRSQL");
        ProcessingDataSource.ExecuteSQL("DROP TABLE nutzungsart", null, "OGRSQL");
    }

    private async Task CreateNutzungsartLayerAsync()
    {
        var catalogData = await GetLnfKatalogNutzungsartAsync().ConfigureAwait(false);

        var nutzungsartLayer = ProcessingDataSource.CreateLayer("nutzungsart", null, wkbGeometryType.wkbNone, null);

        var lnfCodeName = "lnf_code";
        var lnfCode = new FieldDefn(lnfCodeName, FieldType.OFTInteger);
        nutzungsartLayer.CreateField(lnfCode, 1);
        lnfCode.Dispose();

        // TODO: Check suptype to type FieldSubType.OFSTBoolean
        var istBffQiName = "ist_bff_qi";
        var istBffQi = new FieldDefn(istBffQiName, FieldType.OFTInteger);
        nutzungsartLayer.CreateField(istBffQi, 1);
        istBffQi.Dispose();

        var hauptkategorieDeName = "hauptkategorie_de";
        var hauptkategorieDe = new FieldDefn(hauptkategorieDeName, FieldType.OFTString);
        nutzungsartLayer.CreateField(hauptkategorieDe, 1);
        hauptkategorieDe.Dispose();

        var hauptkategorieFrName = "hauptkategorie_fr";
        var hauptkategorieFr = new FieldDefn(hauptkategorieFrName, FieldType.OFTString);
        nutzungsartLayer.CreateField(hauptkategorieFr, 1);
        hauptkategorieFr.Dispose();

        var hauptkategorieItName = "hauptkategorie_it";
        var hauptkategorieIt = new FieldDefn(hauptkategorieItName, FieldType.OFTString);
        nutzungsartLayer.CreateField(hauptkategorieIt, 1);
        hauptkategorieIt.Dispose();

        var nutzungDeName = "nutzung_de";
        using var nutzungDe = new FieldDefn(nutzungDeName, FieldType.OFTString);
        nutzungsartLayer.CreateField(nutzungDe, 1);

        var nutzungFrName = "nutzung_fr";
        var nutzungFr = new FieldDefn(nutzungFrName, FieldType.OFTString);
        nutzungsartLayer.CreateField(nutzungFr, 1);
        nutzungFr.Dispose();

        var nutzungItName = "nutzung_it";
        var nutzungIt = new FieldDefn(nutzungItName, FieldType.OFTString);
        nutzungsartLayer.CreateField(nutzungIt, 1);
        nutzungIt.Dispose();

        catalogData.ForEach(entry =>
        {
            using var feature = new Feature(nutzungsartLayer.GetLayerDefn());
            feature.SetField(lnfCodeName, entry.LNFCode);
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
        using var httpClient = new HttpClient();
        var xmlData = await httpClient.GetStringAsync(CatalogUrl).ConfigureAwait(false);

        using var stringReader = new StringReader(xmlData);
        using var xmlReader = XmlReader.Create(stringReader);
        var serializer = new XmlSerializer(typeof(Transfer));
        var deserializedObject = serializer.Deserialize(xmlReader);

        if (deserializedObject is Transfer catalog)
        {
            return catalog.DataSection.LNFKataloge.LNFKatalogNutzungsart;
        }
        else
        {
            throw new InvalidOperationException("Deserialization failed or returned null.");
        }
    }
}
