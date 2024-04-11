﻿using System.Text.Json;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug;
public class GeodiensteApi(ILogger<GeodiensteApi> logger) : IGeodiensteApi
{
    private const string GEODIENSTE_BASE_URL = "https://geodienste.ch";

    public Func<HttpClient> GetHttpClient = () => new HttpClient();

    public async Task<List<Topic>> RequestTopicInfoAsync()
    {
        var cantons = string.Join(",", Enum.GetValues(typeof(Canton))
                                            .Cast<Canton>()
                                            .Select(e => e.ToString()));
        var baseTopics = string.Join(",", Enum.GetValues(typeof(BaseTopic))
                                            .Cast<BaseTopic>()
                                            .Select(e => e.ToString()));
        var topics = string.Join(",", Enum.GetValues(typeof(BaseTopic))
                                            .Cast<BaseTopic>()
                                            .Select(e => e.ToString() + "_v2_0"));
        var url = $"{GEODIENSTE_BASE_URL}/info/services.json?base_topics={baseTopics}&topics={topics}&cantons={cantons}&language=de";
        var response = await GetHttpClient().GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Fehler beim Abrufen der Themeninformationen von geodienste.ch: {response.StatusCode}  - {response.ReasonPhrase}");
            return [];
        }
        var jsonString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GeodiensteInfoData>(jsonString);
        return result.Services;
    }
}
