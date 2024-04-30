using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_perimeter_terrassenreben_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
public class PerimeterTerrassenrebenProcessorTest
{
    private readonly Topic topic = new ()
    {
        TopicTitle = BaseTopic.lwb_perimeter_terrassenreben.GetDescription(),
        Canton = Canton.NE,
        BaseTopic = BaseTopic.lwb_perimeter_terrassenreben,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private Mock<IAzureStorage> azureStorageMock;
    private PerimeterTerrassenrebenProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        azureStorageMock = new Mock<IAzureStorage>(MockBehavior.Strict);
        processor = new PerimeterTerrassenrebenProcessor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object, topic);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task RunGdalProcessingAsync()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung von Thema {topic.TopicTitle} ({topic.Canton})...");

        processor.InputDataPath = "testdata\\lwb_perimeter_terrassenreben_v2_0_lv95_NE_202404191123.gpkg";
        await processor.RunGdalProcessingAsync();

        var layerName = "perimeter_terrassenreben";

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var inputLayer = inputSource.GetLayerByName(layerName);

        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);
        var resultLayer = resultSource.GetLayerByName(layerName);

        var expectedLayerFields = new List<string>
        {
            "t_id",
            "bezugsjahr",
            "aenderungsdatum",
            "identifikator",
            "flaeche_m2",
            "kanton",
        };
        GdalAssert.AssertLayerFields(resultLayer, expectedLayerFields);

        GdalAssert.AssertFieldType(resultLayer, "t_id", FieldType.OFTInteger);
        GdalAssert.AssertFieldType(resultLayer, "bezugsjahr", FieldType.OFTDateTime);
        GdalAssert.AssertFieldType(resultLayer, "aenderungsdatum", FieldType.OFTDateTime);

        GdalAssert.AssertOnlySinglePartGeometries(resultLayer);

        var firstInputFeature = inputLayer.GetNextFeature();
        resultLayer.ResetReading();
        var firstResultFeature = resultLayer.GetNextFeature();
        Assert.AreEqual(firstInputFeature.GetFID(), firstResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstInputFeature, firstResultFeature, "bezugsjahr");
        GdalAssert.AssertDateTime(firstInputFeature, firstResultFeature, "aenderungsdatum");
        Assert.AreEqual(firstInputFeature.GetFieldAsString("identifikator"), firstResultFeature.GetFieldAsString("identifikator"));
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("flaeche_m2"), firstResultFeature.GetFieldAsInteger("flaeche_m2"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("kanton"), firstResultFeature.GetFieldAsString("kanton"));
        GdalAssert.AssertGeometry(firstInputFeature, firstResultFeature);
    }
}
