using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug;

/// <summary>
/// Access to Azure Storage.
/// </summary>
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
public class AzureStorage(ILogger<AzureStorage> logger) : IAzureStorage
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
{
    private const string StorageContainerName = "processed-topics";

    /// <inheritdoc />
    public async Task<string> UploadFileAsync(string storageFilePath, string localFilePath)
    {
        logger.LogInformation($"Lade Datei {localFilePath} in den Azure Storage hoch...");
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AzureWebJobsStorage is not set");
        }

        var containerClient = new BlobServiceClient(connectionString).GetBlobContainerClient(StorageContainerName);
        var blobClient = containerClient.GetBlobClient(storageFilePath);

        using var localFileStream = File.OpenRead(localFilePath);
        await blobClient.UploadAsync(localFileStream).ConfigureAwait(false);

        logger.LogInformation($"Erstelle DownloadUrl für Datei {localFilePath}...");
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = StorageContainerName,
            BlobName = storageFilePath,
            Resource = "b",
            StartsOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.AddMonths(1),
            Protocol = SasProtocol.Https,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var accountName = Helper.ExtractSettingByKey(connectionString, "AccountName");
        var accountKey = Helper.ExtractSettingByKey(connectionString, "AccountKey");
        string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(accountName, accountKey)).ToString();
        return $"{blobClient.Uri}?{sasToken}";
    }
}
