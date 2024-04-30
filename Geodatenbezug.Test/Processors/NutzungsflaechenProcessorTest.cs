﻿using System.Net;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_nutzungsflaechen_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_bewirtschaftungseinheit_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
public class NutzungsflaechenProcessorTest
{
    private readonly Topic topic = new ()
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

        loggerMock.Setup(LogLevel.Information, $"Bereite Daten für die Prozessierung von {topic.TopicTitle} ({topic.Canton}) vor...");
        loggerMock.Setup(LogLevel.Information, $"Exportiere {topic.TopicTitle} ({topic.Canton})...");
        loggerMock.Setup(LogLevel.Information, $"Exportiere {bewirtschaftungseinheitTopic.TopicTitle} ({bewirtschaftungseinheitTopic.Canton})...");

        await processor.PrepareDataAsync();
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.StartExportAsync(bewirtschaftungseinheitTopic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(bewirtschaftungseinheitTopic), Times.Once);
    }

    [TestMethod]
    public async Task RunGdalProcessingAsync()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung von Thema {topic.TopicTitle} ({topic.Canton})...");

        processor.InputDataPath = "testdata\\lwb_nutzungsflaechen_v2_0_lv95_NE_202404191123.gpkg";
        processor.BewirtschaftungseinheitDataPath = "testdata\\lwb_bewirtschaftungseinheit_v2_0_lv95_NE_202404191123.gpkg";
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
                "ist_bff_qi",
                "hauptkategorie_de",
                "hauptkategorie_fr",
                "hauptkategorie_it",
                "nutzung_de",
                "nutzung_fr",
                "nutzung_it",
                "bewe_betriebsnummer",
            };
        GdalAssert.AssertLayerFields(resultLayer, expectedLayerFields);

        GdalAssert.AssertFieldType(resultLayer, "t_id", FieldType.OFTInteger);
        GdalAssert.AssertFieldType(resultLayer, "bezugsjahr", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(resultLayer, "ist_bff_qi", FieldType.OFTInteger, FieldSubType.OFSTInt16);

        GdalAssert.AssertOnlyValidLnfCodes(resultLayer);

        GdalAssert.AssertOnlySinglePartGeometries(resultLayer);

        var firstInputFeature = inputLayer.GetNextFeature();
        resultLayer.ResetReading();
        var firstResultFeature = resultLayer.GetNextFeature();

        Assert.AreEqual(firstInputFeature.GetFID(), firstResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstInputFeature, firstResultFeature, "bezugsjahr");
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("lnf_code"), firstResultFeature.GetFieldAsInteger("lnf_code"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("code_programm"), firstResultFeature.GetFieldAsString("code_programm"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("programm"), firstResultFeature.GetFieldAsString("programm"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("nutzungsidentifikator"), firstResultFeature.GetFieldAsString("nutzungsidentifikator"));
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("bewirtschaftungsgrad"), firstResultFeature.GetFieldAsInteger("bewirtschaftungsgrad"));
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("flaeche_m2"), firstResultFeature.GetFieldAsInteger("flaeche_m2"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("kanton"), firstResultFeature.GetFieldAsString("kanton"));
        Assert.AreEqual(0, firstResultFeature.GetFieldAsInteger("ist_bff_qi"));
        Assert.AreEqual("Ackerfläche", firstResultFeature.GetFieldAsString("hauptkategorie_de"));
        Assert.AreEqual("Terres cultivées", firstResultFeature.GetFieldAsString("hauptkategorie_fr"));
        Assert.AreEqual("Superficie coltiva", firstResultFeature.GetFieldAsString("hauptkategorie_it"));
        Assert.AreEqual("Sommergerste", firstResultFeature.GetFieldAsString("nutzung_de"));
        Assert.AreEqual("Orge de printemps", firstResultFeature.GetFieldAsString("nutzung_fr"));
        Assert.AreEqual("Orzo primaverile", firstResultFeature.GetFieldAsString("nutzung_it"));
        Assert.AreEqual("NE65020007", firstResultFeature.GetFieldAsString("bewe_betriebsnummer"));
        GdalAssert.AssertGeometry(firstInputFeature, firstResultFeature);
    }
}
