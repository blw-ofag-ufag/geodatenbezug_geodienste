using System.Text.Json;
using Geodatenbezug.Models;
using MaxRev.Gdal.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using OSGeo.GDAL;

namespace Geodatenbezug;

public class Geodatenbezug
{
    private readonly ILogger logger;
    private readonly Processing processing;

    public Geodatenbezug(ILoggerFactory loggerFactory, Processing processing)
    {
        logger = loggerFactory.CreateLogger<Geodatenbezug>();
        this.processing = processing;
    }

    [Function(nameof(OrchestrateProcessing))]
    public async Task OrchestrateProcessing([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        try
        {
            logger.LogInformation("Start der Prozessierung...");
            var topicsString = await context.CallActivityAsync<string>(nameof(RetrieveTopics)).ConfigureAwait(false);
            var topics = JsonSerializer.Deserialize<List<Topic>>(topicsString);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler in {nameof(OrchestrateProcessing)}: {ex}");
        }
    }

    [Function(nameof(RetrieveTopics))]
    public async Task<string> RetrieveTopics([ActivityTrigger] string test)
    {
        try
        {
            logger.LogInformation("Laden der Themen...");
            var topics = await processing.GetTopicsToUpdate().ConfigureAwait(false);
            return JsonSerializer.Serialize(topics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler in {nameof(RetrieveTopics)}: {ex}");
            return string.Empty;
        }
    }

    [Function(nameof(TriggerProcessing))]
    public async Task TriggerProcessing([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
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
