using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DCElectricWebAPI.Models;

namespace DCElectricWebAPI.Modules;

public class AzureBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobService> _logger;

    /// <summary>
    /// Initializes a new instance of the AzureBlobService class.
    /// Reads the connection string and container name from the web.config.
    /// </summary>
    public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
    {
        _configuration = configuration;
        // Get the Azure Blob Storage connection string from web.config
        string connectionString = _configuration["AzureBlobStorageConnectionString"];

        // Get the Azure Blob Storage container name from web.config
        _containerName = _configuration["AzureBlobStorageContainerName"];

        // Create a BlobServiceClient object using the connection string
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;   
    }

    /// <summary>
    /// Uploads a byte array (PDF) to a specific Azure Blob container.
    /// </summary>
    /// <param name="containerName">Name of the Azure Blob container.</param>
    /// <param name="fileBytes">The byte array containing the file data.</param>
    /// <param name="folderName">The optional folder name to prepend to the blob name.</param>
    /// <param name="blobName">The name of the blob (file) in Azure storage.</param>

    public async Task<string> UploadFileAsync(byte[] fileBytes, string blobName, string folderName = null, string containerName = null)
    {
        if (string.IsNullOrEmpty(containerName))
        {
            containerName = _containerName;  // Default container name if none is provided
        }

        // Get a reference to the container
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        // Ensure the container exists
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        // If a folder name is provided, prepend it to the blob name
        if (!string.IsNullOrEmpty(folderName))
        {
            blobName = $"{folderName.TrimEnd('/')}/{blobName}";
        }
        else if (!string.IsNullOrEmpty(_configuration["AzureBlobStorageFolderName"])) // If a folder is provided in configuration
        {
            blobName = $"{_configuration["AzureBlobStorageFolderName"].TrimEnd('/')}/{blobName}";
        }

        // Get a reference to the blob you want to upload
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Upload the byte array to Azure Blob Storage
        using (var memoryStream = new MemoryStream(fileBytes))
        {
            await blobClient.UploadAsync(memoryStream, overwrite: true);
        }

        // Return the URI (Blob URL)
        return blobClient.Uri.ToString();
    }


    /// <summary>
    /// Gets all files (blobs) from a specific Azure Blob container or folder with their names, last modified date, and SAS URL.
    /// </summary>
    /// <param name="containerName">The name of the Azure Blob container.</param>
    /// <param name="folderPath">Optional folder path to filter the blobs. If not provided, gets all blobs in the container.</param>
    /// <returns>A list of blob information, including names, last modified dates, and SAS URLs.</returns>
    public async Task<List<BlobFileInfo>> GetAllFilesAsync(string containerName = null, string folderPath = null)
    {
        // Use the container name from the configuration if not provided
        if (string.IsNullOrEmpty(containerName))
        {
            containerName = _containerName;
        }
        if (string.IsNullOrEmpty(folderPath))
        {
            folderPath = _configuration["AzureBlobStorageFolderName"];
        }

        // Get a reference to the container
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        // List to hold blob info (name, last modified date, and SAS URL)
        List<BlobFileInfo> blobFileInfos = new List<BlobFileInfo>();

        // Optional folder path filter
        string prefix = string.IsNullOrEmpty(folderPath) ? null : $"{folderPath.TrimEnd('/')}/";

        // Iterate through the blobs in the container or folder
        foreach (BlobItem blobItem in containerClient.GetBlobs(prefix: prefix))
        {
            // Get the BlobClient for each blob
            BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);

            // Generate a SAS URL for each blob
            string sasUrl = GenerateSasUrl(blobClient);

            // Add blob information to the list
            blobFileInfos.Add(new BlobFileInfo
            {
                Name = blobItem.Name,
                LastModified = blobItem.Properties.LastModified,
                SasUrl = sasUrl
            });
        }

        return blobFileInfos;
    }
    /// <summary>
    /// Generates a SAS URL for the specified blob client.
    /// </summary>
    /// <param name="blobClient">The BlobClient to generate a SAS URL for.</param>
    /// <returns>The SAS URL that provides access to the blob.</returns>
    private string GenerateSasUrl(BlobClient blobClient)
    {
        // Set the expiry time and permissions for the SAS token
        BlobSasBuilder sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // SAS token valid for 1 hour
        };

        // Set the permissions to allow reading the blob
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // Generate the SAS URL using the storage account's key
        Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

        return sasUri.ToString();
    }
    public async Task<byte[]> GetBlobAsync(string blobName)
    {
        // Get a reference to the container
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Get a reference to the blob
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Check if the blob exists
        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        // Download the blob's content
        BlobDownloadInfo download = await blobClient.DownloadAsync();

        // Copy the blob content into a memory stream
        using (MemoryStream ms = new MemoryStream())
        {
            await download.Content.CopyToAsync(ms);
            return ms.ToArray(); // Return the blob content as a byte array
        }
    }

    /// <summary>
    /// Generates a SAS URL for a blob by name, providing time-limited read access.
    /// </summary>
    /// <param name="blobName">The name/path of the blob.</param>
    /// <param name="inline">If true, sets Content-Disposition to inline for browser preview.</param>
    /// <returns>A SAS URL that provides read access to the blob.</returns>
    public string GetSasUrlForBlob(string blobName, bool inline = false)
    {
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        if (inline)
        {
            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
                ContentDisposition = "inline",
                ContentType = "application/pdf"
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        return GenerateSasUrl(blobClient);
    }

    public async Task<bool> DeleteBlobAsync(string blobName)
    {
        // Get a reference to the container
        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

        // Get a reference to the blob
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        // Delete the blob if it exists
        bool deleted = await blobClient.DeleteIfExistsAsync();

        // Return whether the blob was successfully deleted
        return deleted;
    }

    /// <summary>
    /// Uploads a byte array to Azure Blob Storage and returns a SAS URL for accessing the file.
    /// </summary>
    /// <param name="fileBytes">The byte array containing the file data.</param>
    /// <param name="blobName">The name of the blob (file) in Azure storage.</param>
    /// <param name="folderName">The optional folder name to prepend to the blob name.</param>
    /// <param name="containerName">The optional container name. Uses default if not provided.</param>
    /// <returns>A SAS URL that provides read access to the uploaded blob.</returns>
    public async Task<string> UploadFileAndGetSasUrlAsync(byte[] fileBytes, string blobName, string folderName = null, string containerName = null)
    {
        if (string.IsNullOrEmpty(containerName))
        {
            containerName = _containerName;
        }

        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        if (!string.IsNullOrEmpty(folderName))
        {
            blobName = $"{folderName.TrimEnd('/')}/{blobName}";
        }
        else if (!string.IsNullOrEmpty(_configuration["AzureBlobStorageFolderName"]))
        {
            blobName = $"{_configuration["AzureBlobStorageFolderName"].TrimEnd('/')}/{blobName}";
        }

        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        using (var memoryStream = new MemoryStream(fileBytes))
        {
            await blobClient.UploadAsync(memoryStream, overwrite: true);
        }

        return GenerateSasUrl(blobClient);
    }

}
