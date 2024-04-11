using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug;

internal static class TestExtensions
{
    internal static void Setup<T>(this Mock<ILogger<T>> loggerMock, LogLevel expectedLogLevel, string expectedLogSubstring)
        => loggerMock
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(expectedLogSubstring, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
            .Verifiable();
}
