﻿using System.Text.Json;
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
public class Geodatenbezug(ILoggerFactory loggerFactory, Processing processing)
{
    private readonly ILogger logger = loggerFactory.CreateLogger<Geodatenbezug>();
    private readonly Processing processing = processing;

    /// <summary>
    /// Durable function to orchestrate the processing of geodata.
    /// </summary>
    [Function(nameof(OrchestrateProcessing))]
    public async Task OrchestrateProcessing([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        try
        {
            logger.LogInformation("Start der Prozessierung...");
            var topics = await context.CallActivityAsync<List<Topic>>(nameof(RetrieveTopics)).ConfigureAwait(true);
            var results = new List<ProcessingResult>();
            foreach (var topic in topics)
            {
                var result = await context.CallActivityAsync<ProcessingResult?>(nameof(ProcessTopic), topic).ConfigureAwait(true);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler in {nameof(OrchestrateProcessing)}: {ex}");
        }
    }

    /// <summary>
    /// Durable function to retrieve the topics to process.
    /// </summary>
    /// <param name="param">An unused parameter that is required by the azure function.</param>
    /// <returns>A JSON string with the <see cref="Topic"/>s to process.</returns>
    [Function(nameof(RetrieveTopics))]
    public async Task<List<Topic>?> RetrieveTopics([ActivityTrigger] string param)
    {
        try
        {
            logger.LogInformation("Laden der Themen...");
            return await processing.GetTopicsToProcess().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler in {nameof(RetrieveTopics)}: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Durable function to process a single topic.
    /// </summary>
    [Function(nameof(ProcessTopic))]
    public async Task<ProcessingResult?> ProcessTopic([ActivityTrigger] Topic topic)
    {
        try
        {
            return await processing.ProcessTopic(topic).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler in {nameof(RetrieveTopics)}: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Time trigger function to start the processing.
    /// <param name="timeTrigger">An unused parameter that is required by the azure function.</param>
    /// <param name="client">The durable task client.</param>
    /// </summary>
    [Function(nameof(TriggerProcessing))]
    public async Task TriggerProcessing(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timeTrigger,
        [DurableClient] DurableTaskClient client)
    {
        try
        {
            logger.LogInformation("Die Prozessierung wurde gestartet");
            GdalBase.ConfigureAll();
            logger.LogInformation("Verwendete GDAL-Version: " + Gdal.VersionInfo(null));

            await client.ScheduleNewOrchestrationInstanceAsync(nameof(OrchestrateProcessing)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler in {nameof(TriggerProcessing)}: {ex}");
        }
    }
}
