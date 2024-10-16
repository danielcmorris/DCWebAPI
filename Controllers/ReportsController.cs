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
    private readonly ILogger<ReportsController> _logger;
    public ReportsController(AzureBlobService blobService, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ReportsController> logger)
    {
        _blobService = blobService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;   
    }

    [HttpPost("create")]
    public async Task<IActionResult> PostAsync([FromHeader] string Authorization, [FromBody] CustomerReportRequest customerReportRequest)
    {
        var um = new UserModule(Authorization);
        if (!um.Secured) return Unauthorized();

        var user = GetUserBySessionID(Authorization);
        //ReportService reportService = new ReportService();
        //
        
        try
        {
            var existingReport = await FetchReportByCustomerAndDateAsync(customerReportRequest.Customers[0], customerReportRequest.StartDate, customerReportRequest.EndDate);
            if (existingReport != null)
            {
                return BadRequest(new
                {
                    message = "Report already exists",
                    report = existingReport  // Include the report object in the response
                });
            }
            //string strStart = customerReportRequest.StartDate.Year + customerReportRequest.StartDate.Month + customerReportRequest.EndDate.Day + "_";
            ///string strEnd = customerReportRequest.StartDate.Year + customerReportRequest.EndDate.Month + customerReportRequest.EndDate.Day + "";
            string strStart = $"{customerReportRequest.StartDate.Year}{customerReportRequest.StartDate.Month}{customerReportRequest.StartDate.Day}_";
            string strEnd = $"{customerReportRequest.EndDate.Year}{customerReportRequest.EndDate.Month}{customerReportRequest.EndDate.Day}";
            List<CustomerDetails> customerDetails = new List<CustomerDetails>();
            for (int i = 0; i < customerReportRequest.Customers.Count; i++)
            {
                var report = new Report
                {
                    CustomerName = customerReportRequest.Customers[i],
                    StartDate = customerReportRequest.StartDate,
                    EndDate = customerReportRequest.EndDate,
                    CreatedBy = user.UserId,  // Assuming UserModule provides UserId
                    Status = "In Progress",
                    CreatedAt = DateTime.UtcNow,
                    ReportName = $"{customerReportRequest.Customers[i]}_{strStart}_{strEnd}"
                };

                // Add the report to the database and get the generated ReportId
                var createdReport = await AddReportAsync(report);  // Add to DB
                string fileName = createdReport.ReportId.ToString();
                customerDetails.Add(new CustomerDetails() { CustomerName = customerReportRequest.Customers[i], FileName = fileName, ReportId = createdReport.ReportId });
            }
            // Start the report generation in the background 
            Task.Run(() => ProcessReportsAsync(customerDetails, customerReportRequest.StartDate, customerReportRequest.EndDate));
            return Ok(new { message = "Reports generation initiated" });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
    
    [HttpPost("retry/{reportId}")]
    public async Task<IActionResult> Retry([FromHeader] string Authorization, [FromRoute] Guid reportId)
    {
        try
        {
            var report = await GetReportByIdAsync(reportId);
            if (report == null)
            {
                return NotFound();
            }
            await UpdateReportAsync(report.ReportId, "In Progress");
            if (report.Status == "Failed")
            {
                await UpdateReportAsync(report.ReportId, "In Progress");
                List<CustomerDetails> customerDetails = new List<CustomerDetails>();
                customerDetails.Add(new CustomerDetails() { CustomerName = report.CustomerName, FileName = report.ReportId.ToString(), ReportId = report.ReportId });
                Task.Run(() => ProcessReportsAsync(customerDetails, report.StartDate, report.EndDate));
            }
            if (report.Status == "Completed")
            {
                string strNewReportFile = "C:\\DCEG\\Billing\\" + report.ReportId.ToString() + ".pdf";
                string strStart = $"{report.StartDate.Year}{report.StartDate.Month}{report.StartDate.Day}_";
                string strEnd = $"{report.EndDate.Year}{report.EndDate.Month}{report.EndDate.Day}";
                byte[] pdfBytes = System.IO.File.ReadAllBytes(strNewReportFile);
                string blobURL = await _blobService.UploadFileAsync(pdfBytes, $"{report.CustomerName}_{strStart}_{strEnd}.pdf");
                await UpdateReportAsync(report.ReportId, "Uploaded", blobURL);
            }
            return Ok(new { message = "Retrying" });
        }
        catch (Exception ex)
        {

            throw ex;
        }
        
    }
    //[HttpGet]
    //public async Task<IActionResult> GetAllFilesAsync()
    //{
    //    try
    //    {
    //        // Call the service to get the list of files
    //        List<BlobFileInfo> files = await _blobService.GetAllFilesAsync();
    //        return Ok(files);
    //    }
    //    catch (Exception ex)
    //    {
    //        return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    [HttpGet("all")]
    public async Task<IActionResult> GetAllReports()
    {
        try
        {
            string sql = $@"
                            SELECT ReportId, CustomerName, StartDate, EndDate, CreatedBy, Status, ReportName, BlobURL, CreatedAt, ModifiedOn
                            FROM Reports
                            ORDER BY CreatedAt DESC";

            var dl = new DataLayerBase();
            var x = await dl.QueryAsync<Report>(sql);

            return Ok(x);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadBlob(string blobName)
    {
        _logger.LogInformation("Actual Blob Name: {BlobName}", blobName);
        try
        {
            // Get the blob content as a byte array
            byte[] blobData = await _blobService.GetBlobAsync(blobName);

            if (blobData == null)
            {
                _logger.LogInformation("Blob not found");
                return NotFound();
            }

            // Return the file as a File result with appropriate headers
            return File(blobData, "application/pdf", blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob: {BlobName}", blobName);
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private async Task ProcessReportsAsync(List<CustomerDetails> customerDetails, DateTime startDate, DateTime endDate)
    { 

        var client = _httpClientFactory.CreateClient();
        var reportServiceUrl = _configuration["ReportServer"] + "/api/Report"; // .NET 4.8 Service URL
        // Send request to .NET 4.8 service

        ReportRequest customerReportRequest = new ReportRequest()
        {
            CustomerDetails = customerDetails,
            StartDate = startDate,
            EndDate = endDate
        };
        try
        {
            var response = await client.PostAsJsonAsync(reportServiceUrl, customerReportRequest);

            if (response.IsSuccessStatusCode)
            {

                // Get the report (e.g., PDF) from the .NET Framework service
                var customerReportResponseList = await response.Content.ReadFromJsonAsync<List<CustomerReportResponse>>();
                if (customerReportResponseList.Count > 0 )
                {
                    foreach (var report in customerReportResponseList)
                    {
                        await UpdateReportAsync(report.ReportId, "Completed");
                        string customerName = report.CustomerName;  // Get the customer name

                        // Upload the byte array (PDF) to Azure Blob Storage
      string strStart = $"{startDate.Year}{startDate.Month:00}{startDate.Day:00}_";
string strEnd = $"{endDate.Year}{endDate.Month:00}{endDate.Day:00}";
                        //byte[] pdfBytes = System.IO.File.ReadAllBytes(strNewReportFile);
                        //string blobURL = await _blobService.UploadFileAsync(pdfBytes, $"{report.CustomerName}_{strStart}_{strEnd}.pdf");
                        string blobURL = await _blobService.UploadFileAsync(report.File, $"{report.CustomerName}_{strStart}_{strEnd}.pdf");
                        await UpdateReportAsync(report.ReportId, "Uploaded", blobURL);
                    }
                } 
                else
                {
                    await UpdateFailedStatus(customerReportRequest);
                }
                

            } 
            else
            {
               await UpdateFailedStatus(customerReportRequest);
            }
        }
        catch (Exception ex )
        {
            await UpdateFailedStatus(customerReportRequest);
            throw ex;
        }
       
    }
    private async Task UpdateFailedStatus(ReportRequest customerReportRequest)
    {
        foreach (var report in customerReportRequest.CustomerDetails)
        {
            await UpdateReportAsync(report.ReportId, "Failed");
        }
    }
    private async Task<Report> AddReportAsync(Report report)
    {
        var sql = @"
            INSERT INTO Reports (CustomerName, StartDate, EndDate, CreatedBy, Status, ReportName, BlobURL, CreatedAt)
            OUTPUT INSERTED.ReportId  -- Capture the inserted ReportId
            VALUES (@CustomerName, @StartDate, @EndDate, @CreatedBy, @Status, @ReportName, @BlobURL, GETDATE());";

        using (var dl = new DataLayerBase())
        {
            // Capture the inserted ReportId
            var reportId = await dl.QuerySingleAsync<Guid>(sql, new
            {
                report.CustomerName,
                report.StartDate,
                report.EndDate,
                report.CreatedBy,
                report.Status,
                report.ReportName,
                report.BlobURL
            });

            // Set the generated ReportId on the object
            report.ReportId = reportId;
            report.CreatedAt = DateTime.UtcNow;  // Assuming it's generated during insert
            return report;
        }
    }
    // Update report status and Blob URL
    private async Task UpdateReportAsync(Guid reportId, string status, string blobUrl = null)
    {
        var sql = @"
            UPDATE Reports 
            SET Status = @Status,
                BlobURL = @BlobURL,
                ModifiedOn = GETDATE()   
            WHERE ReportId = @ReportId;";

        using (var dl = new DataLayerBase())
        {
            await dl.ExecuteAsync(sql, new
            {
                Status = status,
                BlobURL = blobUrl,
                ReportId = reportId
            });
        }
    }
    private async Task<Report> GetReportByIdAsync(Guid reportId)
    {
        var sql = @"
            SELECT * 
            FROM Reports 
            WHERE ReportId = @ReportId;";

        using (var dl = new DataLayerBase())
        {
            // Fetch the report with the given ReportId
            var report = await dl.QuerySingleOrDefaultAsync<Report>(sql, new { ReportId = reportId });
            return report;
        }
    }
    public async Task<Report> FetchReportByCustomerAndDateAsync(string customerName, DateTime startDate, DateTime endDate)
    {
        var sql = @"
        SELECT TOP 1 ReportId, CustomerName, StartDate, EndDate, CreatedBy, Status, ReportName, BlobURL, CreatedAt
        FROM Reports
        WHERE CustomerName = @CustomerName
        AND CAST(StartDate AS DATE) = CAST(@StartDate AS DATE)
        AND CAST(EndDate AS DATE) = CAST(@EndDate AS DATE);";  // Compare only the date portion

        using (var dl = new DataLayerBase())
        {
            var report = await dl.QuerySingleOrDefaultAsync<Report>(sql, new
            {
                CustomerName = customerName,
                StartDate = startDate.Date,  // Pass only the date part of the provided StartDate
                EndDate = endDate.Date       // Pass only the date part of the provided EndDate
            });

            return report;
        }
    }

    private User GetUserBySessionID(string sid)
    {
        string sql = $"SELECT * FROM  [dbo].[fnSecurity_UserBySessionId]('{sid}');";
        using (var dl = new DataLayerBase())
        {
            var userSet = dl.Query<User>(sql);
            return userSet.First();
        };
    }
}
