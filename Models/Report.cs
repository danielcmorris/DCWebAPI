namespace DCElectricWebAPI.Models;

public class Report
{
    public Guid ReportID { get; set; }  // Generated automatically

    public string ReportType { get; set; } // Streetlight or Traffic
    public string StrButton { get; set; }
    public string CustomerName { get; set; }
    public DateTime StartDate { get; set; }
    public string Message { get; set; }

    public DateTime EndDate { get; set; }
    public int CreatedByID { get; set; }
    public int UpdatedByID { get; set; }
    public bool IsDeleted { get; set; }
    public string GenerationStatus { get; set; }
    public string ReportName { get; set; }
    public string BlobURL { get; set; }
    public string? GcsURL { get; set; }
    public int TicketCount { get; set; }  // Number of items in the report (fixtures or tickets)
    public DateTime CreatedDate { get; set; }  // Automatically generated
    public DateTime? UpdatedDate { get; set; }  // Automatically generated
}
