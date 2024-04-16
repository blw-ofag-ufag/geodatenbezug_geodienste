using System.Net;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug;

[TestClass]
public class ProcessingTest
{
    private Mock<ILogger<Processing>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processing>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task GetTopicsToUpdateTest()
    {
        var datestring_delta4 = DateTime.Now.AddHours(-4);
        var datestring_delta23 = DateTime.Now.AddHours(-23);
        var datestring_delta30 = DateTime.Now.AddHours(-30);
        geodiensteApiMock
            .Setup(api => api.RequestTopicInfoAsync())
            .ReturnsAsync(
            [
                new ()
                {
                    BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
                    TopicName = "lwb_perimeter_ln_sf_v2_0",
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = Canton.SH,
                    UpdatedAt = datestring_delta4,
                },
                new ()
                {
                    BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
                    TopicName = "lwb_perimeter_ln_sf_v2_0",
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = Canton.ZG,
                    UpdatedAt = datestring_delta23,
                },
                new ()
                {
                    BaseTopic = BaseTopic.lwb_rebbaukataster,
                    TopicName = "lwb_rebbaukataster_v2_0",
                    TopicTitle = "Rebbaukataster",
                    Canton = Canton.SH,
                    UpdatedAt = datestring_delta30,
                },
                new ()
                {
                    BaseTopic = BaseTopic.lwb_rebbaukataster,
                    TopicName = "lwb_rebbaukataster_v2_0",
                    TopicTitle = "Rebbaukataster",
                    Canton = Canton.ZG,
                    UpdatedAt = null,
                },
            ]);

        loggerMock.Setup(LogLevel.Information, $"Thema Perimeter LN- und Sömmerungsflächen (SH) wurde am {datestring_delta4:yyyy-MM-dd HH:mm:ss} aktualisiert und wird verarbeitet");
        loggerMock.Setup(LogLevel.Information, $"Thema Perimeter LN- und Sömmerungsflächen (ZG) wurde am {datestring_delta23:yyyy-MM-dd HH:mm:ss} aktualisiert und wird verarbeitet");
        loggerMock.Setup(LogLevel.Information, $"Thema Rebbaukataster (SH) wurde seit {datestring_delta30:yyyy-MM-dd HH:mm:ss} nicht aktualisiert");
        loggerMock.Setup(LogLevel.Information, "Thema Rebbaukataster (ZG) ist nicht verfügbar");
        loggerMock.Setup(LogLevel.Information, "2 Themen werden prozessiert");

        var topicsToProcess = await new Processing(geodiensteApiMock.Object, loggerMock.Object).GetTopicsToProcess();
        Assert.AreEqual(2, topicsToProcess.Count);
        Assert.AreEqual(BaseTopic.lwb_perimeter_ln_sf, topicsToProcess[0].BaseTopic);
        Assert.AreEqual(Canton.SH, topicsToProcess[0].Canton);
        Assert.AreEqual(BaseTopic.lwb_perimeter_ln_sf, topicsToProcess[1].BaseTopic);
        Assert.AreEqual(Canton.ZG, topicsToProcess[1].Canton);
    }

    [TestMethod]
    public void GetTokenTest()
    {
        var result = new Processing(geodiensteApiMock.Object, loggerMock.Object).GetToken(BaseTopic.lwb_rebbaukataster, Canton.BE);
        Assert.AreEqual("token2", result);
    }

    [TestMethod]
    public void GetTokenFailsTest()
    {
        Assert.ThrowsException<KeyNotFoundException>(() => new Processing(geodiensteApiMock.Object, loggerMock.Object).GetToken(BaseTopic.lwb_rebbaukataster, Canton.AI), "Token not found for topic lwb_rebbaukataster and canton AI");
    }

