namespace DCElectricWebAPI.Modules;

public class DualStorageResult
{
    public string AzureUrl { get; set; } = string.Empty;
    public string? GcsUrl { get; set; }
    public bool GcsUploadSucceeded { get; set; }
    public string? GcsError { get; set; }
}

public class DualStorageService
{
    private readonly AzureBlobService _azureBlobService;
    private readonly GoogleCloudStorageService _gcsService;
    private readonly ILogger<DualStorageService> _logger;

    public DualStorageService(
        AzureBlobService azureBlobService,
        GoogleCloudStorageService gcsService,
        ILogger<DualStorageService> logger)
    {
        _azureBlobService = azureBlobService;
        _gcsService = gcsService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a PDF to both Azure (primary) and GCS (secondary).
    /// Azure failure throws an exception. GCS failure is logged but doesn't fail the request.
    /// </summary>
    /// <param name="fileBytes">The PDF file bytes</param>
    /// <param name="fileName">The filename for the PDF</param>
    /// <param name="azureFolderName">The Azure blob folder (e.g., "streetlights")</param>
    /// <param name="customerName">Customer name for GCS path</param>
    /// <param name="invoiceType">Invoice type for GCS path (e.g., "streetlights", "trafficlights")</param>
    /// <param name="startDate">Report start date for GCS path (yymm folder)</param>
    /// <returns>Result containing both URLs</returns>
    public async Task<DualStorageResult> UploadAsync(
        byte[] fileBytes,
        string fileName,
        string azureFolderName,
        string customerName,
        string invoiceType,
        DateTime startDate)
    {
        var result = new DualStorageResult();

        // Primary: Azure Blob Storage (must succeed)
        _logger.LogInformation("Uploading to Azure Blob Storage: {FileName} to folder {Folder}", fileName, azureFolderName);
        result.AzureUrl = await _azureBlobService.UploadFileAsync(fileBytes, fileName, azureFolderName);
        _logger.LogInformation("Azure upload successful: {AzureUrl}", result.AzureUrl);

        // Secondary: Google Cloud Storage (best effort)
        if (_gcsService.IsEnabled)
        {
            try
            {
                var gcsPath = InvoicePathBuilder.BuildInvoicePath(customerName, invoiceType, startDate, fileName);
                _logger.LogInformation("Uploading to GCS: {GcsPath}", gcsPath);

                result.GcsUrl = await _gcsService.UploadFileAsync(fileBytes, gcsPath);
                result.GcsUploadSucceeded = !string.IsNullOrEmpty(result.GcsUrl);

                if (result.GcsUploadSucceeded)
                {
                    _logger.LogInformation("GCS upload successful: {GcsUrl}", result.GcsUrl);
                }
            }
            catch (Exception ex)
            {
                result.GcsUploadSucceeded = false;
                result.GcsError = ex.Message;
                _logger.LogError(ex, "GCS upload failed for {FileName}. Azure upload succeeded, continuing.", fileName);
            }
        }
        else
        {
            _logger.LogDebug("GCS dual-write disabled, skipping GCS upload for {FileName}", fileName);
        }

        return result;
    }

    /// <summary>
    /// Uploads a PDF to Azure only (for backwards compatibility during transition).
    /// </summary>
    public async Task<string> UploadToAzureOnlyAsync(byte[] fileBytes, string fileName, string folderName)
    {
        return await _azureBlobService.UploadFileAsync(fileBytes, fileName, folderName);
    }
}
