﻿using System.Net;
using Geodatenbezug.Models;
using Geodatenbezug.Processors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug;

[TestClass]
public class TopicProcessorTest
{
    private readonly Topic topic = new ()
    {
        TopicTitle = BaseTopic.lwb_rebbaukataster.GetDescription(),
        Canton = Canton.AG,
        TopicName = BaseTopic.lwb_rebbaukataster.ToString() + "_v2_0",
        BaseTopic = BaseTopic.lwb_rebbaukataster,
        UpdatedAt = DateTime.Now,
    };

    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public void GetToken()
    {
        var topicProcessor = TopicProcessorFactory.Create(geodiensteApiMock.Object, loggerMock.Object, topic);
        var result = topicProcessor.GetToken(BaseTopic.lwb_rebbaukataster, Canton.BE);
        Assert.AreEqual("token2", result);
    }

    [TestMethod]
    public void GetTokenFails()
    {
        var topicProcessor = TopicProcessorFactory.Create(geodiensteApiMock.Object, loggerMock.Object, topic);
        Assert.ThrowsException<KeyNotFoundException>(() => topicProcessor.GetToken(BaseTopic.lwb_rebbaukataster, Canton.AI), "Token not found for topic lwb_rebbaukataster and canton AI");
    }

    [TestMethod]
    public async Task ProcessTopic()
    {
        var token = "token1";
        var processingResult = new ProcessingResult
        {
            Code = HttpStatusCode.Processing,
            TopicTitle = topic.TopicTitle,
            Canton = topic.Canton,
        };
        geodiensteApiMock
            .Setup(api => api.StartExportAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        geodiensteApiMock
            .Setup(api => api.CheckExportStatusAsync(It.IsAny<Topic>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"success\", \"info\":\"Data ready to be downloaded. Provide your credentials to download the data.\", \"download_url\":\"test.com/data.zip\", \"exported_at\":\"2022-03-24T09:31:05.508\"}"), });
        loggerMock.Setup(LogLevel.Information, $"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");

        var result = await TopicProcessorFactory.Create(geodiensteApiMock.Object, loggerMock.Object, topic).ProcessAsync();
        Assert.AreEqual(processingResult, result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic, token), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic, token), Times.Once);
    }

    [TestMethod]
    public async Task ProcessTopicStartExportFails()
    {
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

        var result = await TopicProcessorFactory.Create(geodiensteApiMock.Object, loggerMock.Object, topic).ProcessAsync();
        Assert.AreEqual(processingResult, result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic, token), Times.Once);
    }

    [TestMethod]
    public async Task ProcessTopicCheckExportStatusFailed()
    {
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

        var result = await TopicProcessorFactory.Create(geodiensteApiMock.Object, loggerMock.Object, topic).ProcessAsync();
        Assert.AreEqual(processingResult, result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic, token), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic, token), Times.Once);
    }

    [TestMethod]
    public async Task ProcessTopicCheckExportStatusError()
    {
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

        var result = await TopicProcessorFactory.Create(geodiensteApiMock.Object, loggerMock.Object, topic).ProcessAsync();
        Assert.AreEqual(processingResult, result);
        geodiensteApiMock.Verify(api => api.StartExportAsync(topic, token), Times.Once);
        geodiensteApiMock.Verify(api => api.CheckExportStatusAsync(topic, token), Times.Once);
    }
}