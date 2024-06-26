﻿using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Polly;

namespace Geodatenbezug;

/// <summary>
/// Accesses the geodienste.ch API.
/// </summary>
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
public class GeodiensteApi(ILogger<GeodiensteApi> logger, IHttpClientFactory httpClientFactory) : IGeodiensteApi
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
{
    private const string GeodiensteBaseUrl = "https://geodienste.ch";

    /// <summary>
    /// Timeouts the execution for 1 minute before retrying.
    /// </summary>
    public virtual TimeSpan GetWaitDuration() => TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    public async Task<List<Topic>> RequestTopicInfoAsync()
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient(nameof(GeodiensteApi));
            var cantons = string.Join(",", Enum.GetValues(typeof(Canton)).Cast<Canton>().Select(e => e.ToString()));
            var baseTopics = string.Join(",", Enum.GetValues(typeof(BaseTopic)).Cast<BaseTopic>().Select(e => e.ToString()));
            var topics = string.Join(",", Enum.GetValues(typeof(BaseTopic)).Cast<BaseTopic>().Select(e => e.ToString() + "_v2_0"));
            var url = $"{GeodiensteBaseUrl}/info/services.json?base_topics={baseTopics}&topics={topics}&cantons={cantons}&language=de";
            logger.LogInformation($"Rufe die Themeninformationen ab: {url}");

            var infoData = await httpClient.GetFromJsonAsync<GeodiensteInfoData>(url).ConfigureAwait(false);
            return infoData.Services;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, $"Fehler beim Abrufen der Themeninformationen von geodienste.ch: {ex.Message}");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> StartExportAsync(Topic topic)
    {
        var token = GetToken(topic.BaseTopic, topic.Canton);
        var url = $"{GeodiensteBaseUrl}/downloads/{topic.BaseTopic}/{token}/export.json";
        logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Starte den Datenexport mit {url}");
        using var httpClient = httpClientFactory.CreateClient(nameof(GeodiensteApi));

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>((response) =>
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    var jsonString = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var errorResponse = JsonSerializer.Deserialize<GeodiensteExportError>(jsonString);
                    return errorResponse.Error.Equals(GeodiensteExportError.Pending, StringComparison.Ordinal);
                }

                return false;
            })
            .WaitAndRetryAsync(10, retryAttempt => GetWaitDuration(), (result, timeSpan, retryCount, context) =>
                {
                    var response = result.Result;
                    var jsonString = response.Content.ReadAsStringAsync().Result;
                    var errorResponse = JsonSerializer.Deserialize<GeodiensteExportError>(jsonString);
                    if (retryCount < 10)
                    {
                        logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Es läuft gerade ein anderer Export. Versuche es in 1 Minute erneut.");
                    }
                    else
                    {
                        logger.LogError($"{topic.TopicTitle} ({topic.Canton}): Es läuft bereits ein anderer Export. Zeitlimite überschritten.");
                    }
                });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await httpClient.GetAsync(url).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> CheckExportStatusAsync(Topic topic)
    {
        var token = GetToken(topic.BaseTopic, topic.Canton);
        var url = $"{GeodiensteBaseUrl}/downloads/{topic.BaseTopic}/{token}/status.json";
        logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Prüfe den Status des Datenexports mit {url}");
        using var httpClient = httpClientFactory.CreateClient(nameof(GeodiensteApi));

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>((response) =>
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var jsonString = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var statusResponse = JsonSerializer.Deserialize<GeodiensteStatusSuccess>(jsonString);
                    return statusResponse.Status == GeodiensteStatus.Queued || statusResponse.Status == GeodiensteStatus.Working;
                }

                return false;
            })
            .WaitAndRetryAsync(10, retryAttempt => GetWaitDuration(), (result, timeSpan, retryCount, context) =>
            {
                var response = result.Result;
                var jsonString = response.Content.ReadAsStringAsync().Result;
                var statusResponse = JsonSerializer.Deserialize<GeodiensteStatusSuccess>(jsonString);
                var statusString = statusResponse.Status == GeodiensteStatus.Queued ? "in der Warteschlange" : "in Bearbeitung";
                if (retryCount < 10)
                {
                    logger.LogInformation($"{topic.TopicTitle} ({topic.Canton}): Export ist {statusString}. Versuche es in 1 Minute erneut.");
                }
                else
                {
                    logger.LogError($"{topic.TopicTitle} ({topic.Canton}): Zeitlimite überschritten. Status ist {statusString}");
                }
            });

        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await httpClient.GetAsync(url).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> DownloadExportAsync(string downloadUrl, string destinationPath)
    {
        logger.LogInformation($"Lade die Daten herunter {downloadUrl}");
        Directory.CreateDirectory(destinationPath);
        var downloadedFilePath = string.Empty;
        using var httpClient = httpClientFactory.CreateClient(nameof(GeodiensteApi));
        using var stream = await httpClient.GetStreamAsync(downloadUrl).ConfigureAwait(false);
        using var archive = new ZipArchive(stream);
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.EndsWith(".gpkg", StringComparison.OrdinalIgnoreCase))
            {
                downloadedFilePath = Path.Combine(destinationPath, entry.Name);
                entry.ExtractToFile(downloadedFilePath, overwrite: true);
                break;
            }
        }

        if (string.IsNullOrEmpty(downloadedFilePath))
        {
            throw new FileNotFoundException("Keine GeoPackage-Datei im Archiv gefunden.");
        }

        return downloadedFilePath;
    }

    /// <summary>
    /// Gets the token for the given topic and canton from the environment variables.
    /// </summary>
    protected internal virtual string GetToken(BaseTopic baseTopic, Canton canton)
    {
        var topicTokens = Environment.GetEnvironmentVariable("tokens_" + baseTopic.ToString());
        if (string.IsNullOrEmpty(topicTokens))
        {
            throw new InvalidOperationException($"No tokens available for topic {baseTopic}");
        }

        return topicTokens.ExtractValueByKey(canton.ToString());
    }
}
