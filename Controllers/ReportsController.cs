using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;


namespace DCElectricWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AzureBlobService _blobService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    public ReportsController(AzureBlobService blobService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _blobService = blobService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] CustomerReportRequest request)
    {
        //ReportService reportService = new ReportService();  
        try
        {
            var client = _httpClientFactory.CreateClient();
            var reportServiceUrl = _configuration["ReportServer"] +"/api/Report"; // .NET 4.8 Service URL

            // Send request to .NET 4.8 service
            var response = await client.PostAsJsonAsync(reportServiceUrl, request);

            if (response.IsSuccessStatusCode)
            {
                // Get the report (e.g., PDF) from the .NET Framework service
                var reportList = await response.Content.ReadFromJsonAsync<List<byte[]>>();

                // Return the PDF report to the client


                // Get a list of byte[] for each customer report
                //List<byte[]> customerReportBytes = reportService.GenerateReports(
                //    request.Customers.Count, request.Customers, request.StartDate, request.EndDate);

            if (reportList == null || !reportList.Any())
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to generate report.");
            }

            for (int i = 0; i < reportList.Count; i++)
            {
                // Generate a unique blob name using customer name and current timestamp
                string customerName = request.Customers[i];  // Get the customer name
                string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");  // Create a timestamp
                string blobName = $"{customerName}_{timestamp}.pdf";

                // Upload the byte array (PDF) to Azure Blob Storage
                await _blobService.UploadFileAsync(reportList[i], blobName);
            }

                return Ok(new { message = "Reports Generated" });

            }
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to generate report.");
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllFilesAsync()
    {
        try
        {
            // Call the service to get the list of files
            List<BlobFileInfo> files = await _blobService.GetAllFilesAsync();
            return Ok(files);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet]
    [Route("download")]
    public async Task<IActionResult> DownloadBlob(string blobName)
    {
        try
        {
            // Get the blob content as a byte array
            byte[] blobData = await _blobService.GetBlobAsync(blobName);

            if (blobData == null)
            {
                return NotFound();
            }

            // Return the file as a File result with appropriate headers
            return File(blobData, "application/pdf", blobName);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
