namespace DCElectricWebAPI.Models;

public class BlobFileInfo
{
    public string Name { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string SasUrl { get; set; }
}
