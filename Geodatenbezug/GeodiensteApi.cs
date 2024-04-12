using System.Net.Http.Json;
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
}
