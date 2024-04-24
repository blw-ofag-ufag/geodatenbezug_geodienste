﻿using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Nutzungsflächen".
/// </summary>
public class NutzungsflaechenProcessor(IGeodiensteApi geodiensteApi, IAzureStorage azureStorage, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, azureStorage, logger, topic)
{
    private string bewirtschaftungseinheitDataPath = string.Empty;

    /// <inheritdoc />
    protected internal override async Task PrepareDataAsync()
    {
        Logger.LogInformation($"Bereite Daten für die Prozessierung von {Topic.TopicTitle} ({Topic.Canton}) vor...");

        var tasks = new List<Task>
            {
                Task.Run(async () =>
                {
                    var downloadUrl = await ExportTopicAsync(Topic).ConfigureAwait(false);
                    InputDataPath = await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
                }),
                Task.Run(async () =>
                {
                    var bewirtschaftungseinheitTopic = new Topic()
                    {
                        TopicTitle = BaseTopic.lwb_bewirtschaftungseinheit.GetDescription(),
                        Canton = Topic.Canton,
                        TopicName = BaseTopic.lwb_bewirtschaftungseinheit.ToString() + "_v2_0",
                        BaseTopic = BaseTopic.lwb_bewirtschaftungseinheit,
                    };
                    var downloadUrl = await ExportTopicAsync(bewirtschaftungseinheitTopic).ConfigureAwait(false);
                    bewirtschaftungseinheitDataPath = await GeodiensteApi.DownloadExportAsync(downloadUrl, DataDirectory).ConfigureAwait(false);
                }),
            };
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override Task ProcessTopic()
    {
        throw new NotImplementedException();
    }
}
