using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using OSGeo.OGR;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_testdaten_invalid_donut_valid_polygon.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_testdaten_invalid_wo_donut.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_testdaten_valid_donut_invalid_polygon.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_testdaten_valid_w_donut.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_testdaten_valid_wo_donut.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_testdaten_valid_singlepart_donut.gpkg", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_ln_sf_v2_0_lv95_testdaten_invalid_singlepart_donut.gpkg", "testdata")]
public class GeometryTest
{
    private readonly Topic topic = new ()
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
    public async Task RunGdalProcessingAsyncValidMultiPartWithoutDonut()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");

        processor.InputDataPath = "testdata\\lwb_perimeter_ln_sf_v2_0_lv95_testdaten_valid_wo_donut.gpkg";
        await processor.RunGdalProcessingAsync();

        var layerName = "perimeter_ln_sf";

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var inputLayer = inputSource.GetLayerByName(layerName);

        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);
        var resultLayer = resultSource.GetLayerByName(layerName);

        Assert.AreEqual(1, inputLayer.GetFeatureCount(0));
        Assert.AreEqual(2, resultLayer.GetFeatureCount(0));
    }

    [TestMethod]
    public async Task RunGdalProcessingAsyncInvalidMultiPartWithoutDonut()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");

        processor.InputDataPath = "testdata\\lwb_perimeter_ln_sf_v2_0_lv95_testdaten_invalid_wo_donut.gpkg";
        await Assert.ThrowsExceptionAsync<InvalidGeometryException>(processor.RunGdalProcessingAsync);
    }

    [TestMethod]
    public async Task RunGdalProcessingAsyncValidMultiPartWithDonuts()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");

        processor.InputDataPath = "testdata\\lwb_perimeter_ln_sf_v2_0_lv95_testdaten_valid_w_donut.gpkg";
        await processor.RunGdalProcessingAsync();

        var layerName = "perimeter_ln_sf";

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var inputLayer = inputSource.GetLayerByName(layerName);

        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);
        var resultLayer = resultSource.GetLayerByName(layerName);

        Assert.AreEqual(2, inputLayer.GetFeatureCount(0));
        Assert.AreEqual(3, resultLayer.GetFeatureCount(0));
    }

    [TestMethod]
    public async Task RunGdalProcessingAsyncValidSinglePartDonut()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");

        processor.InputDataPath = "testdata\\lwb_perimeter_ln_sf_v2_0_lv95_testdaten_valid_singlepart_donut.gpkg";
        await processor.RunGdalProcessingAsync();

        var layerName = "perimeter_ln_sf";

        var inputSource = Ogr.Open(processor.InputDataPath, 0);
        var inputLayer = inputSource.GetLayerByName(layerName);

        var resultSource = Ogr.Open(processor.InputDataPath.Replace(".gpkg", ".gdb", StringComparison.InvariantCulture), 0);
        var resultLayer = resultSource.GetLayerByName(layerName);

        Assert.AreEqual(1, inputLayer.GetFeatureCount(0));
        Assert.AreEqual(1, resultLayer.GetFeatureCount(0));
    }

    [TestMethod]
    public async Task RunGdalProcessingAsyncInvalidSinglePartDonut()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");

        processor.InputDataPath = "testdata\\lwb_perimeter_ln_sf_v2_0_lv95_testdaten_invalid_singlepart_donut.gpkg";
        await Assert.ThrowsExceptionAsync<InvalidGeometryException>(processor.RunGdalProcessingAsync);
    }

    [TestMethod]
    public async Task RunGdalProcessingAsyncInvalidDonutValidPolygon()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");

        processor.InputDataPath = "testdata\\lwb_perimeter_ln_sf_v2_0_lv95_testdaten_invalid_donut_valid_polygon.gpkg";
        await Assert.ThrowsExceptionAsync<InvalidGeometryException>(processor.RunGdalProcessingAsync);
    }

    [TestMethod]
    public async Task RunGdalProcessingAsyncValidDonutInvalidPolygon()
    {
        loggerMock.Setup(LogLevel.Information, $"Starte GDAL-Prozessierung");

        processor.InputDataPath = "testdata\\lwb_perimeter_ln_sf_v2_0_lv95_testdaten_valid_donut_invalid_polygon.gpkg";
        await Assert.ThrowsExceptionAsync<InvalidGeometryException>(processor.RunGdalProcessingAsync);
    }
}
