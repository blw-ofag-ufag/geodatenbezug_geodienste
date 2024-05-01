using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Geodatenbezug.Models;
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
    private string connectionString = string.Empty;

    /// <summary>
    /// Connection string to the Azure Storage.
    /// </summary>
    protected string ConnectionString
    {
        get
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                var value = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                if (string.IsNullOrEmpty(value))
                {
                    throw new InvalidOperationException("AzureWebJobsStorage is not set");
                }

                connectionString = value;
            }

            return connectionString;
        }
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLastProcessed(Topic topic)
    {
        var containerClient = new BlobServiceClient(ConnectionString).GetBlobContainerClient(StorageContainerName);

        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
        {
            if (blobItem.Name.Contains(topic.Canton.ToString(), StringComparison.InvariantCulture)
                && blobItem.Name.Contains(topic.BaseTopic.ToString(), StringComparison.InvariantCulture))
            {
                return blobItem.Properties.CreatedOn.GetValueOrDefault().DateTime;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<string> UploadFileAsync(string storageFilePath, string localFilePath)
    {
        logger.LogInformation($"Lade Datei {localFilePath} in den Azure Storage hoch...");

        var containerClient = new BlobServiceClient(ConnectionString).GetBlobContainerClient(StorageContainerName);
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

        var accountName = ConnectionString.ExtractValueByKey("AccountName");
        var accountKey = ConnectionString.ExtractValueByKey("AccountKey");
        string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(accountName, accountKey)).ToString();
        return $"{blobClient.Uri}?{sasToken}";
    }
}
