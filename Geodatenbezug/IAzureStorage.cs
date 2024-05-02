using Geodatenbezug.Models;

namespace Geodatenbezug;

/// <summary>
/// Interface for Azure Storage.
/// </summary>
public interface IAzureStorage
{
    /// <summary>
    /// Gets the date of the last processed file for the given topic.
    /// </summary>
    Task<DateTime?> GetLastProcessed(Topic topic);

    /// <summary>
    /// Uploads the file to the specified container and return the download link.
    /// </summary>
    Task<string> UploadFileAsync(string storageFilePath, string localFilePath);
}
