using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_rebbaukataster_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
public class RebbaukatasterProcessorTest
{
    private readonly Topic topic = new ()
    {
        TopicTitle = BaseTopic.lwb_rebbaukataster.GetDescription(),
        Canton = Canton.NE,
        BaseTopic = BaseTopic.lwb_rebbaukataster,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private Mock<IAzureStorage> azureStorageMock;
    private RebbaukatasterProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        azureStorageMock = new Mock<IAzureStorage>(MockBehavior.Strict);
        processor = new RebbaukatasterProcessor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object, topic);
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

        processor.InputDataPath = "testdata\\lwb_rebbaukataster_v2_0_lv95_NE_202404191123.gpkg";
        await processor.RunGdalProcessingAsync();

        var layerName = "rebbaukataster";

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var inputLayer = inputSource.GetLayerByName(layerName);

        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);
        var resultLayer = resultSource.GetLayerByName(layerName);

        var expectedLayerFields = new List<string>
        {
            "t_id",
            "identifikator",
            "aenderungsdatum",
            "flaeche_m2",
            "kanton",
        };
        GdalAssert.AssertLayerFields(resultLayer, expectedLayerFields);

        GdalAssert.AssertFieldType(resultLayer, "t_id", FieldType.OFTInteger);
        GdalAssert.AssertFieldType(resultLayer, "aenderungsdatum", FieldType.OFTDateTime);

        GdalAssert.AssertOnlySinglePartGeometries(resultLayer);

        var firstInputFeature = inputLayer.GetNextFeature();
        resultLayer.ResetReading();
        var firstResultFeature = resultLayer.GetNextFeature();
        Assert.AreEqual(firstInputFeature.GetFID(), firstResultFeature.GetFieldAsInteger("t_id"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("identifikator"), firstResultFeature.GetFieldAsString("identifikator"));
        GdalAssert.AssertDateTime(firstInputFeature, firstResultFeature, "aenderungsdatum");
        Assert.AreEqual(firstInputFeature.GetFieldAsInteger("flaeche_m2"), firstResultFeature.GetFieldAsInteger("flaeche_m2"));
        Assert.AreEqual(firstInputFeature.GetFieldAsString("kanton"), firstResultFeature.GetFieldAsString("kanton"));
        GdalAssert.AssertGeometry(firstInputFeature, firstResultFeature);
    }
}
