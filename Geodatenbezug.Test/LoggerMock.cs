using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.ObjectModel;

namespace Geodatenbezug;

public class LogMessage
{
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public object? State { get; set; }
    public Exception? Exception { get; set; }
    public string? Message { get; set; }
}

public class LoggerMock<TCategoryName> : Mock<ILogger<TCategoryName>>
{
    private readonly List<LogMessage> logMessages = new();

    public ReadOnlyCollection<LogMessage> LogMessages => new(logMessages);

    protected LoggerMock()
    {
    }
    public void AssertLogs(List<LogMessage> expectedLogs)
    {
        Assert.AreEqual(expectedLogs.Count, logMessages.Count);
        for (int i = 0; i < logMessages.Count; i++)
        {
            Assert.AreEqual(expectedLogs[i].LogLevel, logMessages[i].LogLevel);
            Assert.AreEqual(expectedLogs[i].Message, logMessages[i].Message);
        }
    }

    public static LoggerMock<TCategoryName> CreateDefault()
    {
        return new LoggerMock<TCategoryName>()
            .SetupLog()
            .SetupIsEnabled(LogLevel.Information);
    }

    public LoggerMock<TCategoryName> SetupIsEnabled(LogLevel logLevel, bool enabled = true)
    {
        Setup(x => x.IsEnabled(It.Is<LogLevel>(p => p.Equals(logLevel))))
            .Returns(enabled);
        return this;
    }

    public LoggerMock<TCategoryName> SetupLog()
    {
        Setup(logger => logger.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
        ))
        .Callback(new InvocationAction(invocation => {
            var logLevel = (LogLevel)invocation.Arguments[0];
            var eventId = (EventId)invocation.Arguments[1];
            var state = invocation.Arguments[2];
            var exception = (Exception?)invocation.Arguments[3];
            var formatter = invocation.Arguments[4];

            var invokeMethod = formatter.GetType().GetMethod("Invoke");
            var actualMessage = (string?)invokeMethod?.Invoke(formatter, new[] { state, exception });

            logMessages.Add(new LogMessage
            {
                EventId = eventId,
                LogLevel = logLevel,
                Message = actualMessage,
                Exception = exception,
                State = state
            });
        }));
        return this;
    }
}
