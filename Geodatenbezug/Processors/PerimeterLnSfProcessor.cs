using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Perimeter Ln Sf".
/// </summary>
public class PerimeterLnSfProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
}
