namespace DCElectricWebAPI.Models;

public class CustomerReportRequest
{
    public List<string> Customers { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
