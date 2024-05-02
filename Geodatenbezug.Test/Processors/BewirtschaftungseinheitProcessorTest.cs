using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_bewirtschaftungseinheit_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
public class BewirtschaftungseinheitProcessorTest
{
    private readonly Topic topic = new ()
    {
        TopicTitle = BaseTopic.lwb_bewirtschaftungseinheit.GetDescription(),
        Canton = Canton.NE,
        BaseTopic = BaseTopic.lwb_bewirtschaftungseinheit,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private Mock<IAzureStorage> azureStorageMock;
    private BewirtschaftungseinheitProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        azureStorageMock = new Mock<IAzureStorage>(MockBehavior.Strict);
        processor = new BewirtschaftungseinheitProcessor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object, topic);
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

        processor.InputDataPath = "testdata\\lwb_bewirtschaftungseinheit_v2_0_lv95_NE_202404191123.gpkg";
        await processor.RunGdalProcessingAsync();

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);

        var betriebLayerName = "betrieb";
        var betriebInputLayer = inputSource.GetLayerByName(betriebLayerName);
        var betriebResultLayer = resultSource.GetLayerByName(betriebLayerName);

        var expectedBetriebLayerFields = new List<string>
        {
            "t_id",
            "bezugsjahr",
            "betriebsnummer",
            "betriebsname",
            "bur_nr",
            "kanton",
        };
        GdalAssert.AssertLayerFields(betriebResultLayer, expectedBetriebLayerFields);

        GdalAssert.AssertFieldType(betriebResultLayer, "t_id", FieldType.OFTInteger);
        GdalAssert.AssertFieldType(betriebResultLayer, "bezugsjahr", FieldType.OFTDateTime);

        GdalAssert.AssertOnlySinglePartGeometries(betriebResultLayer);

        var firstBetriebInputFeature = betriebInputLayer.GetNextFeature();
        betriebResultLayer.ResetReading();
        var firstBetriebResultFeature = betriebResultLayer.GetNextFeature();
        Assert.AreEqual(firstBetriebInputFeature.GetFID(), firstBetriebResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstBetriebInputFeature, firstBetriebResultFeature, "bezugsjahr");
        Assert.AreEqual(firstBetriebInputFeature.GetFieldAsString("betriebsnummer"), firstBetriebResultFeature.GetFieldAsString("betriebsnummer"));
        Assert.AreEqual(firstBetriebInputFeature.GetFieldAsString("betriebsname"), firstBetriebResultFeature.GetFieldAsString("betriebsname"));
        Assert.AreEqual(firstBetriebInputFeature.GetFieldAsString("bur_nr"), firstBetriebResultFeature.GetFieldAsString("bur_nr"));
        Assert.AreEqual(firstBetriebInputFeature.GetFieldAsString("kanton"), firstBetriebResultFeature.GetFieldAsString("kanton"));
        GdalAssert.AssertGeometry(firstBetriebInputFeature, firstBetriebResultFeature);

        var bewirtschaftungseinheitLayerName = "bewirtschaftungseinheit";
        var bewirtschaftungseinheitInputLayer = inputSource.GetLayerByName(bewirtschaftungseinheitLayerName);
        var bewirtschaftungseinheitResultLayer = resultSource.GetLayerByName(bewirtschaftungseinheitLayerName);

        var expectedBewirtschaftungseinheitLayerFields = new List<string>
        {
            "t_id",
            "bezugsjahr",
            "ist_definitiv",
            "betriebsnummer",
            "ps_nr",
            "bur_nr",
            "gemeinde",
            "zone_ausland",
            "flaeche_m2",
            "kanton",
        };
        GdalAssert.AssertLayerFields(bewirtschaftungseinheitResultLayer, expectedBewirtschaftungseinheitLayerFields);

        GdalAssert.AssertFieldType(bewirtschaftungseinheitResultLayer, "t_id", FieldType.OFTInteger);
        GdalAssert.AssertFieldType(bewirtschaftungseinheitResultLayer, "bezugsjahr", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(bewirtschaftungseinheitResultLayer, "ist_definitiv", FieldType.OFTInteger, FieldSubType.OFSTInt16);

        GdalAssert.AssertOnlySinglePartGeometries(bewirtschaftungseinheitResultLayer);

        var firstBewirtschaftungseinheitInputFeature = bewirtschaftungseinheitInputLayer.GetNextFeature();
        bewirtschaftungseinheitResultLayer.ResetReading();
        var firstBewirtschaftungseinheitResultFeature = bewirtschaftungseinheitResultLayer.GetNextFeature();
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFID(), firstBewirtschaftungseinheitResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstBewirtschaftungseinheitInputFeature, firstBewirtschaftungseinheitResultFeature, "bezugsjahr");
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFieldAsInteger("ist_definitiv"), firstBewirtschaftungseinheitResultFeature.GetFieldAsInteger("ist_definitiv"));
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFieldAsString("betriebsnummer"), firstBewirtschaftungseinheitResultFeature.GetFieldAsString("betriebsnummer"));
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFieldAsString("ps_nr"), firstBewirtschaftungseinheitResultFeature.GetFieldAsString("ps_nr"));
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFieldAsString("bur_nr"), firstBewirtschaftungseinheitResultFeature.GetFieldAsString("bur_nr"));
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFieldAsString("gemeinde"), firstBewirtschaftungseinheitResultFeature.GetFieldAsString("gemeinde"));
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFieldAsString("zone_ausland"), firstBewirtschaftungseinheitResultFeature.GetFieldAsString("zone_ausland"));
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFieldAsInteger("flaeche_m2"), firstBewirtschaftungseinheitResultFeature.GetFieldAsInteger("flaeche_m2"));
        Assert.AreEqual(firstBewirtschaftungseinheitInputFeature.GetFieldAsString("kanton"), firstBewirtschaftungseinheitResultFeature.GetFieldAsString("kanton"));
        GdalAssert.AssertGeometry(firstBewirtschaftungseinheitInputFeature, firstBewirtschaftungseinheitResultFeature);

        var produktionsstaetteLayerName = "produktionsstaette";
        var produktionsstaetteInputLayer = inputSource.GetLayerByName(produktionsstaetteLayerName);
        var produktionsstaetteResultLayer = resultSource.GetLayerByName(produktionsstaetteLayerName);

        var expectedProduktionsstaetteLayerFields = new List<string>
        {
            "t_id",
            "bezugsjahr",
            "ps_nr",
            "ps_name",
            "betriebsnummer",
            "bur_nr",
            "kanton",
        };
        GdalAssert.AssertLayerFields(produktionsstaetteResultLayer, expectedProduktionsstaetteLayerFields);

        GdalAssert.AssertFieldType(produktionsstaetteResultLayer, "t_id", FieldType.OFTInteger);
        GdalAssert.AssertFieldType(produktionsstaetteResultLayer, "bezugsjahr", FieldType.OFTDateTime);

        GdalAssert.AssertOnlySinglePartGeometries(produktionsstaetteResultLayer);

        var firstProduktionsstaetteInputFeature = produktionsstaetteInputLayer.GetNextFeature();
        produktionsstaetteResultLayer.ResetReading();
        var firstProduktionsstaetteResultFeature = produktionsstaetteResultLayer.GetNextFeature();
        Assert.AreEqual(firstProduktionsstaetteInputFeature.GetFID(), firstProduktionsstaetteResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstProduktionsstaetteInputFeature, firstProduktionsstaetteResultFeature, "bezugsjahr");
        Assert.AreEqual(firstProduktionsstaetteInputFeature.GetFieldAsString("ps_nr"), firstProduktionsstaetteResultFeature.GetFieldAsString("ps_nr"));
        Assert.AreEqual(firstProduktionsstaetteInputFeature.GetFieldAsString("ps_name"), firstProduktionsstaetteResultFeature.GetFieldAsString("ps_name"));
        Assert.AreEqual(firstProduktionsstaetteInputFeature.GetFieldAsString("betriebsnummer"), firstProduktionsstaetteResultFeature.GetFieldAsString("betriebsnummer"));
        Assert.AreEqual(firstProduktionsstaetteInputFeature.GetFieldAsString("bur_nr"), firstProduktionsstaetteResultFeature.GetFieldAsString("bur_nr"));
        Assert.AreEqual(firstProduktionsstaetteInputFeature.GetFieldAsString("kanton"), firstProduktionsstaetteResultFeature.GetFieldAsString("kanton"));
        GdalAssert.AssertGeometry(firstProduktionsstaetteInputFeature, firstProduktionsstaetteResultFeature);
    }
}
