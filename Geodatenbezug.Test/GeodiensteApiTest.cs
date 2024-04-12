using System.Net;
using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;

namespace Geodatenbezug;

[TestClass]
public class GeodiensteApiTest
{
    private const string InfoServicesUrl = "https://geodienste.ch/info/services.json*";

    private Mock<ILogger<GeodiensteApi>> loggerMock;
    private MockHttpMessageHandler messageHandlerMock;
    private Mock<IHttpClientFactory> httpClientFactoryMock;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<GeodiensteApi>>(MockBehavior.Strict);
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
    public async Task RequestTopicInfoAsyncTest()
    {
        var data = new GeodiensteInfoData
        {
            Services =
            [
                new Topic
                {
                    BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
                    TopicName = "lwb_perimeter_ln_sf_v2_0",
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = Canton.ZG,
                    UpdatedAt = DateTime.Now.AddHours(-23),
                },
                new Topic
                {
                    BaseTopic = BaseTopic.lwb_rebbaukataster,
                    TopicName = "lwb_rebbaukataster_v2_0",
                    TopicTitle = "Rebbaukataster",
                    Canton = Canton.ZG,
                    UpdatedAt = null,
                },
            ],
        };
        var responseBody = JsonSerializer.Serialize(data);

        messageHandlerMock.When(InfoServicesUrl)
            .Respond("application/json", responseBody);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(messageHandlerMock.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Rufe die Themeninformationen ab: https://geodienste.ch/info/services.json?base_topics=lwb_perimeter_ln_sf,lwb_rebbaukataster,lwb_perimeter_terrassenreben,lwb_biodiversitaetsfoerderflaechen,lwb_bewirtschaftungseinheit,lwb_nutzungsflaechen&topics=lwb_perimeter_ln_sf_v2_0,lwb_rebbaukataster_v2_0,lwb_perimeter_terrassenreben_v2_0,lwb_biodiversitaetsfoerderflaechen_v2_0,lwb_bewirtschaftungseinheit_v2_0,lwb_nutzungsflaechen_v2_0&cantons=AG,AI,AR,BE,BL,BS,FR,GE,GL,GR,JU,LU,NE,NW,OW,SG,SH,SO,SZ,TG,TI,UR,VD,VS,ZG,ZH&language=de");

        var result = await new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).RequestTopicInfoAsync();
        CollectionAssert.AreEquivalent(data.Services, result);
    }

    [TestMethod]
    public async Task RequestTopicInfoAsyncFailsTest()
    {
        messageHandlerMock.When(InfoServicesUrl)
            .Respond(HttpStatusCode.InternalServerError);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(messageHandlerMock.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Rufe die Themeninformationen ab: https://geodienste.ch/info/services.json?base_topics=lwb_perimeter_ln_sf,lwb_rebbaukataster,lwb_perimeter_terrassenreben,lwb_biodiversitaetsfoerderflaechen,lwb_bewirtschaftungseinheit,lwb_nutzungsflaechen&topics=lwb_perimeter_ln_sf_v2_0,lwb_rebbaukataster_v2_0,lwb_perimeter_terrassenreben_v2_0,lwb_biodiversitaetsfoerderflaechen_v2_0,lwb_bewirtschaftungseinheit_v2_0,lwb_nutzungsflaechen_v2_0&cantons=AG,AI,AR,BE,BL,BS,FR,GE,GL,GR,JU,LU,NE,NW,OW,SG,SH,SO,SZ,TG,TI,UR,VD,VS,ZG,ZH&language=de");
        loggerMock.Setup(LogLevel.Error, $"Fehler beim Abrufen der Themeninformationen von geodienste.ch: Response status code does not indicate success: 500 (Internal Server Error).");

        var result = await new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).RequestTopicInfoAsync();
        Assert.AreEqual(0, result.Count);
    }
}
