using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;

namespace Geodatenbezug;

[TestClass]
public class GeoDiensteApiTest
{
    private GeodiensteApi api;
    private LoggerMock<GeodiensteApi> loggerMock;
    private MockHttpMessageHandler messageHandlerMock;
    private Mock<IHttpClientFactory> httpClientFactoryMock;


    [TestInitialize]
    public void Initialize()
    {
        loggerMock = LoggerMock<GeodiensteApi>.CreateDefault();
        httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        messageHandlerMock = new MockHttpMessageHandler();
        api = new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        httpClientFactoryMock.Verify();
        messageHandlerMock.VerifyNoOutstandingExpectation();
        messageHandlerMock.Dispose();
    }

    [TestMethod]
    public async Task TestRequestTopicInfoAsync()
    {
        var data = new GeodiensteInfoData
        {
            Services =
            [
                new Topic
                {
                    BaseTopic = "lwb_perimeter_ln_sf",
                    TopicName = "lwb_perimeter_ln_sf_v2_0",
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = "ZG",
                    UpdatedAt = DateTime.Now.AddHours(-23)
                },
                new Topic
                {
                    BaseTopic = "lwb_rebbaukataster",
                    TopicName = "lwb_rebbaukataster_v2_0",
                    TopicTitle = "Rebbaukataster",
                    Canton = "ZG",
                    UpdatedAt = null,
                },
            ]
        };

        var responseBody = JsonSerializer.Serialize(data);
        messageHandlerMock.When("https://geodienste.ch/info/services.json*")
            .Respond("application/json", responseBody);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(messageHandlerMock.ToHttpClient()).Verifiable();
        var result = await api.RequestTopicInfoAsync();
        Assert.AreEqual(data.Services.Count, result.Count);
        for (var i = 0; i < data.Services.Count; i++)
        {
            Assert.AreEqual(data.Services[i].BaseTopic, result[i].BaseTopic);
            Assert.AreEqual(data.Services[i].TopicName, result[i].TopicName);
            Assert.AreEqual(data.Services[i].TopicTitle, result[i].TopicTitle);
            Assert.AreEqual(data.Services[i].Canton, result[i].Canton);
            Assert.AreEqual(data.Services[i].UpdatedAt, result[i].UpdatedAt);
        }

        var logs = new List<LogMessage>
        {
            new()
            {
                Message = "Rufe die Themeninformationen ab...",
                LogLevel = LogLevel.Information,
            },
        };

        loggerMock.AssertLogs(logs);
    }

    [TestMethod]
    public async Task TestRequestTopicInfoAsyncFailed()
    {
        messageHandlerMock.When("https://geodienste.ch/info/services.json*")
            .Respond(HttpStatusCode.InternalServerError);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(messageHandlerMock.ToHttpClient()).Verifiable();
        var result = await api.RequestTopicInfoAsync();
        Assert.AreEqual(0, result.Count);
        var logs = new List<LogMessage>
        {
            new()
            {
                Message = "Rufe die Themeninformationen ab...",
                LogLevel = LogLevel.Information
            },
            new()
            {
                Message = $"Fehler beim Abrufen der Themeninformationen von geodienste.ch: {HttpStatusCode.InternalServerError}  - Internal Server Error",
                LogLevel = LogLevel.Error
            }
        };

        loggerMock.AssertLogs(logs);
    }
}