    [TestMethod]
    public async Task ProcessTopicTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_rebbaukataster,
            TopicName = "lwb_rebbaukataster_v2_0",
            TopicTitle = "Rebbaukataster",
            Canton = Canton.AG,
            UpdatedAt = DateTime.Now.AddHours(-4),
        };
        var token = "token1";
        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.OK,
            Reason = "Success",
            Info = "Processing completed",
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
            DownloadUrl = null,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"success\", \"info\":\"Data ready to be downloaded. Provide your credentials to download the data.\", \"download_url\":\"test.com/data.zip\", \"exported_at\":\"2022-03-24T09:31:05.508\"}"), });
        loggerMock.Setup(LogLevel.Information, $"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");

        var result = await new Processing(geodiensteApiMock.Object, loggerMock.Object).ProcessTopic(topic);
        Assert.AreEqual(processingResult, result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(It.Is<Topic>(t => t == topic), It.Is<string>(s => s == token)), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(It.Is<Topic>(t => t == topic), It.Is<string>(s => s == token)), Times.Once);
    }

    [TestMethod]
    public async Task ProcessTopicStartExportFailsTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_rebbaukataster,
            TopicName = "lwb_rebbaukataster_v2_0",
            TopicTitle = "Rebbaukataster",
            Canton = Canton.AG,
            UpdatedAt = DateTime.Now.AddHours(-4),
        };
        var token = "token1";
        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.NotFound,
            Reason = "Not Found",
            Info = "Data export information not found. Invalid token?",
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"Data export information not found. Invalid token?\"}"), });

        loggerMock.Setup(LogLevel.Information, $"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");
        loggerMock.Setup(LogLevel.Error, $"Fehler beim Starten des Exports für Thema {topic.TopicTitle} ({topic.Canton}): {HttpStatusCode.NotFound} - Data export information not found. Invalid token?");

        var result = await new Processing(geodiensteApiMock.Object, loggerMock.Object).ProcessTopic(topic);
        Assert.AreEqual(processingResult, result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(It.Is<Topic>(t => t == topic), It.Is<string>(s => s == token)), Times.Once);
    }

    [TestMethod]
    public async Task ProcessTopicCheckExportStatusFailedTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_rebbaukataster,
            TopicName = "lwb_rebbaukataster_v2_0",
            TopicTitle = "Rebbaukataster",
            Canton = Canton.AG,
            UpdatedAt = DateTime.Now.AddHours(-4),
        };
        var token = "token1";
        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.OK,
            Reason = "Failed",
            Info = "An unexpected error occurred. Please try again by starting a new data export.",
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"failed\", \"info\":\"An unexpected error occurred. Please try again by starting a new data export.\"}"), });

        loggerMock.Setup(LogLevel.Information, $"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");
        loggerMock.Setup(LogLevel.Error, $"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): An unexpected error occurred. Please try again by starting a new data export.");

        var result = await new Processing(geodiensteApiMock.Object, loggerMock.Object).ProcessTopic(topic);
        Assert.AreEqual(processingResult, result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(It.Is<Topic>(t => t == topic), It.Is<string>(s => s == token)), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(It.Is<Topic>(t => t == topic), It.Is<string>(s => s == token)), Times.Once);
    }

    [TestMethod]
    public async Task ProcessTopicCheckExportStatusErrorTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_rebbaukataster,
            TopicName = "lwb_rebbaukataster_v2_0",
            TopicTitle = "Rebbaukataster",
            Canton = Canton.AG,
            UpdatedAt = DateTime.Now.AddHours(-4),
        };
        var token = "token1";
        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.NotFound,
            Reason = "Not Found",
            Info = "Data export information not found. Invalid token?",
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"Data export information not found. Invalid token?\"}"), });

        loggerMock.Setup(LogLevel.Information, $"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");
        loggerMock.Setup(LogLevel.Error, $"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): {HttpStatusCode.NotFound} - Data export information not found. Invalid token?");

        var result = await new Processing(geodiensteApiMock.Object, loggerMock.Object).ProcessTopic(topic);
        Assert.AreEqual(processingResult, result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(It.Is<Topic>(t => t == topic), It.Is<string>(s => s == token)), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(It.Is<Topic>(t => t == topic), It.Is<string>(s => s == token)), Times.Once);
    }
}
