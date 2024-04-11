using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug;

public class GeodiensteApi(ILogger<GeodiensteApi> logger, IHttpClientFactory httpClientFactory) : IGeodiensteApi
{
    private const string GEODIENSTE_BASE_URL = "https://geodienste.ch";

    public async Task<List<Topic>> RequestTopicInfoAsync()
    {
        logger.LogInformation("Rufe die Themeninformationen ab...");
        using var httpClient = httpClientFactory.CreateClient(nameof(GeodiensteApi));
        var cantons = string.Join(",", Enum.GetValues(typeof(Canton)).Cast<Canton>().Select(e => e.ToString()));
        var baseTopics = string.Join(",", Enum.GetValues(typeof(BaseTopic)).Cast<BaseTopic>().Select(e => e.ToString()));
        var topics = string.Join(",", Enum.GetValues(typeof(BaseTopic)).Cast<BaseTopic>().Select(e => e.ToString() + "_v2_0"));
        var url = $"{GEODIENSTE_BASE_URL}/info/services.json?base_topics={baseTopics}&topics={topics}&cantons={cantons}&language=de";

        var httpResponse = await httpClient.GetAsync(url).ConfigureAwait(false);
        if (!httpResponse.IsSuccessStatusCode)
        {
            logger.LogError($"Fehler beim Abrufen der Themeninformationen von geodienste.ch: {httpResponse.StatusCode}  - {httpResponse.ReasonPhrase}");
            return [];
        }
        var jsonString = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<GeodiensteInfoData>(jsonString);
        return result.Services;
    }
}
