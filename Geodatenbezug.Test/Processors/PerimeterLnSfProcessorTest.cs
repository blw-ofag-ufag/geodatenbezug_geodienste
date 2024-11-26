using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_testdaten.gpkg", "testdata")]
public class PerimeterLnSfProcessorTest
{
    private readonly Topic topic = new()
    {
        TopicTitle = BaseTopic.lwb_perimeter_ln_sf.GetDescription(),
        Canton = Canton.NE,
        BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private Mock<IAzureStorage> azureStorageMock;
    private PerimeterLnSfProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        azureStorageMock = new Mock<IAzureStorage>(MockBehavior.Strict);
        processor = new PerimeterLnSfProcessor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object, topic);
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

        processor.InputDataPath = "testdata\\lwb_perimeter_ln_sf_v2_0_lv95_testdaten.gpkg";
        await processor.RunGdalProcessingAsync();

        var layerName = "perimeter_ln_sf";

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var inputLayer = inputSource.GetLayerByName(layerName);

        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);
        var resultLayer = resultSource.GetLayerByName(layerName);

        var expectedLayerFields = new List<string>
        {
            "t_id",
            "bezugsjahr",
            "typ",
            "identifikator",
            "flaeche_m2",
            "kanton",
        };
        GdalAssert.AssertLayerFields(resultLayer, expectedLayerFields);

        GdalAssert.AssertFieldType(resultLayer, "t_id", FieldType.OFTString);
        GdalAssert.AssertFieldType(resultLayer, "bezugsjahr", FieldType.OFTDateTime);

        GdalAssert.AssertOnlySinglePartGeometries(resultLayer);
        Assert.AreEqual(2, inputLayer.GetFeatureCount(0));
        Assert.AreEqual(3, resultLayer.GetFeatureCount(0));

        var firstInputFeature = inputLayer.GetNextFeature();
        resultLayer.ResetReading();
        var firstResultFeature = resultLayer.GetNextFeature();
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("t_id"), firstResultFeature.GetFieldAsInteger("t_id"));
        GdalAssert.AssertDateTime(firstInputFeature, firstResultFeature, "bezugsjahr");
        Assert.AreEqual(firstInputFeature.GetFieldAsString("typ"), firstResultFeature.GetFieldAsString("typ"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("identifikator"), firstResultFeature.GetFieldAsString("identifikator"));
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("flaeche_m2"), firstResultFeature.GetFieldAsInteger("flaeche_m2"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("kanton"), firstResultFeature.GetFieldAsString("kanton"));
        GdalAssert.AssertGeometry(firstInputFeature, firstResultFeature);
    }
}
