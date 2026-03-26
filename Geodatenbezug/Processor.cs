using System.Globalization;
using Geodatenbezug.Models;
using Geodatenbezug.Processors;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug;

/// <summary>
/// Handles the processing of the topics.
/// </summary>
public class Processor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger<Processor> logger, IMailService mailService)
{
    /// <summary>
    /// Gets the topics that have new data and need to be processed.
    /// </summary>
    public async Task<List<Topic>> GetTopicsToProcess()
    {
        logger.LogInformation("Laden der Themen...");

        var topics = await geodiensteApi.RequestTopicInfoAsync().ConfigureAwait(false);
        var topicsToProcess = new List<Topic>();
        foreach (var topic in topics)
        {
            if (topic.UpdatedAt.HasValue && topic.PublicationData != "Im Aufbau")
            {
                var updatedAtString = topic.UpdatedAt.Value.ToString("G", CultureInfo.GetCultureInfo("de-CH"));

                var lastProcessed = await azureStorage.GetLastProcessed(topic).ConfigureAwait(false);
                if (lastProcessed == null || lastProcessed < topic.UpdatedAt.Value)
                {
                    logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Thema wurde am {updatedAtString} aktualisiert und wird verarbeitet");
                    topicsToProcess.Add(topic);
                }
                else
                {
                    logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Thema wurde seit {updatedAtString} nicht aktualisiert");
                }
            }
            else
            {
                logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Thema ist nicht verfügbar");
            }
        }

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

    /// <summary>
    /// Sends an email with the processing results.
    /// </summary>
    public void SendEmail(List<ProcessingResult> results)
    {
        mailService.SendProcessingResults(results);
    }
}
