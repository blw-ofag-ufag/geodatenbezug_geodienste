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
                    Canton = Models.Canton.ZG,
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
}
