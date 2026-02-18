namespace DCElectricWebAPI.Models;

public class GoogleCloudStorageSettings
{
    public string BucketName { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string SecretManagerProjectId { get; set; } = string.Empty;
    public string CredentialSecretName { get; set; } = string.Empty;
    public string LocalCredentialPath { get; set; } = string.Empty;
    public bool EnableDualWrite { get; set; } = true;
}
