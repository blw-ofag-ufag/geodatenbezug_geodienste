using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug.Test;

[TestClass]
public class ProcessingTest
{
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
        var logs = new List<LogMessage>
            {
                new()
                {
                    Message = $"Thema Perimeter LN- und Sömmerungsflächen (SH) wurde am {datestring_delta4:yyyy-MM-dd HH:mm:ss} aktualisiert und wird verarbeitet",
                    LogLevel = LogLevel.Information
                },
                new()
                {
                    Message = $"Thema Perimeter LN- und Sömmerungsflächen (ZG) wurde am {datestring_delta23:yyyy-MM-dd HH:mm:ss} aktualisiert und wird verarbeitet",
                    LogLevel = LogLevel.Information
                },
                new()
                {
                    Message = $"Thema Rebbaukataster (SH) wurde seit {datestring_delta30:yyyy-MM-dd HH:mm:ss} nicht aktualisiert",
                    LogLevel = LogLevel.Information
                },
                new()
                {
                    Message = "Thema Rebbaukataster (ZG) ist nicht verfügbar",
                    LogLevel = LogLevel.Information
                },
                new() {
                    Message = "2 Themen werden prozessiert",
                    LogLevel = LogLevel.Information
                }
            };

        var loggerMock = LoggerMock<Processing>.CreateDefault();
        var topicsToProcess = await new Processing(geodiensteApiMock.Object, loggerMock.Object).GetTopicsToUpdate();
        Assert.AreEqual(2, topicsToProcess.Count);
        Assert.AreEqual("lwb_perimeter_ln_sf", topicsToProcess[0].BaseTopic);
        Assert.AreEqual("SH", topicsToProcess[0].Canton);
        Assert.AreEqual("lwb_perimeter_ln_sf", topicsToProcess[1].BaseTopic);
        Assert.AreEqual("ZG", topicsToProcess[1].Canton);
        Helpers.AssertLogs(loggerMock.LogMessages, logs);
    }
}
