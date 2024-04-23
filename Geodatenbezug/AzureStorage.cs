using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Geodatenbezug;

/// <summary>
/// Access to Azure Storage.
/// </summary>
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
public class AzureStorage() : IAzureStorage
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly
{
    private const string StorageContainerName = "processed-topics";

    private string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AzureWebJobsStorage is not set");
        }

        return connectionString;
    }

    private StorageSharedKeyCredential GetStorageSharedKeyCredential()
    {
        var connectionString = GetConnectionString().Split(";");
        var accountName = connectionString.Where(item => item.StartsWith("AccountName", StringComparison.InvariantCulture)).FirstOrDefault().Replace("AccountName=", string.Empty, StringComparison.CurrentCulture);
        var accountKey = connectionString.Where(item => item.StartsWith("AccountKey", StringComparison.InvariantCulture)).FirstOrDefault().Replace("AccountKey=", string.Empty, StringComparison.CurrentCulture);
        return new StorageSharedKeyCredential(accountName, accountKey);
    }

    /// <inheritdoc />
    public async Task<string> UploadFileAsync(string storageFilePath, string localFilePath)
    {
        var containerClient = new BlobServiceClient(GetConnectionString()).GetBlobContainerClient(StorageContainerName);
        var blobClient = containerClient.GetBlobClient(storageFilePath);

        using var localFileStream = File.OpenRead(localFilePath);
        await blobClient.UploadAsync(localFileStream).ConfigureAwait(false);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = StorageContainerName,
            BlobName = storageFilePath,
            Resource = "b", // b for blob
            StartsOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.AddHours(24), // TODO: How long should the link be valid?
            Protocol = SasProtocol.Https,
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        string sasToken = sasBuilder.ToSasQueryParameters(GetStorageSharedKeyCredential()).ToString();
        return blobClient.Uri + "?" + sasToken;
    }
}
