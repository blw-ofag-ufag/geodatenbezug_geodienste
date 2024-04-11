using Geodatenbezug.Models;

namespace Geodatenbezug;

/// <summary>
/// Interface for the geodienste.ch API.
/// </summary>
public interface IGeodiensteApi
{
    /// <summary>
    /// Gets the information about all the topics from geodienste.ch.
    /// </summary>
    Task<List<Topic>> RequestTopicInfoAsync();
}
