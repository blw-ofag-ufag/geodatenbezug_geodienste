﻿using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_biodiversitaetsfoerderflaechen_v2_0_lv95_testdaten.gpkg", "testdata")]
public class BiodiversitaetsfoerderflaechenProcessorTest
{
    private readonly Topic topic = new()
    {
        TopicTitle = BaseTopic.lwb_biodiversitaetsfoerderflaechen.GetDescription(),
        Canton = Canton.NE,
        BaseTopic = BaseTopic.lwb_biodiversitaetsfoerderflaechen,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private Mock<IAzureStorage> azureStorageMock;
    private BiodiversitaetsfoerderflaechenProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        azureStorageMock = new Mock<IAzureStorage>(MockBehavior.Strict);
        processor = new BiodiversitaetsfoerderflaechenProcessor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object, topic);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task RunGdalProcessingAsync()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");

        processor.InputDataPath = "testdata\\lwb_biodiversitaetsfoerderflaechen_v2_0_lv95_testdaten.gpkg";
        await processor.RunGdalProcessingAsync();

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);

        var qualitaetLayerName = "bff_qualitaet_2_flaechen";
        var qualitaetInputLayer = inputSource.GetLayerByName(qualitaetLayerName);
        var qualitaetResultLayer = resultSource.GetLayerByName(qualitaetLayerName);

        var expectedQualitaetLayerFields = new List<string>
        {
            "t_id",
            "bezugsjahr",
            "anzahl_baeume",
            "ist_definitiv",
            "verpflichtung_von",
            "verpflichtung_bis",
            "schnittzeitpunkt",
            "bewirtschaftungsgrad",
            "beitragsberechtigt",
            "nhg",
            "qualitaetsanteil",
            "lnf_code",
            "identifikator",
            "flaeche_m2",
            "kanton",
        };
        GdalAssert.AssertLayerFields(qualitaetResultLayer, expectedQualitaetLayerFields);

        GdalAssert.AssertFieldType(qualitaetResultLayer, "t_id", FieldType.OFTString, 50);
        GdalAssert.AssertFieldType(qualitaetResultLayer, "bezugsjahr", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(qualitaetResultLayer, "verpflichtung_von", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(qualitaetResultLayer, "verpflichtung_bis", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(qualitaetResultLayer, "schnittzeitpunkt", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(qualitaetResultLayer, "ist_definitiv", FieldType.OFTInteger, FieldSubType.OFSTInt16);
        GdalAssert.AssertFieldType(qualitaetResultLayer, "beitragsberechtigt", FieldType.OFTInteger, FieldSubType.OFSTInt16);

        GdalAssert.AssertOnlyValidLnfCodes(qualitaetResultLayer);
        GdalAssert.AssertOnlySinglePartGeometries(qualitaetResultLayer);

        var t_ids = new List<int>();
        qualitaetResultLayer.ResetReading();
        for (var i = 0; i < qualitaetResultLayer.GetFeatureCount(1); i++)
        {
            var feature = qualitaetResultLayer.GetNextFeature();
            t_ids.Add(feature.GetFieldAsInteger("t_id"));
        }

        // Delete feature with invalid lnf_code
        Assert.AreEqual(0, t_ids.FindAll(t_id => t_id == 550616).Count);

        // Feature with multipart geometry was split into three features
        Assert.AreEqual(2, t_ids.FindAll(t_id => t_id == 584805).Count);

        // The first feature was deleted so we want to compare the second one
        qualitaetInputLayer.GetNextFeature();
        var firstQualitaetInputFeature = qualitaetInputLayer.GetNextFeature();
        qualitaetResultLayer.ResetReading();
        var firstQualitaetResultFeature = qualitaetResultLayer.GetNextFeature();
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("t_id"), firstQualitaetResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstQualitaetInputFeature, firstQualitaetResultFeature, "bezugsjahr");
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("anzahl_baeume"), firstQualitaetResultFeature.GetFieldAsInteger("anzahl_baeume"));
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("ist_definitiv"), firstQualitaetResultFeature.GetFieldAsInteger("ist_definitiv"));
        GdalAssert.AssertDateTime(firstQualitaetInputFeature, firstQualitaetResultFeature, "verpflichtung_von");
        GdalAssert.AssertDateTime(firstQualitaetInputFeature, firstQualitaetResultFeature, "verpflichtung_bis");
        GdalAssert.AssertDateTime(firstQualitaetInputFeature, firstQualitaetResultFeature, "schnittzeitpunkt");
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("bewirtschaftungsgrad"), firstQualitaetResultFeature.GetFieldAsInteger("bewirtschaftungsgrad"));
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("beitragsberechtigt"), firstQualitaetResultFeature.GetFieldAsInteger("beitragsberechtigt"));
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("nhg"), firstQualitaetResultFeature.GetFieldAsInteger("nhg"));
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("qualitaetsanteil"), firstQualitaetResultFeature.GetFieldAsInteger("qualitaetsanteil"));
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("lnf_code"), firstQualitaetResultFeature.GetFieldAsInteger("lnf_code"));
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsString("identifikator"), firstQualitaetResultFeature.GetFieldAsString("identifikator"));
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsInteger("flaeche_m2"), firstQualitaetResultFeature.GetFieldAsInteger("flaeche_m2"));
        Assert.AreEqual(firstQualitaetInputFeature.GetFieldAsString("kanton"), firstQualitaetResultFeature.GetFieldAsString("kanton"));
        GdalAssert.AssertGeometry(firstQualitaetInputFeature, firstQualitaetResultFeature);

        var vernetzungLayerName = "bff_vernetzung_flaechen";
        var vernetzungInputLayer = inputSource.GetLayerByName(vernetzungLayerName);
        var vernetzungResultLayer = resultSource.GetLayerByName(vernetzungLayerName);

        var expectedVernetzungLayerFields = new List<string>
        {
            "t_id",
            "bezugsjahr",
            "anzahl_baeume",
            "ist_definitiv",
            "verpflichtung_von",
            "verpflichtung_bis",
            "schnittzeitpunkt",
            "beitragsberechtigt",
            "lnf_code",
            "identifikator",
            "flaeche_m2",
            "kanton",
        };
        GdalAssert.AssertLayerFields(vernetzungResultLayer, expectedVernetzungLayerFields);

        GdalAssert.AssertFieldType(vernetzungResultLayer, "t_id", FieldType.OFTString, 50);
        GdalAssert.AssertFieldType(vernetzungResultLayer, "bezugsjahr", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(vernetzungResultLayer, "verpflichtung_von", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(vernetzungResultLayer, "verpflichtung_bis", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(vernetzungResultLayer, "schnittzeitpunkt", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(vernetzungResultLayer, "ist_definitiv", FieldType.OFTInteger, FieldSubType.OFSTInt16);
        GdalAssert.AssertFieldType(vernetzungResultLayer, "beitragsberechtigt", FieldType.OFTInteger, FieldSubType.OFSTInt16);

        GdalAssert.AssertOnlyValidLnfCodes(vernetzungResultLayer);
        GdalAssert.AssertOnlySinglePartGeometries(vernetzungResultLayer);
        t_ids = new List<int>();
        vernetzungResultLayer.ResetReading();
        for (var i = 0; i < vernetzungResultLayer.GetFeatureCount(1); i++)
        {
            var feature = vernetzungResultLayer.GetNextFeature();
            t_ids.Add(feature.GetFieldAsInteger("t_id"));
        }

        // Delete feature with invalid lnf_code
        Assert.AreEqual(0, t_ids.FindAll(t_id => t_id == 914572).Count);

        var firstVernetzungInputFeature = vernetzungInputLayer.GetNextFeature();
        vernetzungResultLayer.ResetReading();
        var firstVernetzungResultFeature = vernetzungResultLayer.GetNextFeature();
        var tid = firstVernetzungInputFeature.GetFieldAsInteger("t_id");
        Assert.AreEqual(firstVernetzungInputFeature.GetFieldAsInteger("t_id"), firstVernetzungResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstVernetzungInputFeature, firstVernetzungResultFeature, "bezugsjahr");
        Assert.AreEqual(firstVernetzungInputFeature.GetFieldAsInteger("anzahl_baeume"), firstVernetzungResultFeature.GetFieldAsInteger("anzahl_baeume"));
        Assert.AreEqual(firstVernetzungInputFeature.GetFieldAsInteger("ist_definitiv"), firstVernetzungResultFeature.GetFieldAsInteger("ist_definitiv"));
        GdalAssert.AssertDateTime(firstVernetzungInputFeature, firstVernetzungResultFeature, "verpflichtung_von");
        GdalAssert.AssertDateTime(firstVernetzungInputFeature, firstVernetzungResultFeature, "verpflichtung_bis");
        GdalAssert.AssertDateTime(firstVernetzungInputFeature, firstVernetzungResultFeature, "schnittzeitpunkt");
        Assert.AreEqual(firstVernetzungInputFeature.GetFieldAsInteger("beitragsberechtigt"), firstVernetzungResultFeature.GetFieldAsInteger("beitragsberechtigt"));
        Assert.AreEqual(firstVernetzungInputFeature.GetFieldAsInteger("lnf_code"), firstVernetzungResultFeature.GetFieldAsInteger("lnf_code"));
        Assert.AreEqual(firstVernetzungInputFeature.GetFieldAsString("identifikator"), firstVernetzungResultFeature.GetFieldAsString("identifikator"));
        Assert.AreEqual(firstVernetzungInputFeature.GetFieldAsInteger("flaeche_m2"), firstVernetzungResultFeature.GetFieldAsInteger("flaeche_m2"));
        Assert.AreEqual(firstVernetzungInputFeature.GetFieldAsString("kanton"), firstVernetzungResultFeature.GetFieldAsString("kanton"));
        GdalAssert.AssertGeometry(firstVernetzungInputFeature, firstVernetzungResultFeature);
    }
}
