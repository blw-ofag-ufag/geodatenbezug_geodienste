using System.Globalization;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug;

[TestClass]
public class ProcessingTest
{
    private Mock<ILogger<Processor>> loggerMock;
    private Mock<IGeodiensteApi> geodiensteApiMock;
    private Mock<IAzureStorage> azureStorageMock;
    private Mock<IMailService> mailServiceMock;
    private Processor processor;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processor>>(MockBehavior.Strict);
        geodiensteApiMock = new Mock<IGeodiensteApi>(MockBehavior.Strict);
        azureStorageMock = new Mock<IAzureStorage>(MockBehavior.Strict);
        mailServiceMock = new Mock<IMailService>(MockBehavior.Strict);
        processor = new Processor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object, mailServiceMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task GetTopicsToUpdate()
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
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = Canton.SH,
                    UpdatedAt = datestring_delta4,
                },
                new ()
                {
                    BaseTopic = BaseTopic.lwb_perimeter_ln_sf,
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = Canton.ZG,
                    UpdatedAt = datestring_delta23,
                },
                new ()
                {
                    BaseTopic = BaseTopic.lwb_rebbaukataster,
                    TopicTitle = "Rebbaukataster",
                    Canton = Canton.SH,
                    UpdatedAt = datestring_delta30,
                },
                new ()
                {
                    BaseTopic = BaseTopic.lwb_rebbaukataster,
                    TopicTitle = "Rebbaukataster",
                    Canton = Canton.ZG,
                    UpdatedAt = null,
                },
            ]);

        loggerMock.Setup(LogLevel.Information, "Laden der Themen...");
        loggerMock.Setup(LogLevel.Information, $"Thema Perimeter LN- und Sömmerungsflächen (SH) wurde am {datestring_delta4.ToString("G", CultureInfo.GetCultureInfo("de-CH"))} aktualisiert und wird verarbeitet");
        loggerMock.Setup(LogLevel.Information, $"Thema Perimeter LN- und Sömmerungsflächen (ZG) wurde am {datestring_delta23.ToString("G", CultureInfo.GetCultureInfo("de-CH"))} aktualisiert und wird verarbeitet");
        loggerMock.Setup(LogLevel.Information, $"Thema Rebbaukataster (SH) wurde seit {datestring_delta30.ToString("G", CultureInfo.GetCultureInfo("de-CH"))} nicht aktualisiert");
        loggerMock.Setup(LogLevel.Information, "Thema Rebbaukataster (ZG) ist nicht verfügbar");
        loggerMock.Setup(LogLevel.Information, "2 Themen werden prozessiert");

        azureStorageMock.SetupSequence(storage => storage.GetLastProcessed(It.IsAny<Topic>()))
            .ReturnsAsync(datestring_delta23)
            .ReturnsAsync((DateTime?)null)
            .ReturnsAsync(datestring_delta23);

        var topicsToProcess = await new Processor(geodiensteApiMock.Object, azureStorageMock.Object, loggerMock.Object).GetTopicsToProcess();
        Assert.AreEqual(2, topicsToProcess.Count);
        Assert.AreEqual(BaseTopic.lwb_perimeter_ln_sf, topicsToProcess[0].BaseTopic);
        Assert.AreEqual(Canton.SH, topicsToProcess[0].Canton);
        Assert.AreEqual(BaseTopic.lwb_perimeter_ln_sf, topicsToProcess[1].BaseTopic);
        Assert.AreEqual(Canton.ZG, topicsToProcess[1].Canton);
    }
}
