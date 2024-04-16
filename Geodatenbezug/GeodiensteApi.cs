using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

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
    public virtual Task WaitBeforeRetry() => Task.Delay(TimeSpan.FromMinutes(1));

    private AuthenticationHeaderValue GetAuthenticationHeader()
    {
        var username = Environment.GetEnvironmentVariable("AuthUser");
        var password = Environment.GetEnvironmentVariable("AuthPw");
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("AuthUser and AuthPw environment variables must be set.");
        }

        var encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        return new AuthenticationHeaderValue("Basic", encodedCredentials);
    }

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
            logger.LogError($"Fehler beim Abrufen der Themeninformationen von geodienste.ch: {ex.Message}");
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly
            return [];
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> StartExportAsync(Topic topic, string token) => StartExportAsync(topic, token, 0);

    private async Task<HttpResponseMessage> StartExportAsync(Topic topic, string token, int attempts)
    {
        var url = $"{GeodiensteBaseUrl}/downloads/{topic.BaseTopic}/{token}/export.json";
        logger.LogInformation($"Starte den Datenexport für {topic.TopicTitle} ({topic.Canton}) mit {url}...");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = GetAuthenticationHeader();
        using var httpClient = httpClientFactory.CreateClient(nameof(GeodiensteApi));
        var httpResponse = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (httpResponse.StatusCode == HttpStatusCode.NotFound)
        {
            var jsonString = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var errorResponse = JsonSerializer.Deserialize<GeodiensteExportError>(jsonString);
            if (errorResponse.Error.Equals(GeodiensteExportError.Pending, StringComparison.Ordinal))
            {
                if (attempts < 10)
                {
                    logger.LogInformation("Es läuft gerade ein anderer Export. Versuche es in 1 Minute erneut.");
                    await WaitBeforeRetry().ConfigureAwait(false);
                    return await StartExportAsync(topic, token, attempts + 1).ConfigureAwait(false);
                }
                else
                {
                    logger.LogError("Es läuft bereits ein anderer Export. Zeitlimite überschritten.");
                    return httpResponse;
                }
            }
        }

        return httpResponse;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> CheckExportStatusAsync(Topic topic, string token) => CheckExportStatusAsync(topic, token, 0);

    private async Task<HttpResponseMessage> CheckExportStatusAsync(Topic topic, string token, int attempts)
    {
        var url = $"{GeodiensteBaseUrl}/downloads/{topic.BaseTopic}/{token}/status.json";
        logger.LogInformation($"Prüfe den Status des Datenexports für {topic.TopicTitle} ({topic.Canton}) mit {url}...");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = GetAuthenticationHeader();
        using var httpClient = httpClientFactory.CreateClient(nameof(GeodiensteApi));
        var httpResponse = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (httpResponse.StatusCode == HttpStatusCode.OK)
        {
            var jsonString = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var statusResponse = JsonSerializer.Deserialize<GeodiensteStatusSuccess>(jsonString);
            if (statusResponse.Status == GeodiensteStatus.Queued || statusResponse.Status == GeodiensteStatus.Working)
            {
                var statusString = statusResponse.Status == GeodiensteStatus.Queued ? "in der Warteschlange" : "in Bearbeitung";

                if (attempts < 10)
                {
                    logger.LogInformation($"Export ist {statusString}. Versuche es in 1 Minute erneut.");
                    await WaitBeforeRetry().ConfigureAwait(false);
                    return await CheckExportStatusAsync(topic, token, attempts + 1).ConfigureAwait(false);
                }
                else
                {
                    logger.LogError($"Zeitlimite überschritten. Status ist {statusString}");
                    return httpResponse;
                }
            }
        }

        return httpResponse;
    }
}
