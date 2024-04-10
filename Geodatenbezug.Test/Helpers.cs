using System.Collections.ObjectModel;

namespace Geodatenbezug.Test;
internal static class Helpers
{
    internal static void AssertLogs(ReadOnlyCollection<LogMessage> logs, List<LogMessage> expectedLogs)
    {
        Assert.AreEqual(expectedLogs.Count, logs.Count);
        for (int i = 0; i < logs.Count; i++)
        {
            Assert.AreEqual(expectedLogs[i].LogLevel, logs[i].LogLevel);
            Assert.AreEqual(expectedLogs[i].Message, logs[i].Message);
        }
    }
}
