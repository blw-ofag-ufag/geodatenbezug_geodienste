using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug;

[TestClass]
public class ProcessingTest
{
    private Mock<ILogger<Processing>> loggerMock;


    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<Processing>>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public async Task TestGetTopicsToUpdate()
    {
        var datestring_delta4 = DateTime.Now.AddHours(-4);
        var datestring_delta23 = DateTime.Now.AddHours(-23);
        var datestring_delta30 = DateTime.Now.AddHours(-30);
        var geodiensteApiMock = new Mock<IGeodiensteApi>();
        geodiensteApiMock.Setup(api => api.RequestTopicInfoAsync())
            .ReturnsAsync(
            [
                new()
                {
                    BaseTopic = "lwb_perimeter_ln_sf",
                    TopicName = "lwb_perimeter_ln_sf_v2_0",
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = "SH",
                    UpdatedAt = datestring_delta4
                },
                new()
                {
                    BaseTopic = "lwb_perimeter_ln_sf",
                    TopicName = "lwb_perimeter_ln_sf_v2_0",
                    TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                    Canton = "ZG",
                    UpdatedAt = datestring_delta23
                },
                new() {
                    BaseTopic = "lwb_rebbaukataster",
                    TopicName = "lwb_rebbaukataster_v2_0",
                    TopicTitle = "Rebbaukataster",
                    Canton = "SH",
                    UpdatedAt = datestring_delta30
                },
                new()
                {
                    BaseTopic = "lwb_rebbaukataster",
                    TopicName = "lwb_rebbaukataster_v2_0",
                    TopicTitle = "Rebbaukataster",
                    Canton = "ZG",
                    UpdatedAt = null
                }
            ]);

        loggerMock.Setup(LogLevel.Information, $"Thema Perimeter LN- und Sömmerungsflächen (SH) wurde am {datestring_delta4:yyyy-MM-dd HH:mm:ss} aktualisiert und wird verarbeitet");
        loggerMock.Setup(LogLevel.Information, $"Thema Perimeter LN- und Sömmerungsflächen (ZG) wurde am {datestring_delta23:yyyy-MM-dd HH:mm:ss} aktualisiert und wird verarbeitet");
        loggerMock.Setup(LogLevel.Information, $"Thema Rebbaukataster (SH) wurde seit {datestring_delta30:yyyy-MM-dd HH:mm:ss} nicht aktualisiert");
        loggerMock.Setup(LogLevel.Information, "Thema Rebbaukataster (ZG) ist nicht verfügbar");
        loggerMock.Setup(LogLevel.Information, "2 Themen werden prozessiert");
        var topicsToProcess = await new Processing(geodiensteApiMock.Object, loggerMock.Object).GetTopicsToUpdate();
        Assert.AreEqual(2, topicsToProcess.Count);
        Assert.AreEqual("lwb_perimeter_ln_sf", topicsToProcess[0].BaseTopic);
        Assert.AreEqual("SH", topicsToProcess[0].Canton);
        Assert.AreEqual("lwb_perimeter_ln_sf", topicsToProcess[1].BaseTopic);
        Assert.AreEqual("ZG", topicsToProcess[1].Canton);
    }
}
