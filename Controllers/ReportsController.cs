using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Net;


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

        var user = GetUserBySessionID(Authorization.Substring("Bearer ".Length).Trim());
        //ReportService reportService = new ReportService();
        //
        
        try
        {
            var existingReport = await FetchReportByCustomerAndDateAsync(customerReportRequest.Customers[0], customerReportRequest.StartDate, customerReportRequest.EndDate);
          
            //string strStart = customerReportRequest.StartDate.Year + customerReportRequest.StartDate.Month + customerReportRequest.EndDate.Day + "_";
            ///string strEnd = customerReportRequest.StartDate.Year + customerReportRequest.EndDate.Month + customerReportRequest.EndDate.Day + "";
            string strStart = $"{customerReportRequest.StartDate.Year}{customerReportRequest.StartDate.Month:00}{customerReportRequest.StartDate.Day:00}_";
            string strEnd = $"{customerReportRequest.EndDate.Year}{customerReportRequest.EndDate.Month:00}{customerReportRequest.EndDate.Day:00}";
            List<CustomerDetails> customerDetails = new List<CustomerDetails>();
            for (int i = 0; i < customerReportRequest.Customers.Count; i++)
            {
                var report = new Report
                {
                    CustomerName = customerReportRequest.Customers[i],
                    StartDate = customerReportRequest.StartDate,
                    EndDate = customerReportRequest.EndDate,
                    CreatedByID = user.UserId,  // Assuming UserModule provides UserId
                    GenerationStatus = "In Progress",
                    CreatedDate = DateTime.UtcNow,
                    ReportName = $"{customerReportRequest.Customers[i]}_{strStart}_{strEnd}"
                };
                if (existingReport != null)
                {
                    await OverrideReportAsync(report);  // Add to DB
                    string fileName = existingReport.ReportID.ToString();
                    customerDetails.Add(new CustomerDetails() { CustomerName = customerReportRequest.Customers[i], FileName = fileName, ReportId = existingReport.ReportID });
                }
                else
                {
                    var createdReport = await AddReportAsync(report);  // Add to DB
                    string fileName = createdReport.ReportID.ToString();
                    customerDetails.Add(new CustomerDetails() { CustomerName = customerReportRequest.Customers[i], FileName = fileName, ReportId = createdReport.ReportID });
                }
                // Add the report to the database and get the generated ReportId
               
            }
            // Start the report generation in the background 
            Task.Run(() => ProcessReportsAsync(customerDetails, customerReportRequest.StartDate, customerReportRequest.EndDate, user.UserId));
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
        var um = new UserModule(Authorization);
        if (!um.Secured) return Unauthorized();

        var user = GetUserBySessionID(Authorization.Substring("Bearer ".Length).Trim());
        try
        {
            var report = await GetReportByIdAsync(reportId);
            if (report == null)
            {
                return NotFound();
            }
            await UpdateReportAsync(report.ReportID, "In Progress", user.UserId);
            if (report.GenerationStatus == "Failed")
            {
                await UpdateReportAsync(report.ReportID, "In Progress", user.UserId);
                List<CustomerDetails> customerDetails = new List<CustomerDetails>();
                customerDetails.Add(new CustomerDetails() { CustomerName = report.CustomerName, FileName = report.ReportID.ToString(), ReportId = report.ReportID });
                Task.Run(() => ProcessReportsAsync(customerDetails, report.StartDate, report.EndDate, user.UserId));
            }
            if (report.GenerationStatus == "Completed")
            {
               
                string strStart = $"{report.StartDate.Year}{report.StartDate.Month:00}{report.StartDate.Day:00}_";
                string strEnd = $"{report.EndDate.Year}{report.EndDate.Month:00}{report.EndDate.Day:00}";
                string strNewReportFile = $"C:\\DCEG\\Billing\\{report.CustomerName}_{strStart}_{strEnd}.pdf";
                byte[] pdfBytes = System.IO.File.ReadAllBytes(strNewReportFile);
                string blobURL = await _blobService.UploadFileAsync(pdfBytes, $"{report.CustomerName}_{strStart}_{strEnd}.pdf");
                await UpdateReportAsync(report.ReportID, "Uploaded", user.UserId, blobURL);

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
    public async Task<IActionResult> GetAllReports([FromHeader] string Authorization)
    {
        var um = new UserModule(Authorization);
        if (!um.Secured) return Unauthorized();
        try
        {
            string sql = $@"
                            SELECT ReportId, CustomerName, StartDate, EndDate, CreatedByID, UpdatedByID, GenerationStatus, ReportName, BlobURL, CreatedDate, UpdatedDate
                            FROM Report
                            WHERE IsDeleted = 0
                            ORDER BY CreatedDate DESC";

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
    public async Task<IActionResult> DownloadBlob([FromHeader] string Authorization, string blobName)
    {
        var um = new UserModule(Authorization);
        if (!um.Secured) return Unauthorized();

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
    [HttpPost("download-zip")]
    public async Task<IActionResult> DownloadReportsAsZip([FromHeader] string Authorization, [FromBody] List<Guid> reportIds)
    {
        var um = new UserModule(Authorization);
        if (!um.Secured) return Unauthorized();

        try
        {
            // Create a temporary memory stream to hold the zip file
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Loop through the reportIds to fetch blob URLs and download each blob
                    foreach (var reportId in reportIds)
                    {
                        // Fetch the report from the database (fetch Blob URL)
                        var report = await GetReportByIdAsync(reportId);

                        if (report != null && report.GenerationStatus == "Uploaded" && !string.IsNullOrEmpty(report.BlobURL))
                        {
                            _logger.LogInformation("Downloading the blobname: {BlobName}", $"traffic/{report.ReportName}.pdf");
                            // Download the blob from Azure Storage
                            byte[] blobData = await _blobService.GetBlobAsync($"traffic/{report.ReportName}.pdf");

                            // Add each blob as an entry in the zip archive
                            var zipEntry = archive.CreateEntry($"{report.ReportName}.pdf", CompressionLevel.Fastest);
                            using (var zipStream = zipEntry.Open())
                            {
                                await zipStream.WriteAsync(blobData, 0, blobData.Length);
                            }
                        }
                    }
                }

                // Set the position back to the beginning
                memoryStream.Position = 0;

                // Return the zip file
                return File(memoryStream.ToArray(), "application/zip", "Reports.zip");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download zip: {BlobName}", reportIds.ToString());
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    [HttpDelete("{reportId}")]
    public async Task<IActionResult> DeleteReportAsync([FromHeader] string Authorization, [FromRoute] Guid reportId)
    {
        var um = new UserModule(Authorization);
        if (!um.Secured) return Unauthorized();

        var user = GetUserBySessionID(Authorization.Substring("Bearer ".Length).Trim());
        try
        {
            var report = await GetReportByIdAsync(reportId);
            if (report == null)
            {
                return NotFound(new { message = "Report not found" });
            }

            // Check if the report has already been deleted
            if (report.GenerationStatus == "Deleted" || report.IsDeleted)
            {
                return BadRequest(new { message = "Report is already deleted" });
            }

            // Step 1: Delete the blob from Azure Blob Storage if the blob URL exists
            if (!string.IsNullOrEmpty(report.BlobURL))
            {
                bool isBlobDeleted = await _blobService.DeleteBlobAsync($"traffic/{report.ReportName}.pdf");
                if (!isBlobDeleted)
                {
                    return Problem("Failed to delete the blob from Azure Storage", statusCode: StatusCodes.Status500InternalServerError);
                }
            }

            // Step 2: Update the report status in the database to "Deleted" and set IsDeleted flag
            await UpdateReportAsync(report.ReportID, "Deleted", user.UserId, isDeleted: true);

            return Ok(new { message = "Report deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting report with ID: {ReportId}", reportId);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    private async Task ProcessReportsAsync(List<CustomerDetails> customerDetails, DateTime startDate, DateTime endDate, int userId)
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
                        if (report.File !=null)
                        {
                        await UpdateReportAsync(report.ReportId, "Completed", userId);
                        string customerName = report.CustomerName;  // Get the customer name

                        // Upload the byte array (PDF) to Azure Blob Storage
                          string strStart = $"{startDate.Year}{startDate.Month:00}{startDate.Day:00}_";
                          string strEnd = $"{endDate.Year}{endDate.Month:00}{endDate.Day:00}";
                        //byte[] pdfBytes = System.IO.File.ReadAllBytes(strNewReportFile);
                        //string blobURL = await _blobService.UploadFileAsync(pdfBytes, $"{report.CustomerName}_{strStart}_{strEnd}.pdf");
                        string blobURL = await _blobService.UploadFileAsync(report.File, $"{report.CustomerName}_{strStart}_{strEnd}.pdf");
                        await UpdateReportAsync(report.ReportId, "Uploaded", userId, blobURL);
                            string strNewReportFile = $"C:\\DCEG\\Billing\\{report.CustomerName}_{strStart}_{strEnd}.pdf";
                            System.IO.File.WriteAllBytes(strNewReportFile, report.File);
                        }
                        else
                        {
                            await UpdateFailedStatus(customerReportRequest, userId);
                        }
                    }
                } 
                else
                {
                    await UpdateFailedStatus(customerReportRequest, userId);
                }
                

            } 
            else
            {
               await UpdateFailedStatus(customerReportRequest, userId);
            }
        }
        catch (Exception ex )
        {
            await UpdateFailedStatus(customerReportRequest, userId);
            throw ex;
        }
       
    }
    private async Task UpdateFailedStatus(ReportRequest customerReportRequest, int userId)
    {
        foreach (var report in customerReportRequest.CustomerDetails)
        {
            await UpdateReportAsync(report.ReportId, "Failed", userId);
        }
    }
    private async Task<Report> AddReportAsync(Report report)
    {
        var sql = @"
            INSERT INTO Report (CustomerName, StartDate, EndDate, CreatedByID, GenerationStatus, ReportName, BlobURL, CreatedDate)
            OUTPUT INSERTED.ReportId  -- Capture the inserted ReportId
            VALUES (@CustomerName, @StartDate, @EndDate, @CreatedByID, @GenerationStatus, @ReportName, @BlobURL, GETDATE());";

        using (var dl = new DataLayerBase())
        {
            // Capture the inserted ReportId
            var reportId = await dl.QuerySingleAsync<Guid>(sql, new
            {
                report.CustomerName,
                report.StartDate,
                report.EndDate,
                report.CreatedByID,
                report.GenerationStatus,
                report.ReportName,
                report.BlobURL
            });

            // Set the generated ReportId on the object
            report.ReportID = reportId;
            report.CreatedDate = DateTime.UtcNow;  // Assuming it's generated during insert
            return report;
        }
    }
    private async Task OverrideReportAsync(Report report)
    {
        var sql = @"
        UPDATE Report 
        SET GenerationStatus = @GenerationStatus,
            BlobURL = @BlobURL,
            CreatedDate = GETDATE(),
            CreatedByID = @CreatedByID,
            UpdatedDate =null,
            UpdatedByID = null
        WHERE ReportID = @ReportID;";

        using (var dl = new DataLayerBase())
        {
            await dl.ExecuteAsync(sql, new
            {
                GenerationStatus = report.GenerationStatus,
                BlobURL = report.BlobURL,
                CreatedByID = report.CreatedByID  
            });
        }
    }
    private async Task UpdateReportAsync(Guid reportId, string status, int updateById, string blobUrl = null, bool isDeleted = false)
    {
        var sql = @"
        UPDATE Report 
        SET GenerationStatus = @GenerationStatus,
            BlobURL = @BlobURL,
            IsDeleted = @IsDeleted,
            UpdatedDate = GETDATE(),
            UpdatedByID = @UpdatedByID
        WHERE ReportID = @ReportID;";

        using (var dl = new DataLayerBase())
        {
            await dl.ExecuteAsync(sql, new
            {
                GenerationStatus = status,
                BlobURL = blobUrl,
                IsDeleted = isDeleted,
                ReportID = reportId,
                UpdatedByID = updateById  // Assuming UserModule provides UserId
            });
        }
    }
    private async Task<Report> GetReportByIdAsync(Guid reportId)
    {
        var sql = @"
            SELECT * 
            FROM Report 
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
        SELECT TOP 1 ReportId, CustomerName, StartDate, EndDate, CreatedByID, UpdatedByID, GenerationStatus, ReportName, BlobURL, CreatedDate, UpdatedDate
        FROM Report
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
