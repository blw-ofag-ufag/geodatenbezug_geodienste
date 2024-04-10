using BLW.Models;
using Microsoft.Extensions.Logging;

namespace BLW;
public class Processing(IGeodiensteApi geodiensteApi, ILogger<Processing> logger)
{
    public async Task<List<Topic>> GetTopicsToUpdate()
    {
        var topics = await geodiensteApi.RequestTopicInfoAsync();
        var currentTime = DateTime.Now;
        var topicsToProcess = topics.FindAll((topic) =>
        {
            if (topic.UpdatedAt != null)
            {
                var updatedAt = (DateTime)topic.UpdatedAt;
                var updatedAtString = updatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                var timeDifference = currentTime - updatedAt;
                if (timeDifference.Days < 1)
                {
                    logger.LogInformation($"Thema {topic.TopicTitle} ({topic.Canton}) wurde am {updatedAtString} aktualisiert und wird verarbeitet");
                    return true;
                }
                else
                {
                    logger.LogInformation($"Thema {topic.TopicTitle} ({topic.Canton}) wurde seit {updatedAtString} nicht aktualisiert");
                    return false;
                }
            }
            else
            {
                logger.LogInformation($"Thema {topic.TopicTitle} ({topic.Canton}) ist nicht verfügbar");
                return false;
            }
        });
        string topicsProcessedMessage = topicsToProcess.Count != 1 ? "Themen werden" : "Thema wird";
        logger.LogInformation($"{topicsToProcess.Count} {topicsProcessedMessage} prozessiert");
        return topicsToProcess;
    }
}
