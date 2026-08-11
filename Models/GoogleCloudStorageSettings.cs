namespace DCElectricWebAPI.Models;

public class GoogleCloudStorageSettings
{
    public string BucketName { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string SecretManagerProjectId { get; set; } = string.Empty;
    public string CredentialSecretName { get; set; } = string.Empty;
    public string LocalCredentialPath { get; set; } = string.Empty;
    // Hard cap on upload size — files pushed to Google services must have a strict, configured size limit
    public long MaxUploadSizeBytes { get; set; } = 52428800; // 50 MB
}
