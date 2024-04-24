using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Perimeter Terrassenreben".
/// </summary>
public class PerimeterTerrassenrebenProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
}
