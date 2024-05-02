using System.Net;
using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Geodatenbezug;

[TestClass]
[DeploymentItem("testdata/lwb_perimeter_terrassenreben_lv95.zip", "testdata")]
[DeploymentItem("testdata/lwb_perimeter_terrassenreben_lv95_no_gpkg.zip", "testdata")]
public class GeodiensteApiTest
{
    private readonly Topic topic = new ()
    {
        BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
        TopicTitle = "Perimeter LN- und Sömmerungsflächen",
        Canton = Canton.ZG,
        UpdatedAt = DateTime.Now.AddHours(-23),
    };

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
    public async Task RequestTopicInfoAsync()
    {
        var data = new GeodiensteInfoData
        {
            Services =
            [
                new Topic
                {
                    BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = Canton.ZG,
                    UpdatedAt = DateTime.Now.AddHours(-23),
                },
                new Topic
                {
                    BaseTopic = BaseTopic.lwb_rebbaukataster,
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
    public async Task RequestTopicInfoAsyncFails()
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
    public async Task StartExportAsync()
    {
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.NotFound, Content = "{\"error\":\"Cannot start data export because there is another data export pending\"}" },
            new () { Code = HttpStatusCode.OK, Content = "{\"info\":\"Data export successfully started. Call the URL of status_url to get the current status of the export.\"}" },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Starte den Datenexport mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/export.json", Times.Once());
        loggerMock.Setup(LogLevel.Information, "Es läuft gerade ein anderer Export. Versuche es in 1 Minute erneut.", Times.Once());

        var result = await CreateGeodiensteApiMock().StartExportAsync(topic);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
    }

    [TestMethod]
    public async Task StartExportAsyncTimeout()
    {
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
        loggerMock.Setup(LogLevel.Information, "Starte den Datenexport mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/export.json", Times.Once());
        loggerMock.Setup(LogLevel.Information, "Es läuft gerade ein anderer Export. Versuche es in 1 Minute erneut.", Times.Exactly(9));
        loggerMock.Setup(LogLevel.Error, "Es läuft bereits ein anderer Export. Zeitlimite überschritten.", Times.Once());

        var result = await CreateGeodiensteApiMock().StartExportAsync(topic);
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
    }

    [TestMethod]
    public async Task StartExportAsyncFails()
    {
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.Unauthorized },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Starte den Datenexport mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/export.json", Times.Once());

        var result = await CreateGeodiensteApiMock().StartExportAsync(topic);
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [TestMethod]
    public async Task CheckExportStatusAsync()
    {
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
        loggerMock.Setup(LogLevel.Information, "Prüfe den Status des Datenexports mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/status.json", Times.Once());
        loggerMock.Setup(LogLevel.Information, "Export ist in der Warteschlange. Versuche es in 1 Minute erneut.", Times.Once());
        loggerMock.Setup(LogLevel.Information, "Export ist in Bearbeitung. Versuche es in 1 Minute erneut.", Times.Once());

        var result = await CreateGeodiensteApiMock().CheckExportStatusAsync(topic);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.AreEqual(GeodiensteStatus.Success, JsonSerializer.Deserialize<GeodiensteStatusSuccess>(await result.Content.ReadAsStringAsync()).Status);
    }

    [TestMethod]
    public async Task CheckExportStatusAsyncTimeout()
    {
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
        loggerMock.Setup(LogLevel.Information, "Prüfe den Status des Datenexports mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/status.json", Times.Once());
        loggerMock.Setup(LogLevel.Information, "Export ist in der Warteschlange. Versuche es in 1 Minute erneut.", Times.Exactly(7));
        loggerMock.Setup(LogLevel.Information, "Export ist in Bearbeitung. Versuche es in 1 Minute erneut.", Times.Exactly(2));
        loggerMock.Setup(LogLevel.Error, "Zeitlimite überschritten. Status ist in Bearbeitung", Times.Once());

        var result = await CreateGeodiensteApiMock().CheckExportStatusAsync(topic);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.AreEqual(GeodiensteStatus.Working, JsonSerializer.Deserialize<GeodiensteStatusSuccess>(await result.Content.ReadAsStringAsync()).Status);
    }

    [TestMethod]
    public async Task CheckExportStatusAsyncFails()
    {
        httpTestMessageHandler.SetTestMessageResponses(
        [
            new () { Code = HttpStatusCode.Unauthorized },
        ]);
        httpClientFactoryMock.Setup(cf => cf.CreateClient(It.IsAny<string>())).Returns(httpTestMessageHandler.ToHttpClient()).Verifiable();
        loggerMock.Setup(LogLevel.Information, "Prüfe den Status des Datenexports mit https://geodienste.ch/downloads/lwb_perimeter_ln_sf/1234567890/status.json", Times.Once());

        var result = await CreateGeodiensteApiMock().CheckExportStatusAsync(topic);
        Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [TestMethod]
    public async Task DownloadExportAsync()
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                Content = new StreamContent(File.OpenRead("testdata\\lwb_perimeter_terrassenreben_lv95.zip")),
            });

        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler.Object));

