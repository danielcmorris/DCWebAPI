using DCElectricWebAPI.Models;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace DCElectricWebAPI.Modules;

public class GoogleCloudStorageService
{
    private readonly GoogleCloudStorageSettings _settings;
    private readonly ILogger<GoogleCloudStorageService> _logger;
    private readonly StorageClient? _storageClient;
    private readonly bool _isInitialized;

    public GoogleCloudStorageService(
        IOptions<GoogleCloudStorageSettings> settings,
        ILogger<GoogleCloudStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        try
        {
            _storageClient = InitializeStorageClient();
            _isInitialized = _storageClient != null;

            if (_isInitialized)
            {
                _logger.LogInformation("GoogleCloudStorageService initialized successfully for bucket: {Bucket}", _settings.BucketName);
            }
            else
            {
                _logger.LogWarning("GoogleCloudStorageService not initialized - GCS uploads will be skipped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize GoogleCloudStorageService");
            _isInitialized = false;
        }
    }

    public bool IsEnabled => _isInitialized && _settings.EnableDualWrite;

    private StorageClient? InitializeStorageClient()
    {
        GoogleCredential? credential = null;

        // Try loading from local file first (development)
        if (!string.IsNullOrEmpty(_settings.LocalCredentialPath) && File.Exists(_settings.LocalCredentialPath))
        {
            _logger.LogInformation("Loading GCS credentials from local file: {Path}", _settings.LocalCredentialPath);
            credential = GoogleCredential.FromFile(_settings.LocalCredentialPath);
        }
        // Try Application Default Credentials (works in Cloud Run with service account)
        else
        {
            try
            {
                _logger.LogInformation("Attempting to load GCS credentials from Application Default Credentials");
                credential = GoogleCredential.GetApplicationDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load Application Default Credentials for GCS");
                return null;
            }
        }

        if (credential == null)
        {
            _logger.LogWarning("No GCS credentials found");
            return null;
        }

        return StorageClient.Create(credential);
    }

    public async Task<string?> UploadFileAsync(byte[] fileBytes, string objectPath)
    {
        if (!IsEnabled || _storageClient == null)
        {
            _logger.LogDebug("GCS upload skipped - service not enabled or initialized");
            return null;
        }

        try
        {
            using var stream = new MemoryStream(fileBytes);

            var obj = await _storageClient.UploadObjectAsync(
                _settings.BucketName,
                objectPath,
                "application/pdf",
                stream);

            var gcsUrl = $"gs://{_settings.BucketName}/{objectPath}";
            _logger.LogInformation("GCS upload successful: {GcsUrl}", gcsUrl);

            return gcsUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to GCS: {ObjectPath}", objectPath);
            throw;
        }
    }

    public string GenerateSignedUrl(string objectPath, TimeSpan? expiration = null)
    {
        if (_storageClient == null)
        {
            throw new InvalidOperationException("GCS client not initialized");
        }

        var urlSigner = UrlSigner.FromCredential(GoogleCredential.GetApplicationDefault());
        var duration = expiration ?? TimeSpan.FromHours(1);

        return urlSigner.Sign(
            _settings.BucketName,
            objectPath,
            duration,
            HttpMethod.Get);
    }

    public async Task<bool> ObjectExistsAsync(string objectPath)
    {
        if (_storageClient == null)
        {
            return false;
        }

        try
        {
            await _storageClient.GetObjectAsync(_settings.BucketName, objectPath);
            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
