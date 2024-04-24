using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Bewirtschaftungseinheit".
/// </summary>
public class BewirtschaftungseinheitProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
}
