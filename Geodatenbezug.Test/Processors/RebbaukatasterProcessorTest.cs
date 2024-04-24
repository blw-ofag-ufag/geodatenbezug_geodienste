using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

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
        processor.InputDataPath = "testdata\\lwb_rebbaukataster_v2_0_lv95_NE_202404191123.gpkg";
        await processor.RunGdalProcessingAsync();
    }
}
