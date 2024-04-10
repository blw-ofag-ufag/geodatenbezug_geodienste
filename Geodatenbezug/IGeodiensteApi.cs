using BLW.Models;

namespace BLW;
public interface IGeodiensteApi
{
    Task<List<Topic>> RequestTopicInfoAsync();
}
