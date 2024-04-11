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
    private Mock<ILogger<GeodiensteApi>> loggerMock;
    private MockHttpMessageHandler messageHandlerMock;
    private Mock<IHttpClientFactory> httpClientFactoryMock;


    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<GeodiensteApi>>();
        httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        messageHandlerMock = new MockHttpMessageHandler();
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
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
        loggerMock.Setup(LogLevel.Information, "Rufe die Themeninformationen ab...");
        var result = await new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).RequestTopicInfoAsync();
        Assert.AreEqual(data.Services.Count, result.Count);
        for (var i = 0; i < data.Services.Count; i++)
        {
            Assert.AreEqual(data.Services[i].BaseTopic, result[i].BaseTopic);
            Assert.AreEqual(data.Services[i].TopicName, result[i].TopicName);
            Assert.AreEqual(data.Services[i].TopicTitle, result[i].TopicTitle);
            Assert.AreEqual(data.Services[i].Canton, result[i].Canton);
            Assert.AreEqual(data.Services[i].UpdatedAt, result[i].UpdatedAt);
        }
    }

    [TestMethod]
    public async Task TestRequestTopicInfoAsyncFailed()
    {
        messageHandlerMock.When("https://geodienste.ch/info/services.json*")
            .Respond(HttpStatusCode.InternalServerError);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(messageHandlerMock.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Rufe die Themeninformationen ab...");
        loggerMock.Setup(LogLevel.Error, $"Fehler beim Abrufen der Themeninformationen von geodienste.ch: {HttpStatusCode.InternalServerError}  - Internal Server Error");
        var result = await new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).RequestTopicInfoAsync();
        Assert.AreEqual(0, result.Count);
    }
}
