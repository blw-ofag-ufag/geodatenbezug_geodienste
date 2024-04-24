using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// Factory to create a <see cref="ITopicProcessor"/> for a given <see cref="Topic"/>.
/// </summary>
public static class TopicProcessorFactory
{
    /// <summary>
    /// Creates a <see cref="ITopicProcessor"/> for the given <see cref="Topic"/>.
    /// </summary>
    public static ITopicProcessor Create(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic)
    {
        return topic.BaseTopic switch
        {
            BaseTopic.lwb_perimeter_ln_sf => new PerimeterLnSfProcessor(geodiensteApi, logger, topic),
            BaseTopic.lwb_rebbaukataster => new RebbaukatasterProcessor(geodiensteApi, logger, topic),
            BaseTopic.lwb_perimeter_terrassenreben => new PerimeterTerrassenrebenProcessor(geodiensteApi, logger, topic),
            BaseTopic.lwb_biodiversitaetsfoerderflaechen => new BiodiversitaetsfoerderflaechenProcessor(geodiensteApi, logger, topic),
            BaseTopic.lwb_bewirtschaftungseinheit => new BewirtschaftungseinheitProcessor(geodiensteApi, logger, topic),
            BaseTopic.lwb_nutzungsflaechen => new NutzungsflaechenProcessor(geodiensteApi, logger, topic),
            _ => throw new InvalidOperationException($"Unknown topic {topic.BaseTopic}"),
        };
    }
}
