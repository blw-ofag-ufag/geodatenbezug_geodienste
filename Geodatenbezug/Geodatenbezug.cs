using Geodatenbezug.Models;
using MaxRev.Gdal.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using OSGeo.GDAL;

namespace Geodatenbezug;

/// <summary>
/// Azure Function for processing geodata.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Geodatenbezug"/> class.
/// </remarks>
public class Geodatenbezug(ILoggerFactory loggerFactory, Processor processing)
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Geodatenbezug>();
    private readonly Processor processing = processing;

    /// <summary>
    /// Durable function to orchestrate the processing of geodata.
    /// </summary>
    [Function(nameof(OrchestrateProcessing))]
    public async Task OrchestrateProcessing([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        try
        {
            logger.LogInformation("Start der Prozessierung");
            var parallelProcessingTasks = new List<Task<List<ProcessingResult>>>();
            var topics = await context.CallActivityAsync<List<Topic>>(nameof(RetrieveTopics)).ConfigureAwait(true);
            var cantonGroups = topics.GroupBy(t => t.Canton).ToList();

            foreach (var cantonTopics in cantonGroups)
            {
                // lwb_bewirtschaftungseinheit and lwb_nutzungsflaechen must be processed sequentially, but in parallel to other topics
                var bewirtschaftungseinheit = cantonTopics.FirstOrDefault(t => t.BaseTopic == BaseTopic.lwb_bewirtschaftungseinheit);
                var nutzungsflaechen = cantonTopics.FirstOrDefault(t => t.BaseTopic == BaseTopic.lwb_nutzungsflaechen);
                var shouldProcessTopicsSequentially = bewirtschaftungseinheit != null && nutzungsflaechen != null;
                if (shouldProcessTopicsSequentially)
                {
                    async Task<List<ProcessingResult>> ProcessSequentialTopics()
                    {
                        logger.LogInformation($"Prozessiere {bewirtschaftungseinheit.TopicTitle} ({bewirtschaftungseinheit.Canton}) und {nutzungsflaechen.TopicTitle} ({nutzungsflaechen.Canton}) sequenziell");
                        var results = new List<ProcessingResult>();
                        var bewirtschaftungseinheitResult = await context.CallActivityAsync<ProcessingResult>(nameof(ProcessTopic), bewirtschaftungseinheit).ConfigureAwait(true);
                        results.Add(bewirtschaftungseinheitResult);
                        var nutzungsflaechenResult = await context.CallActivityAsync<ProcessingResult>(nameof(ProcessTopic), nutzungsflaechen).ConfigureAwait(true);
                        results.Add(nutzungsflaechenResult);

                        return results;
                    }

                    parallelProcessingTasks.Add(ProcessSequentialTopics());
                }

                // Process remaining topics in parallel
                foreach (var topic in cantonTopics)
                {
                    // Skip already processed sequential topics
                    if (shouldProcessTopicsSequentially
                        && (topic.BaseTopic == BaseTopic.lwb_bewirtschaftungseinheit || topic.BaseTopic == BaseTopic.lwb_nutzungsflaechen))
                    {
                        continue;
                    }

                    async Task<List<ProcessingResult>> ProcessSingleTopic()
                    {
                        var result = await context.CallActivityAsync<ProcessingResult>(nameof(ProcessTopic), topic).ConfigureAwait(true);
                        return new List<ProcessingResult> { result };
                    }

                    parallelProcessingTasks.Add(ProcessSingleTopic());
                }
            }

            var results = await Task.WhenAll(parallelProcessingTasks).ConfigureAwait(true);
            var resultList = (results ?? []).SelectMany(r => r).ToList();
            if (resultList.Count > 0)
            {
                await context.CallActivityAsync(nameof(SendNotification), resultList).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler bei der Prozessierung");
            var errorResult = new ProcessingResult()
            {
                Code = System.Net.HttpStatusCode.InternalServerError,
                TopicTitle = "Prozessierungsfehler",
                Reason = ex.Message,
            };
            await context.CallActivityAsync(nameof(SendNotification), new List<ProcessingResult> { errorResult }).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Durable function to retrieve the topics to process.
    /// </summary>
    /// <param name="param">An unused parameter that is required by the azure function.</param>
    /// <returns>A list with the <see cref="Topic"/>s to process.</returns>
    [Function(nameof(RetrieveTopics))]
    public async Task<List<Topic>> RetrieveTopics([ActivityTrigger] string param)
    {
        return await processing.GetTopicsToProcess().ConfigureAwait(false);
    }

    /// <summary>
    /// Durable function to process a single topic.
    /// </summary>
    /// <param name="topic">The <see cref="Topic"/> to be processed.</param>
    /// <returns>The <see cref="ProcessingResult"/>.</returns>
    [Function(nameof(ProcessTopic))]
    public async Task<ProcessingResult> ProcessTopic([ActivityTrigger] Topic topic)
    {
        return await processing.ProcessTopic(topic).ConfigureAwait(false);
    }

    /// <summary>
    /// Durable function to send a notification with the processing results.
    /// </summary>
    /// <param name="results">A list with the <see cref="ProcessingResult"/>s.</param>
    [Function(nameof(SendNotification))]
    public void SendNotification([ActivityTrigger] List<ProcessingResult> results)
    {
        processing.SendEmail(results);
    }

    /// <summary>
    /// Time trigger function to start the processing.
    /// <param name="timeTrigger">An unused parameter that is required by the azure function.</param>
    /// <param name="client">The durable task client.</param>
    /// </summary>
    [Function(nameof(TriggerProcessing))]
    public async Task TriggerProcessing(
    [TimerTrigger("%TimeTriggerSchedule%", RunOnStartup = true)] TimerInfo timeTrigger,
    [DurableClient] DurableTaskClient client)
    {
        logger.LogInformation("Die Prozessierung wurde gestartet");
        GdalBase.ConfigureAll();
        logger.LogInformation("Verwendete GDAL-Version: " + Gdal.VersionInfo(null));

        await client.ScheduleNewOrchestrationInstanceAsync(nameof(OrchestrateProcessing)).ConfigureAwait(false);
    }
}
