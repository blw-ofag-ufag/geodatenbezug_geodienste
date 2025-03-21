﻿using System.Net;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_nutzungsflaechen_v2_0_lv95_testdaten.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_bewirtschaftungseinheit_v2_0_lv95_testdaten.gpkg", "testdata")]
public class NutzungsflaechenProcessorTest
{
    private readonly Topic topic = new()
    {
        TopicTitle = BaseTopic.lwb_nutzungsflaechen.GetDescription(),
        Canton = Canton.NE,
        BaseTopic = BaseTopic.lwb_nutzungsflaechen,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private Mock<IAzureStorage> azureStorageMock;
    private NutzungsflaechenProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        azureStorageMock = new Mock<IAzureStorage>(MockBehavior.Strict);
        processor = new NutzungsflaechenProcessor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object, topic);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task PrepareDataAsync()
    {
        var bewirtschaftungseinheitTopic = new Topic()
        {
            TopicTitle = BaseTopic.lwb_bewirtschaftungseinheit.GetDescription(),
            Canton = topic.Canton,
            BaseTopic = BaseTopic.lwb_bewirtschaftungseinheit,
        };

        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"success\", \"info\":\"Data ready to be downloaded. Provide your credentials to download the data.\", \"download_url\":\"test.com/data.zip\", \"exported_at\":\"2022-03-24T09:31:05.508\"}"), });
        geodiensteApiMock
            .Setup(api => api.DownloadExportAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("downloadedFilePath");

        loggerMock.Setup(LogLevel.Information, $"Bereite Daten für die Prozessierung vor");
        loggerMock.Setup(LogLevel.Information, $"Export", Times.Exactly(2));

        await processor.PrepareDataAsync();
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.StartExportAsync(bewirtschaftungseinheitTopic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(bewirtschaftungseinheitTopic), Times.Once);
    }

    [TestMethod]
    public async Task RunGdalProcessingAsync()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");
        loggerMock.Setup(LogLevel.Information, $"Kopiere Features aus dem GPKG in die GDB");
        loggerMock.Setup(LogLevel.Information, $"Lade Nutzungsart-Katalog von https://models.geo.admin.ch/BLW/LWB_Nutzungsflaechen_Kataloge_V3_0.xml");
        loggerMock.Setup(LogLevel.Information, $"Erstelle temporären Nutzungsartlayer");
        loggerMock.Setup(LogLevel.Information, $"Erstelle temporären Bewirtschaftungslayer");
        loggerMock.Setup(LogLevel.Information, $"Führe Join mit Nutzungsart und Bewirtschaftungseinheit aus");
        loggerMock.Setup(LogLevel.Information, $"Lösche initialen Nutzungsflächenlayer");
        loggerMock.Setup(LogLevel.Information, $"Erstelle neuen Nutzungsflächenlayer");
        loggerMock.Setup(LogLevel.Information, $"Kopiere Features vom Joined Layer zum neuen Nutzungsflächenlayer");
        loggerMock.Setup(LogLevel.Information, $"Lösche temporäre Layer");

        processor.InputDataPath = "testdata\\lwb_nutzungsflaechen_v2_0_lv95_testdaten.gpkg";
        processor.BewirtschaftungseinheitDataPath = "testdata\\lwb_bewirtschaftungseinheit_v2_0_lv95_testdaten.gpkg";
        await processor.RunGdalProcessingAsync();

        var layerName = "nutzungsflaechen";

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var inputLayer = inputSource.GetLayerByName(layerName);

        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);
        Assert.AreEqual(1, resultSource.GetLayerCount());

        var resultLayer = resultSource.GetLayerByName(layerName);

        var expectedLayerFields = new List<string>
        {
            "t_id",
            "bezugsjahr",
            "lnf_code",
            "code_programm",
            "programm",
            "nutzungsidentifikator",
            "bewirtschaftungsgrad",
            "flaeche_m2",
            "kanton",
            "bff_qualitaet_1",
            "hauptkategorie_de",
            "hauptkategorie_fr",
            "hauptkategorie_it",
            "nutzung_de",
            "nutzung_fr",
            "nutzung_it",
            "betriebsnummer",
            "bur_nr",
        };
        GdalAssert.AssertLayerFields(resultLayer, expectedLayerFields);

        GdalAssert.AssertFieldType(resultLayer, "t_id", FieldType.OFTString, 50);
        GdalAssert.AssertFieldType(resultLayer, "bezugsjahr", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(resultLayer, "bff_qualitaet_1", FieldType.OFTInteger, FieldSubType.OFSTInt16);
        GdalAssert.AssertFieldType(resultLayer, "code_programm", FieldType.OFTString, 999);
        GdalAssert.AssertFieldType(resultLayer, "programm", FieldType.OFTString, 999);
        GdalAssert.AssertFieldType(resultLayer, "hauptkategorie_de", FieldType.OFTString, 999);
        GdalAssert.AssertFieldType(resultLayer, "hauptkategorie_fr", FieldType.OFTString, 999);
        GdalAssert.AssertFieldType(resultLayer, "hauptkategorie_it", FieldType.OFTString, 999);
        GdalAssert.AssertFieldType(resultLayer, "nutzung_de", FieldType.OFTString, 999);
        GdalAssert.AssertFieldType(resultLayer, "nutzung_fr", FieldType.OFTString, 999);
        GdalAssert.AssertFieldType(resultLayer, "nutzung_it", FieldType.OFTString, 999);

        GdalAssert.AssertOnlyValidLnfCodes(resultLayer);
        GdalAssert.AssertOnlySinglePartGeometries(resultLayer);

        var t_ids = new List<int>();
        resultLayer.ResetReading();
        for (var i = 0; i < resultLayer.GetFeatureCount(1); i++)
        {
            var feature = resultLayer.GetNextFeature();
            t_ids.Add(feature.GetFieldAsInteger("t_id"));
        }

        // Delete feature with invalid lnf_code
        Assert.AreEqual(0, t_ids.FindAll(t_id => t_id == 6225522).Count);

        // Feature with multipart geometry was split into three features
        Assert.AreEqual(3, t_ids.FindAll(t_id => t_id == 6225610).Count);

        var firstInputFeature = inputLayer.GetNextFeature();
        resultLayer.ResetReading();
        var firstResultFeature = resultLayer.GetNextFeature();

        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("t_id"), firstResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstInputFeature, firstResultFeature, "bezugsjahr");
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("lnf_code"), firstResultFeature.GetFieldAsInteger("lnf_code"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("code_programm"), firstResultFeature.GetFieldAsString("code_programm"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("programm"), firstResultFeature.GetFieldAsString("programm"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("nutzungsidentifikator"), firstResultFeature.GetFieldAsString("nutzungsidentifikator"));
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("bewirtschaftungsgrad"), firstResultFeature.GetFieldAsInteger("bewirtschaftungsgrad"));
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("flaeche_m2"), firstResultFeature.GetFieldAsInteger("flaeche_m2"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("kanton"), firstResultFeature.GetFieldAsString("kanton"));
        Assert.AreEqual(0, firstResultFeature.GetFieldAsInteger("bff_qualitaet_1"));
        Assert.AreEqual("Reben", firstResultFeature.GetFieldAsString("hauptkategorie_de"));
        Assert.AreEqual("Vignobles", firstResultFeature.GetFieldAsString("hauptkategorie_fr"));
        Assert.AreEqual("Vigna", firstResultFeature.GetFieldAsString("hauptkategorie_it"));
        Assert.AreEqual("Reben", firstResultFeature.GetFieldAsString("nutzung_de"));
        Assert.AreEqual("Vignes", firstResultFeature.GetFieldAsString("nutzung_fr"));
        Assert.AreEqual("Vigna", firstResultFeature.GetFieldAsString("nutzung_it"));
        Assert.AreEqual("BEB102228", firstResultFeature.GetFieldAsString("betriebsnummer"));
        Assert.AreEqual(string.Empty, firstResultFeature.GetFieldAsString("bur_nr"));
        GdalAssert.AssertGeometry(firstInputFeature, firstResultFeature);
    }
}
