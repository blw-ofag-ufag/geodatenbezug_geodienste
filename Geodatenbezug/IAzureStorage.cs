namespace Geodatenbezug;

/// <summary>
/// Interface for Azure Storage.
/// </summary>
public interface IAzureStorage
{
    /// <summary>
    /// Uploads the file to the specified container and return the download link.
    /// </summary>
    Task<string> UploadFileAsync(string storageFilePath, string localFilePath);
}
