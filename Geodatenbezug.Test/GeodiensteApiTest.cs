using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text.Json;

namespace Geodatenbezug.Test;

[TestClass]
public class GeoDiensteApiTest
{
    private LoggerMock<GeodiensteApi> loggerMock;
    private MockHttpMessageHandler mockHttp;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = LoggerMock<GeodiensteApi>.CreateDefault();
        mockHttp = new MockHttpMessageHandler();
    }

    [TestCleanup]
    public void Cleanup()
    {
        mockHttp.Dispose();
    }

    [TestMethod]
    public async Task TestRequestTopicInfoAsync()
    {
        var data = new GeodiensteInfoData()
        {
            Services =
            [
            new()
            {
                BaseTopic = "lwb_perimeter_ln_sf",
                TopicName = "lwb_perimeter_ln_sf_v2_0",
                TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                Canton = "ZG",
                UpdatedAt = DateTime.Now.AddHours(-23)
            },
            new()
            {
                BaseTopic = "lwb_rebbaukataster",
                TopicName = "lwb_rebbaukataster_v2_0",
                TopicTitle = "Rebbaukataster",
                Canton = "ZG",
                UpdatedAt = null
            }
        ]
        };
        var responseBody = JsonSerializer.Serialize(data);
        mockHttp.When("https://geodienste.ch/info/services.json*")
            .Respond("application/json", responseBody);
        var client = mockHttp.ToHttpClient();
        var api = new GeodiensteApi(loggerMock.Object)
        {
            GetHttpClient = () => client
        };
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
        Helpers.AssertLogs(loggerMock.LogMessages, []);
    }

    [TestMethod]
    public async Task TestRequestTopicInfoAsyncFailed()
    {
        mockHttp.When("https://geodienste.ch/info/services.json*")
            .Respond(HttpStatusCode.InternalServerError);
        var client = mockHttp.ToHttpClient();
        var loggerMock = LoggerMock<GeodiensteApi>.CreateDefault();
        var api = new GeodiensteApi(loggerMock.Object)
        {
            GetHttpClient = () => client
        };
        var result = await api.RequestTopicInfoAsync();
        Assert.AreEqual(0, result.Count);
        var logs = new List<LogMessage>
            {
                new()
                {
                    Message = $"Fehler beim Abrufen der Themeninformationen von geodienste.ch: {HttpStatusCode.InternalServerError}  - Internal Server Error",
                    LogLevel = LogLevel.Error
                },
            };
        Helpers.AssertLogs(loggerMock.LogMessages, logs);
    }
}
