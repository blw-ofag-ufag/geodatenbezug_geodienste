using System.Net;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug.Processors;

[TestClass]
public class NutzungsflaechenProcessorTest
{
    private readonly Topic topic = new ()
    {
        TopicTitle = BaseTopic.lwb_nutzungsflaechen.GetDescription(),
        Canton = Canton.AG,
        TopicName = BaseTopic.lwb_nutzungsflaechen.ToString() + "_v2_0",
        BaseTopic = BaseTopic.lwb_nutzungsflaechen,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private NutzungsflaechenProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        processor = new NutzungsflaechenProcessor(geodiensteApiMock.Object, loggerMock.Object, topic);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task ProcessTopicCheckExportStatusError()
    {
        var bewirtschaftungseinheitTopic = new Topic()
        {
            TopicTitle = BaseTopic.lwb_bewirtschaftungseinheit.GetDescription(),
            Canton = topic.Canton,
            TopicName = BaseTopic.lwb_bewirtschaftungseinheit.ToString() + "_v2_0",
            BaseTopic = BaseTopic.lwb_bewirtschaftungseinheit,
        };

        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.NotFound,
            Reason = "Not Found",
            Info = "Data export information not found. Invalid token?",
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"success\", \"info\":\"Data ready to be downloaded. Provide your credentials to download the data.\", \"download_url\":\"test.com/data.zip\", \"exported_at\":\"2022-03-24T09:31:05.508\"}"), });

        loggerMock.Setup(LogLevel.Information, $"Bereite Daten für die Prozessierung von {topic.TopicTitle} ({topic.Canton}) vor...");
        loggerMock.Setup(LogLevel.Information, $"Exportiere {topic.TopicTitle} ({topic.Canton})...");
        loggerMock.Setup(LogLevel.Information, $"Exportiere {bewirtschaftungseinheitTopic.TopicTitle} ({bewirtschaftungseinheitTopic.Canton})...");

        await processor.PrepareData();
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.StartExportAsync(bewirtschaftungseinheitTopic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(bewirtschaftungseinheitTopic), Times.Once);
    }
}
