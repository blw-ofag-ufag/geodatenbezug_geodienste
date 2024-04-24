using System.Net;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug.Processors;

[TestClass]
public class TopicProcessorTest
{
    private readonly Topic topic = new()
    {
        TopicTitle = BaseTopic.lwb_rebbaukataster.GetDescription(),
        Canton = Canton.AG,
        TopicName = BaseTopic.lwb_rebbaukataster.ToString() + "_v2_0",
        BaseTopic = BaseTopic.lwb_rebbaukataster,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private RebbaukatasterProcessor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        processor = new RebbaukatasterProcessor(geodiensteApiMock.Object, loggerMock.Object, topic);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task ExportTopic()
    {
        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.Processing,
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"success\", \"info\":\"Data ready to be downloaded. Provide your credentials to download the data.\", \"download_url\":\"test.com/data.zip\", \"exported_at\":\"2022-03-24T09:31:05.508\"}"), });
        loggerMock.Setup(LogLevel.Information, $"Exportiere {topic.TopicTitle} ({topic.Canton})...");

        var result = await processor.ExportTopicAsync(topic);

        Assert.AreEqual("test.com/data.zip", result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic), Times.Once);
    }

    [TestMethod]
    public async Task ExportTopicOnlyOneExportIn24h()
    {
        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.Processing,
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"Only one data export per topic allowed every 24 h\"}"), });
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"success\", \"info\":\"Data ready to be downloaded. Provide your credentials to download the data.\", \"download_url\":\"test.com/data.zip\", \"exported_at\":\"2022-03-24T09:31:05.508\"}"), });
        loggerMock.Setup(LogLevel.Information, $"Exportiere {topic.TopicTitle} ({topic.Canton})...");

        var result = await processor.ExportTopicAsync(topic);

        Assert.AreEqual("test.com/data.zip", result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic), Times.Once);
    }

    [TestMethod]
    public async Task ExportTopicStartExportFails()
    {
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
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"Data export information not found. Invalid token?\"}"), });
        loggerMock.Setup(LogLevel.Information, $"Exportiere {topic.TopicTitle} ({topic.Canton})...");
        loggerMock.Setup(LogLevel.Error, $"Fehler beim Starten des Exports für Thema {topic.TopicTitle} ({topic.Canton}): {HttpStatusCode.NotFound} - Data export information not found. Invalid token?");

        // TODO: Check processingResult
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await processor.ExportTopicAsync(topic), "Export failed");
        Assert.AreEqual(processingResult.Code, processor.ProcessingResult.Code);
        Assert.AreEqual(processingResult.Reason, processor.ProcessingResult.Reason);
        Assert.AreEqual(processingResult.Info, processor.ProcessingResult.Info);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic), Times.Once);
    }

    [TestMethod]
    public async Task ExportTopicCheckExportStatusFailed()
    {
        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.OK,
            Reason = "Failed",
            Info = "An unexpected error occurred. Please try again by starting a new data export.",
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"failed\", \"info\":\"An unexpected error occurred. Please try again by starting a new data export.\"}"), });
        loggerMock.Setup(LogLevel.Information, $"Exportiere {topic.TopicTitle} ({topic.Canton})...");
        loggerMock.Setup(LogLevel.Error, $"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): An unexpected error occurred. Please try again by starting a new data export.");

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await processor.ExportTopicAsync(topic), "Export failed");
        Assert.AreEqual(processingResult.Code, processor.ProcessingResult.Code);
        Assert.AreEqual(processingResult.Reason, processor.ProcessingResult.Reason);
        Assert.AreEqual(processingResult.Info, processor.ProcessingResult.Info);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic), Times.Once);
    }

    [TestMethod]
    public async Task ExportTopicCheckExportStatusError()
    {
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
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"Data export information not found. Invalid token?\"}"), });
        loggerMock.Setup(LogLevel.Information, $"Exportiere {topic.TopicTitle} ({topic.Canton})...");
        loggerMock.Setup(LogLevel.Error, $"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): {HttpStatusCode.NotFound} - Data export information not found. Invalid token?");

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await processor.ExportTopicAsync(topic), "Export failed");
        Assert.AreEqual(processingResult.Code, processor.ProcessingResult.Code);
        Assert.AreEqual(processingResult.Reason, processor.ProcessingResult.Reason);
        Assert.AreEqual(processingResult.Info, processor.ProcessingResult.Info);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic), Times.Once);
    }
}
