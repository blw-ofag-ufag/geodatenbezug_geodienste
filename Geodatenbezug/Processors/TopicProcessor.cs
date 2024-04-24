using System.Net;
using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// Represents a processor for a specific topic.
/// </summary>
public abstract class TopicProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : ITopicProcessor
{
    private readonly string dataDirectory = Path.Combine(Path.GetTempPath(), "Geodatenbezug", topic.Canton.ToString(), topic.BaseTopic.ToString());

    /// <summary>
    /// The directory where the data is stored for processing.
    /// </summary>
    protected string DataDirectory => dataDirectory;

    private string inputData = string.Empty;

    /// <summary>
    /// The input data for processing.
    /// </summary>
    protected string InputData
    {
        get { return inputData; }
        set { inputData = value; }
    }

    /// <summary>
    /// The geodienste.ch API.
    /// </summary>
    protected IGeodiensteApi GeodiensteApi => geodiensteApi;

    /// <summary>
    /// The logger for the processor.
    /// </summary>
    protected ILogger Logger => logger;

    /// <summary>
    /// The topic that is being processed.
    /// </summary>
    protected Topic Topic => topic;

    private readonly ProcessingResult processingResult = new ()
    {
        Code = HttpStatusCode.Processing,
        Canton = topic.Canton,
        TopicTitle = topic.TopicTitle,
    };

    /// <summary>
    /// The processing result of the topic.
    /// </summary>
    protected internal ProcessingResult ProcessingResult => processingResult;

    /// <inheritdoc />
    public async Task<ProcessingResult> ProcessAsync()
    {
        try
        {
            logger.LogInformation($"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");

            await PrepareData().ConfigureAwait(false);

            // TODO: Process data and upload data to storage.
        }
        catch (Exception ex)
        {
            if (processingResult.Code == HttpStatusCode.Processing)
            {
                logger.LogError(ex, $"Fehler beim Verarbeiten des Themas {topic.TopicTitle} ({topic.Canton})");

                processingResult.Code = HttpStatusCode.InternalServerError;
                processingResult.Reason = ex.Message;
                processingResult.Info = ex.InnerException?.Message;
            }
        }

        return processingResult;
    }

    /// <summary>
    /// Prepares the data for processing.
    /// </summary>
    protected internal virtual async Task PrepareData()
    {
        logger.LogInformation($"Bereite Daten für die Prozessierung von {topic.TopicTitle} ({topic.Canton}) vor...");
        var downloadUrl = await ExportTopicAsync(topic).ConfigureAwait(false);
        InputData = await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports the provided topic from geodienste.ch.
    /// </summary>
    protected internal async Task<string> ExportTopicAsync(Topic topic)
    {
        var token = GetToken(topic.BaseTopic, topic.Canton);
        var exportResponse = await GeodiensteApi.StartExportAsync(topic, token).ConfigureAwait(false);
        if (!exportResponse.IsSuccessStatusCode)
        {
            var exportResponseContent = await exportResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var errorMessage = JsonSerializer.Deserialize<GeodiensteExportError>(exportResponseContent);
            if (!errorMessage.Error.Contains(GeodiensteExportError.OnlyOneExport, StringComparison.CurrentCulture))
            {
                var errorString = exportResponse.StatusCode == HttpStatusCode.Unauthorized ? exportResponse.ReasonPhrase : errorMessage.Error;
                logger.LogError($"Fehler beim Starten des Exports für Thema {topic.TopicTitle} ({topic.Canton}): {exportResponse.StatusCode} - {errorString}");

                processingResult.Code = exportResponse.StatusCode;
                processingResult.Reason = exportResponse.ReasonPhrase;
                processingResult.Info = exportResponse.StatusCode == HttpStatusCode.Unauthorized ? string.Empty : errorMessage.Error;
                throw new InvalidOperationException("Export failed");
            }
        }

        var statusResponse = await GeodiensteApi.CheckExportStatusAsync(topic, token).ConfigureAwait(false);
        var statusResponseContent = await statusResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!statusResponse.IsSuccessStatusCode)
        {
            var errorMessage = JsonSerializer.Deserialize<GeodiensteStatusError>(statusResponseContent);
            logger.LogError($"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): {statusResponse.StatusCode} - {errorMessage.Error}");

            processingResult.Code = statusResponse.StatusCode;
            processingResult.Reason = statusResponse.ReasonPhrase;
            processingResult.Info = statusResponse.StatusCode == HttpStatusCode.NotFound ? errorMessage.Error : string.Empty;
            throw new InvalidOperationException("Export failed");
        }

        var statusMessage = JsonSerializer.Deserialize<GeodiensteStatusSuccess>(statusResponseContent);
        if (statusMessage.Status == GeodiensteStatus.Failed)
        {
            logger.LogError($"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): {statusMessage.Info}");

            processingResult.Code = statusResponse.StatusCode;
            processingResult.Reason = statusMessage.Status.ToString();
            processingResult.Info = statusMessage.Info;
            throw new InvalidOperationException("Export failed");
        }

        if (statusMessage.DownloadUrl == null)
        {
            logger.LogError($"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): Download-URL nicht gefunden");

            processingResult.Code = statusResponse.StatusCode;
            processingResult.Reason = statusMessage.Status.ToString();
            processingResult.Info = "Download-URL not found";
            throw new InvalidOperationException("Export failed");
        }

        return statusMessage.DownloadUrl;
    }
}