        var downloadUrl = "http://test.com/test.zip";
        var destinationPath = ".\\testresult";
        var destinationFile = Path.Combine(destinationPath, "lwb_perimeter_terrassenreben_v2_0_lv95.gpkg");

        loggerMock.Setup(LogLevel.Information, $"Lade die Daten herunter {downloadUrl}", Times.Once());

        var result = await new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).DownloadExportAsync(downloadUrl, destinationPath);
        Assert.AreEqual(destinationFile, result);
        Assert.IsTrue(File.Exists(destinationFile));
    }

    [TestMethod]
    public async Task DownloadExportAsyncNoPackage()
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                Content = new StreamContent(File.OpenRead("testdata\\lwb_perimeter_terrassenreben_lv95_no_gpkg.zip")),
            });

        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpMessageHandler.Object));

        var downloadUrl = "http://test.com/test.zip";
        var destinationPath = ".\\testresult";
        loggerMock.Setup(LogLevel.Information, $"Lade die Daten herunter {downloadUrl}", Times.Once());

        await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).DownloadExportAsync(downloadUrl, destinationPath), "Keine GeoPackage-Datei im Archiv gefunden.");
    }

    [TestMethod]
    public void GetToken()
    {
        var result = new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object).GetToken(BaseTopic.lwb_rebbaukataster, Canton.BE);
        Assert.AreEqual("token2", result);
    }

    [TestMethod]
    public void GetTokenFailsNoKey()
    {
        var api = new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object);
        Assert.ThrowsException<KeyNotFoundException>(() => api.GetToken(BaseTopic.lwb_rebbaukataster, Canton.AI), "Token not found for topic lwb_rebbaukataster and canton AI");
    }

    [TestMethod]
    public void GetTokenFailsWithMissingTokenForTopic()
    {
        var api = new GeodiensteApi(loggerMock.Object, httpClientFactoryMock.Object);
        Assert.ThrowsException<InvalidOperationException>(() => api.GetToken(BaseTopic.lwb_bewirtschaftungseinheit, Canton.AI), "No tokens available for topic lwb_bewirtschaftungseinheit");
    }

    private GeodiensteApi CreateGeodiensteApiMock()
    {
        var mockGeodiensteApi = new Mock<GeodiensteApi>(loggerMock.Object, httpClientFactoryMock.Object)
        {
            CallBase = true,
        };
        mockGeodiensteApi.Setup(api => api.GetWaitDuration()).Returns(TimeSpan.Zero);
        mockGeodiensteApi.Setup(api => api.GetToken(It.IsAny<BaseTopic>(), It.IsAny<Canton>())).Returns("1234567890");
        return mockGeodiensteApi.Object;
    }
}
