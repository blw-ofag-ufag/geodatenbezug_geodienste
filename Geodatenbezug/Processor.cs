using System.Globalization;
using Geodatenbezug.Models;
using Geodatenbezug.Processors;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug;

/// <summary>
/// Handles the processing of the topics.
/// </summary>
public class Processor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger<Processor> logger)
{
    /// <summary>
    /// Gets the topics that have new data and need to be processed.
    /// </summary>
    public async Task<List<Topic>> GetTopicsToProcess()
    {
        var topics = await geodiensteApi.RequestTopicInfoAsync().ConfigureAwait(false);
        var currentTime = DateTime.Now;
        var topicsToProcess = topics.FindAll(topic =>
        {
            if (topic.UpdatedAt.HasValue)
            {
                var updatedAtString = topic.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var timeDifference = currentTime - topic.UpdatedAt.Value;
                if (timeDifference.Days < 1)
                {
                    logger.LogInformation($"Thema {topic.TopicTitle} ({topic.Canton}) wurde am {updatedAtString} aktualisiert und wird verarbeitet");
                    return true;
                }
                else
                {
                    logger.LogInformation($"Thema {topic.TopicTitle} ({topic.Canton}) wurde seit {updatedAtString} nicht aktualisiert");
                    return false;
                }
            }
            else
            {
                logger.LogInformation($"Thema {topic.TopicTitle} ({topic.Canton}) ist nicht verfügbar");
                return false;
            }
        });

        var topicsProcessedMessage = topicsToProcess.Count != 1 ? "Themen werden" : "Thema wird";
        logger.LogInformation($"{topicsToProcess.Count} {topicsProcessedMessage} prozessiert");
        return topicsToProcess;
    }

    /// <summary>
    /// Processes the given topic: Downloads the data from geodienste.ch, processes it and uploads it to Azure Blob Storage.
    /// </summary>
    public async Task<ProcessingResult> ProcessTopic(Topic topic)
    {
        var topicProcessor = TopicProcessorFactory.Create(geodiensteApi, azureStorage, logger, topic);
        return await topicProcessor.ProcessAsync().ConfigureAwait(false);
    }
}
