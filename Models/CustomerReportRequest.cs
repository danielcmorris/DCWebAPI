namespace DCElectricWebAPI.Models;

public class CustomerReportRequest
{
    public List<string> Customers { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
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
}

public class CustomerReportResponse
{
    public Guid ReportId { get; set; }
    public string FileName { get; set; }
    public string CustomerName { get; set; }
    public byte[] File { get; set; }
}
