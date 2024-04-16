using System.Net;
using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug;

[TestClass]
public class GeodiensteApiTest
{
    private Mock<ILogger<GeodiensteApi>> loggerMock;
    private Mock<IHttpClientFactory> httpClientFactoryMock;
    private HttpTestMessageHandler httpTestMessageHandler;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<GeodiensteApi>>(MockBehavior.Strict);
        httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpTestMessageHandler = new HttpTestMessageHandler();
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
        httpClientFactoryMock.Verify();
        httpTestMessageHandler.VerifyNoOutstandingExpectation();
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
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.OK, Content = responseBody },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Rufe die Themeninformationen ab: https://geodienste.ch/info/services.json?base_topics=lwb_perimeter_ln_sf,lwb_rebbaukataster,lwb_perimeter_terrassenreben,lwb_biodiversitaetsfoerderflaechen,lwb_bewirtschaftungseinheit,lwb_nutzungsflaechen&topics=lwb_perimeter_ln_sf_v2_0,lwb_rebbaukataster_v2_0,lwb_perimeter_terrassenreben_v2_0,lwb_biodiversitaetsfoerderflaechen_v2_0,lwb_bewirtschaftungseinheit_v2_0,lwb_nutzungsflaechen_v2_0&cantons=AG,AI,AR,BE,BL,BS,FR,GE,GL,GR,JU,LU,NE,NW,OW,SG,SH,SO,SZ,TG,TI,UR,VD,VS,ZG,ZH&language=de");

        var result = await new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).RequestTopicInfoAsync();
        CollectionAssert.AreEquivalent(data.Services, result);
    }

    [TestMethod]
    public async Task RequestTopicInfoAsyncFailsTest()
    {
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.InternalServerError },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Rufe die Themeninformationen ab: https://geodienste.ch/info/services.json?base_topics=lwb_perimeter_ln_sf,lwb_rebbaukataster,lwb_perimeter_terrassenreben,lwb_biodiversitaetsfoerderflaechen,lwb_bewirtschaftungseinheit,lwb_nutzungsflaechen&topics=lwb_perimeter_ln_sf_v2_0,lwb_rebbaukataster_v2_0,lwb_perimeter_terrassenreben_v2_0,lwb_biodiversitaetsfoerderflaechen_v2_0,lwb_bewirtschaftungseinheit_v2_0,lwb_nutzungsflaechen_v2_0&cantons=AG,AI,AR,BE,BL,BS,FR,GE,GL,GR,JU,LU,NE,NW,OW,SG,SH,SO,SZ,TG,TI,UR,VD,VS,ZG,ZH&language=de");
        loggerMock.Setup(LogLevel.Error, $"Fehler beim Abrufen der Themeninformationen von geodienste.ch: Response status code does not indicate success: 500 (Internal Server Error).");

        var result = await new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).RequestTopicInfoAsync();
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task StartExportAsyncTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
            TopicName = "lwb_perimeter_ln_sf_v2_0",
            TopicTitle = "Perimeter LN- und Sömmerungsflächen",
            Canton = Canton.ZG,
            UpdatedAt = DateTime.Now.AddHours(-23),
        };
        var token = "1234567890";
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"info\":\"Data export successfully started. Call the URL of status_url to get the current status of the export.\"}" },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Starte den Datenexport für Perimeter LN- und Sömmerungsflächen (ZG) mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/export.json...");
        loggerMock.Setup(LogLevel.Information, "Es läuft gerade ein anderer Export. Versuche es in 1 Minute erneut.");
        var mockGeodiensteApi = new Mock<GeodiensteApi>(loggerMock.Object, httpClientFactoryMock.Object)
        {
            CallBase = true,
        };
        mockGeodiensteApi.Setup(api => api.WaitBeforeRetry()).Returns(Task.CompletedTask);

        var result = await mockGeodiensteApi.Object.StartExportAsync(topic, token);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task StartExportAsyncTimeoutTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
            TopicName = "lwb_perimeter_ln_sf_v2_0",
            TopicTitle = "Perimeter LN- und Sömmerungsflächen",
            Canton = Canton.ZG,
            UpdatedAt = DateTime.Now.AddHours(-23),
        };
        var token = "1234567890";
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Starte den Datenexport für Perimeter LN- und Sömmerungsflächen (ZG) mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/export.json...");
        loggerMock.Setup(LogLevel.Information, "Es läuft gerade ein anderer Export. Versuche es in 1 Minute erneut.", Times.Exactly(10));
        loggerMock.Setup(LogLevel.Error, "Es läuft bereits ein anderer Export. Zeitlimite überschritten.");
        var mockGeodiensteApi = new Mock<GeodiensteApi>(loggerMock.Object, httpClientFactoryMock.Object)
        {
            CallBase = true,
        };
        mockGeodiensteApi.Setup(api => api.WaitBeforeRetry()).Returns(Task.CompletedTask);

        var result = await mockGeodiensteApi.Object.StartExportAsync(topic, token);
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task StartExportAsyncFailsTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
            TopicName = "lwb_perimeter_ln_sf_v2_0",
            TopicTitle = "Perimeter LN- und Sömmerungsflächen",
            Canton = Canton.ZG,
            UpdatedAt = DateTime.Now.AddHours(-23),
        };
        var token = "1234567890";
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.Unauthorized },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Starte den Datenexport für Perimeter LN- und Sömmerungsflächen (ZG) mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/export.json...");
        var mockGeodiensteApi = new Mock<GeodiensteApi>(loggerMock.Object, httpClientFactoryMock.Object)
        {
            CallBase = true,
        };
        mockGeodiensteApi.Setup(api => api.WaitBeforeRetry()).Returns(Task.CompletedTask);

        var result = await mockGeodiensteApi.Object.StartExportAsync(topic, token);
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [TestMethod]
    public async Task CheckExportStatusAsyncTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
            TopicName = "lwb_perimeter_ln_sf_v2_0",
            TopicTitle = "Perimeter LN- und Sömmerungsflächen",
            Canton = Canton.ZG,
            UpdatedAt = DateTime.Now.AddHours(-23),
        };
        var token = "1234567890";
        var responseJson1 = new GeodiensteStatusSuccess()
        {
            Status = GeodiensteStatus.Queued,
            Info = "Try again later.",
            DownloadUrl = null,
            ExportedAt = null,
        };
        var responseJson2 = new GeodiensteStatusSuccess()
        {
            Status = GeodiensteStatus.Working,
            Info = "Try again later.",
            DownloadUrl = null,
            ExportedAt = null,
        };

        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"queued\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"working\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"success\", \"info\":\"Data ready to be downloaded. Provide your credentials to download the data.\", \"download_url\":\"test.com/data.zip\", \"exported_at\":\"2022-03-24T09:31:05.508\"}" },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Prüfe den Status des Datenexports für Perimeter LN- und Sömmerungsflächen (ZG) mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/status.json...");
        loggerMock.Setup(LogLevel.Information, "Export ist in der Warteschlange. Versuche es in 1 Minute erneut.");
        loggerMock.Setup(LogLevel.Information, "Export ist in Bearbeitung. Versuche es in 1 Minute erneut.");
        var mockGeodiensteApi = new Mock<GeodiensteApi>(loggerMock.Object, httpClientFactoryMock.Object)
        {
            CallBase = true,
        };
        mockGeodiensteApi.Setup(api => api.WaitBeforeRetry()).Returns(Task.CompletedTask);

        var result = await mockGeodiensteApi.Object.CheckExportStatusAsync(topic, token);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.AreEqual(GeodiensteStatus.Success, JsonSerializer.Deserialize<GeodiensteStatusSuccess>(await result.Content.ReadAsStringAsync()).Status);
    }

    [TestMethod]
    public async Task CheckExportStatusAsyncTimeoutTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
            TopicName = "lwb_perimeter_ln_sf_v2_0",
            TopicTitle = "Perimeter LN- und Sömmerungsflächen",
            Canton = Canton.ZG,
            UpdatedAt = DateTime.Now.AddHours(-23),
        };
        var token = "1234567890";
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"queued\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"queued\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"queued\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"queued\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"queued\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"queued\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"queued\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"working\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"working\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"working\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"status\":\"working\",\"info\":\"Try again later.\",\"download_url\":null,\"exported_at\":null}" },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Prüfe den Status des Datenexports für Perimeter LN- und Sömmerungsflächen (ZG) mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/status.json...");
        loggerMock.Setup(LogLevel.Information, "Export ist in der Warteschlange. Versuche es in 1 Minute erneut.", Times.Exactly(7));
        loggerMock.Setup(LogLevel.Information, "Export ist in Bearbeitung. Versuche es in 1 Minute erneut.", Times.Exactly(3));
        loggerMock.Setup(LogLevel.Error, "Zeitlimite überschritten. Status ist in Bearbeitung");
        var mockGeodiensteApi = new Mock<GeodiensteApi>(loggerMock.Object, httpClientFactoryMock.Object)
        {
            CallBase = true,
        };
        mockGeodiensteApi.Setup(api => api.WaitBeforeRetry()).Returns(Task.CompletedTask);

        var result = await mockGeodiensteApi.Object.CheckExportStatusAsync(topic, token);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.AreEqual(GeodiensteStatus.Working, JsonSerializer.Deserialize<GeodiensteStatusSuccess>(await result.Content.ReadAsStringAsync()).Status);
    }

    [TestMethod]
    public async Task CheckExportStatusAsyncFailsTest()
    {
        var topic = new Topic
        {
            BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
            TopicName = "lwb_perimeter_ln_sf_v2_0",
            TopicTitle = "Perimeter LN- und Sömmerungsflächen",
            Canton = Canton.ZG,
            UpdatedAt = DateTime.Now.AddHours(-23),
        };
        var token = "1234567890";
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.Unauthorized },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Prüfe den Status des Datenexports für Perimeter LN- und Sömmerungsflächen (ZG) mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/status.json...");
        var mockGeodiensteApi = new Mock<GeodiensteApi>(loggerMock.Object, httpClientFactoryMock.Object)
        {
            CallBase = true,
        };
        mockGeodiensteApi.Setup(api => api.WaitBeforeRetry()).Returns(Task.CompletedTask);

        var result = await mockGeodiensteApi.Object.CheckExportStatusAsync(topic, token);
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }
}
