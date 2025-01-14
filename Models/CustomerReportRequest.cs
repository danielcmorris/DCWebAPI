namespace DCElectricWebAPI.Models;

public class CustomerReportRequest
{
    public List<string> Customers { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? StrButton { get; set; }
    public string ReportType { get; set; } //
}
public class CustomerDetails
{
    public Guid ReportId { get; set; }
    public string FileName { get; set; }
    public string CustomerName { get; set; }
}

public class ReportRequest
{
    public List<CustomerDetails> CustomerDetails { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string StrButton { get; set; } = string.Empty;
}

public class CustomerReportResponse
{
    public Guid ReportId { get; set; }
    public string FileName { get; set; }
    public string CustomerName { get; set; }

    public string ErrorMessage { get; set; }
    public byte[] File { get; set; }
}
