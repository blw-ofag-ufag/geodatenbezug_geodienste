﻿using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Nutzungsflächen".
/// </summary>
public class NutzungsflaechenProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
    private string bewirtschaftungseinheitDataPath = string.Empty;

    /// <inheritdoc />
    protected internal override async Task PrepareDataAsync()
    {
        Logger.LogInformation($"Bereite Daten für die Prozessierung von {Topic.TopicTitle} ({Topic.Canton}) vor...");

        var exportInputTopic = PrepareTopic(Topic);

        var bewirtschaftungseinheitTopic = new Topic()
        {
            TopicTitle = BaseTopic.lwb_bewirtschaftungseinheit.GetDescription(),
            Canton = Topic.Canton,
            BaseTopic = BaseTopic.lwb_bewirtschaftungseinheit,
        };
        var exportBewirtschaftungseinheitTopic = PrepareTopic(bewirtschaftungseinheitTopic);

        var downloadUrls = await Task.WhenAll(exportInputTopic, exportBewirtschaftungseinheitTopic).ConfigureAwait(false);

        InputDataPath = downloadUrls[0];
        bewirtschaftungseinheitDataPath = downloadUrls[1];
    }

    private async Task<string> PrepareTopic(Topic topic)
    {
        var downloadUrl = await ExportTopicAsync(topic).ConfigureAwait(false);
        return await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
    }
}
