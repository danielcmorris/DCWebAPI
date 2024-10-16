namespace DCElectricWebAPI.Models;

public class Report
{
    public Guid ReportId { get; set; }  // Generated automatically
    public string CustomerName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CreatedBy { get; set; }
    public string Status { get; set; }
    public string ReportName { get; set; }
    public string BlobURL { get; set; }
    public DateTime CreatedAt { get; set; }  // Automatically generated
    public DateTime? ModifiedOn { get; set; }  // Automatically generated
}
