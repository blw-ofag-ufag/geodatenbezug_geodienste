using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug.Processors;

[TestClass]
[DeploymentItem("testdata/lwb_perimeter_terrassenreben_v2_0_lv95_NE_202404191123.gpkg", "testdata")]
public class PerimeterTerrassenrebenProcessorTest
{
    private readonly Topic topic = new()
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
    public async Task PrepareDataAsync()
    {
        await processor.RunGdalProcessing();
    }
}

