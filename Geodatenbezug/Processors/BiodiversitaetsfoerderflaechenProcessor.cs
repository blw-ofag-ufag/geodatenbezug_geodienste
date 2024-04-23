using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug.Processors;

/// <summary>
/// The processor for the topic "Biodiversitätsförderflächen".
/// </summary>
public class BiodiversitaetsfoerderflaechenProcessor(IGeodiensteApi geodiensteApi, ILogger logger, Topic topic) : TopicProcessor(geodiensteApi, logger, topic)
{
}
