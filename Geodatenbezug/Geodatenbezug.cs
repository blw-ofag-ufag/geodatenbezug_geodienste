using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using BLW.Models;
using System.Text.Json;

namespace BLW;

public class Geodatenbezug
{
    private readonly ILogger _logger;
    private readonly Processing _processing;

    public Geodatenbezug(ILoggerFactory loggerFactory, Processing processing)
    {
        _logger = loggerFactory.CreateLogger<Geodatenbezug>();
        _processing = processing;
    }

    [Function(nameof(OrchestrateProcessing))]
    public async Task OrchestrateProcessing([OrchestrationTrigger] TaskOrchestrationContext context, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(OrchestrateProcessing));
        try
        {
            logger.LogInformation("Start der Prozessierung...");
            var topicsString = await context.CallActivityAsync<string>(nameof(RetrieveTopics));
            var topics = JsonSerializer.Deserialize<List<Topic>>(topicsString);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler in {nameof(OrchestrateProcessing)}: {ex}");
        }
    }

    [Function(nameof(RetrieveTopics))]
    public async Task<string> RetrieveTopics([ActivityTrigger] string test, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(RetrieveTopics));
        try
        {
            logger.LogInformation("Laden der Themen...");
            var topics = await _processing.GetTopicsToUpdate();
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
    [DurableClient] DurableTaskClient client,
    FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(TriggerProcessing));
        try
        {
            logger.LogInformation("Die Prozessierung wurde gestartet");
            GdalBase.ConfigureAll();
            logger.LogInformation("Verwendete GDAL-Version: " + Gdal.VersionInfo(null));

            await client.ScheduleNewOrchestrationInstanceAsync(nameof(OrchestrateProcessing));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler in {nameof(TriggerProcessing)}: {ex}");
        }   
    }
}
