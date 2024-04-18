using System.Globalization;
using System.Net;
using System.Text.Json;
using Geodatenbezug.Models;
using Geodatenbezug.Topics;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug;

/// <summary>
/// Handles the processing of the topics.
/// </summary>
public class Processing(IGeodiensteApi geodiensteApi, ILogger<Processing> logger)
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
        try
        {
            logger.LogInformation($"Verarbeite Thema {topic.TopicTitle} ({topic.Canton})...");
            var token = GetToken(topic.BaseTopic, topic.Canton);

            var exportResponse = await geodiensteApi.StartExportAsync(topic, token).ConfigureAwait(false);
            if (!exportResponse.IsSuccessStatusCode)
            {
                var exportResponseContent = await exportResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var errorMessage = JsonSerializer.Deserialize<GeodiensteExportError>(exportResponseContent);
                var errorString = exportResponse.StatusCode == HttpStatusCode.Unauthorized ? exportResponse.ReasonPhrase : errorMessage.Error;
                logger.LogError($"Fehler beim Starten des Exports für Thema {topic.TopicTitle} ({topic.Canton}): {exportResponse.StatusCode} - {errorString}");

                return new ProcessingResult
                {
                    Code = exportResponse.StatusCode,
                    Reason = exportResponse.ReasonPhrase,
                    Info = exportResponse.StatusCode == HttpStatusCode.Unauthorized ? string.Empty : errorMessage.Error,
                    TopicTitle = topic.TopicTitle,
                    Canton = topic.Canton,
                };
            }

            var statusResponse = await geodiensteApi.CheckExportStatusAsync(topic, token).ConfigureAwait(false);
            var statusResponseContent = await statusResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!statusResponse.IsSuccessStatusCode)
            {
                var errorMessage = JsonSerializer.Deserialize<GeodiensteStatusError>(statusResponseContent);
                logger.LogError($"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): {statusResponse.StatusCode} - {errorMessage.Error}");

                return new ProcessingResult
                {
                    Code = statusResponse.StatusCode,
                    Reason = statusResponse.ReasonPhrase,
                    Info = statusResponse.StatusCode == HttpStatusCode.NotFound ? errorMessage.Error : string.Empty,
                    TopicTitle = topic.TopicTitle,
                    Canton = topic.Canton,
                };
            }

            var statusMessage = JsonSerializer.Deserialize<GeodiensteStatusSuccess>(statusResponseContent);
            if (statusMessage.Status == GeodiensteStatus.Failed)
            {
                logger.LogError($"Fehler bei der Statusabfrage des Datenexports für Thema {topic.TopicTitle} ({topic.Canton}): {statusMessage.Info}");

                return new ProcessingResult
                {
                    Code = statusResponse.StatusCode,
                    Reason = statusMessage.Status.ToString(),
                    Info = statusMessage.Info,
                    TopicTitle = topic.TopicTitle,
                    Canton = topic.Canton,
                };
            }

            if (statusMessage.DownloadUrl == null)
            {
                logger.LogError($"Kein Download-Link für Thema {topic.TopicTitle} ({topic.Canton}) vorhanden");

                return new ProcessingResult
                {
                    Code = HttpStatusCode.NotFound,
                    Reason = "Download link not found",
                    TopicTitle = topic.TopicTitle,
                    Canton = topic.Canton,
                };
            }

            var downloadLink = string.Empty;
            switch (topic.BaseTopic)
            {
                case BaseTopic.lwb_perimeter_ln_sf:
                    downloadLink = new PerimeterLnSf(statusMessage.DownloadUrl).Process();
                    break;
                case BaseTopic.lwb_rebbaukataster:
                    downloadLink = new Rebbaukataster(statusMessage.DownloadUrl).Process();
                    break;
                case BaseTopic.lwb_perimeter_terrassenreben:
                    downloadLink = new PerimeterTerrassenreben(statusMessage.DownloadUrl).Process();
                    break;
                case BaseTopic.lwb_biodiversitaetsfoerderflaechen:
                    downloadLink = new Biodiversitaetsfoerderflaechen(statusMessage.DownloadUrl).Process();
                    break;
                case BaseTopic.lwb_bewirtschaftungseinheit:
                    downloadLink = new Bewirtschaftungseinheit(statusMessage.DownloadUrl).Process();
                    break;
                case BaseTopic.lwb_nutzungsflaechen:
                    downloadLink = new Nutzungsflaechen(statusMessage.DownloadUrl).Process();
                    break;
            }

            return new ProcessingResult
            {
                Code = HttpStatusCode.OK,
                Reason = "Success",
                Info = "Processing completed",
                TopicTitle = topic.TopicTitle,
                Canton = topic.Canton,
                DownloadUrl = downloadLink,
            };
        }
        catch (KeyNotFoundException)
        {
            logger.LogError($"Kein Token für Thema {topic.TopicTitle} ({topic.Canton}) vorhanden");
            return new ProcessingResult
            {
                Code = HttpStatusCode.NotFound,
                Reason = "Token not found",
                TopicTitle = topic.TopicTitle,
                Canton = topic.Canton,
            };
        }
        catch (Exception e)
        {
            logger.LogError($"Fehler bei der Verarbeitung des Themas {topic.TopicTitle} ({topic.Canton}): {e.Message}");
            return new ProcessingResult
            {
                Code = HttpStatusCode.InternalServerError,
                Reason = e.Message,
                TopicTitle = topic.TopicTitle,
                Canton = topic.Canton,
            };
        }
    }

    /// <summary>
    /// Gets the token for the given topic and canton from the environment variables.
    /// </summary>
    public string GetToken(BaseTopic topic, Canton canton)
    {
        var topicTokens = Environment.GetEnvironmentVariable("tokens_" + topic.ToString());
        var selectedToken = topicTokens.Split(";").Where(token => token.StartsWith(canton.ToString(), StringComparison.InvariantCulture)).FirstOrDefault();
        if (selectedToken != null)
        {
            return selectedToken.Split("=")[1];
        }
        else
        {
            throw new KeyNotFoundException($"Token not found for topic {topic} and canton {canton}");
        }
    }
}
