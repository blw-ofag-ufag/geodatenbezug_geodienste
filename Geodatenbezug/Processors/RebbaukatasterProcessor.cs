using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Rebbaukataster".
/// </summary>
public class RebbaukatasterProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
}
