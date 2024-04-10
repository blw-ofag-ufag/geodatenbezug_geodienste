using Geodatenbezug.Models;

namespace Geodatenbezug;
public interface IGeodiensteApi
{
    Task<List<Topic>> RequestTopicInfoAsync();
}
