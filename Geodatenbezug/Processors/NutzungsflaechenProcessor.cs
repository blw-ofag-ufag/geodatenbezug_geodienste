using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Nutzungsflächen".
/// </summary>
public class NutzungsflaechenProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
    /// <inheritdoc />
    protected internal override async Task PrepareData()
    {
        Logger.LogInformation($"Bereite Daten für die Prozessierung von {Topic.TopicTitle} ({Topic.Canton}) vor...");

        var tasks = new List<Task>
            {
                Task.Run(async () =>
                {
                    var downloadUrl = await ExportTopicAsync(Topic).ConfigureAwait(false);

                    // TODO: Download data from downloadUrl
                }),
                Task.Run(async () =>
                {
                    var bewirtschaftungseinheitTopic = new Topic()
                    {
                        TopicTitle = BaseTopic.lwb_bewirtschaftungseinheit.GetDescription(),
                        Canton = Topic.Canton,
                        BaseTopic = BaseTopic.lwb_bewirtschaftungseinheit,
                    };
                    var downloadUrl = await ExportTopicAsync(bewirtschaftungseinheitTopic).ConfigureAwait(false);

                    // TODO: Download data from downloadUrl
                }),
            };
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
