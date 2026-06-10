using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO.Compression;
 

namespace DCElectricWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AzureBlobService _blobService;
    private readonly GoogleCloudStorageService _gcsService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportsController> _logger;
    private readonly DataLayerBase _dl;
    public ReportsController(AzureBlobService blobService, GoogleCloudStorageService gcsService, IHttpClientFactory httpClientFactory, DataLayerBase db, IConfiguration configuration, ILogger<ReportsController> logger)
    {
        _blobService = blobService;
        _gcsService = gcsService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _dl = db;
    }

    [HttpPost("create")]
    public async Task<IActionResult> PostAsync([FromHeader] string Authorization, [FromBody] CustomerReportRequest customerReportRequest)
    {
        var um = new UserModule(Authorization, _dl);
        if (!um.Secured) return Unauthorized();

        var user = GetUserBySessionID(Authorization.Substring("Bearer ".Length).Trim());
        //ReportService reportService = new ReportService();
        //
        _logger.LogInformation("Generating Reports for request: {customerReportRequest}", JsonConvert.SerializeObject(customerReportRequest));
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
                    ReportType = customerReportRequest.ReportType,
                    StrButton = customerReportRequest.StrButton,
                    StartDate = customerReportRequest.StartDate,
                    EndDate = customerReportRequest.EndDate,
                    CreatedByID = user.UserId,  // Assuming UserModule provides UserId
                    GenerationStatus = "In Progress",
                    CreatedDate = DateTime.UtcNow,
                    ReportName = $"{customerReportRequest.Customers[i]}_{strStart}_{strEnd}"
                };
                if (existingReport != null)
                {
                    report.ReportID = existingReport.ReportID;
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
            Task.Run(() => ProcessReportsAsync(customerDetails, customerReportRequest.StartDate, customerReportRequest.EndDate, user.UserId, customerReportRequest.ReportType, customerReportRequest.StrButton));
            return Ok(new { message = "Reports generation initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Generating Reports for request: {customerReportRequest}", JsonConvert.SerializeObject(customerReportRequest));
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("retry/{reportId}")]
    public async Task<IActionResult> Retry([FromHeader] string Authorization, [FromRoute] Guid reportId)
    {
        var um = new UserModule(Authorization, _dl);
        if (!um.Secured) return Unauthorized();

        var user = GetUserBySessionID(Authorization.Substring("Bearer ".Length).Trim());
        try
        {
            _logger.LogInformation("Retryiing report generation for reportId: {reportId}", reportId);
            var report = await GetReportByIdAsync(reportId);
            if (report == null)
            {
                return NotFound();
            }
            report.CreatedByID = user.UserId;
            await OverrideReportAsync(report);
            if (report.GenerationStatus == "Failed" || report.GenerationStatus == "Uploaded")
            {
                await UpdateReportAsync(report.ReportID, "In Progress", user.UserId);
                List<CustomerDetails> customerDetails = new List<CustomerDetails>();
                customerDetails.Add(new CustomerDetails() { CustomerName = report.CustomerName, FileName = report.ReportID.ToString(), ReportId = report.ReportID });
                Task.Run(() => ProcessReportsAsync(customerDetails, report.StartDate, report.EndDate, user.UserId, report.ReportType, report.StrButton));
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
            _logger.LogError(ex.Message, "Retryiing report generation failed for reportId: {reportId}", reportId);
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
    //        return Pr1oblem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    [HttpGet("all/{reportType}")]
    public async Task<IActionResult> GetAllReports([FromHeader] string Authorization, [FromRoute] string reportType)
    {
        var um = new UserModule(Authorization, _dl);
        if (!um.Secured) return Unauthorized();
        var user = GetUserBySessionID(Authorization.Substring("Bearer ".Length).Trim());
        _logger.LogInformation("Retryiing all reports for user: {userID}", user.UserId);
        try
        {
            string sql = $@"
                            SELECT ReportId, CustomerName, StartDate, EndDate, CreatedByID, UpdatedByID, GenerationStatus, ReportName, COALESCE(GcsURL, BlobURL) AS BlobURL,GcsURL, ReportType, StrButton,Message, CreatedDate, UpdatedDate
                            FROM Report
                            WHERE IsDeleted = 0 and LOWER(ReportType) = LOWER(@ReportType)
                            ORDER BY CreatedDate DESC";

          
            var x = await _dl.QueryAsync<Report>(sql, new
            {
                ReportType = reportType
            });

            return Ok(x);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Error on fetching all reports");
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadBlob([FromHeader] string Authorization, string blobName)
    {
        var um = new UserModule(Authorization, _dl);
        if (!um.Secured) return Unauthorized();

        _logger.LogInformation("Downlloading Blob Name: {BlobName}", blobName);
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
    [HttpGet("sas-url")]
    public IActionResult GetSasUrl([FromHeader] string Authorization, [FromQuery] string blobName)
    {
        var um = new UserModule(Authorization, _dl);
        if (!um.Secured) return Unauthorized();

        try
        {
            if (string.IsNullOrEmpty(blobName))
            {
                return BadRequest(new { message = "blobName is required" });
            }

            // Decode the blob name in case it's URL-encoded
            var decodedBlobName = Uri.UnescapeDataString(blobName);

            string signedUrl;

            // Check if this is a Google Cloud Storage URL (gs://)
            if (decodedBlobName.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Generating GCS signed URL for: {GcsUri}", decodedBlobName);
                signedUrl = _gcsService.GenerateSignedUrlFromGsUri(decodedBlobName, TimeSpan.FromHours(1));
            }
            else
            {
                // Azure Blob Storage
                signedUrl = _blobService.GetSasUrlForBlob(decodedBlobName, inline: true);
            }

            return Ok(new { url = signedUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate signed URL for blob: {BlobName}", blobName);
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("download-zip")]
    public async Task<IActionResult> DownloadReportsAsZip([FromHeader] string Authorization, [FromBody] List<Guid> reportIds)
    {
        _logger.LogWarning("=== download-zip endpoint called with {Count} report IDs ===", reportIds?.Count ?? 0);

        var um = new UserModule(Authorization, _dl);
        if (!um.Secured)
        {
            _logger.LogWarning("=== download-zip: Authorization failed ===");
            return Unauthorized();
        }

        try
        {
            _logger.LogWarning("=== download-zip: Processing {Count} reports: {Reports} ===", reportIds.Count, string.Join(",", reportIds));
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

                        if (report == null)
                        {
                            _logger.LogWarning("Report not found: {ReportId}", reportId);
                            continue;
                        }

                        _logger.LogInformation("Report {ReportId}: Status={Status}, BlobURL={BlobURL}",
                            reportId, report.GenerationStatus, report.BlobURL ?? "(null)");

                        if (report.GenerationStatus != "Uploaded")
                        {
                            _logger.LogWarning("Report {ReportId} status is '{Status}', not 'Uploaded'", reportId, report.GenerationStatus);
                            continue;
                        }

                        if (string.IsNullOrEmpty(report.BlobURL))
                        {
                            _logger.LogWarning("Report {ReportId} has no BlobURL", reportId);
                            continue;
                        }

                        // Extract blob path from BlobURL (e.g., "streetlights/filename.pdf" or "traffic/filename.pdf")
                        var blobUri = new Uri(report.BlobURL);
                        var blobPath = Uri.UnescapeDataString(string.Concat(blobUri.Segments.Skip(2)));

                        _logger.LogInformation("Downloading blob: {BlobPath}", blobPath);
                        // Download the blob from Azure Storage
                        byte[] blobData = await _blobService.GetBlobAsync(blobPath);

                        if (blobData != null)
                        {
                            // Add each blob as an entry in the zip archive
                            var zipEntry = archive.CreateEntry($"{report.ReportName}.pdf", CompressionLevel.Fastest);
                            using (var zipStream = zipEntry.Open())
                            {
                                await zipStream.WriteAsync(blobData, 0, blobData.Length);
                            }
                            _logger.LogInformation("Added {FileName} to zip ({Size} bytes)", $"{report.ReportName}.pdf", blobData.Length);
                        }
                        else
                        {
                            _logger.LogWarning("Blob not found for report {ReportId}: {BlobPath}", reportId, blobPath);
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
            _logger.LogError(ex, "Failed to download zip: {BlobName}", string.Join(",", reportIds));
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    [HttpDelete("{reportId}")]
    public async Task<IActionResult> DeleteReportAsync([FromHeader] string Authorization, [FromRoute] Guid reportId)
    {
        var um = new UserModule(Authorization, _dl);
        if (!um.Secured) return Unauthorized();

        var user = GetUserBySessionID(Authorization.Substring("Bearer ".Length).Trim());
        _logger.LogInformation("Deleting report {reportId} by user: {userID}", reportId, user.UserId);
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

            // Step 1: Attempt to delete the blob from Azure Blob Storage if the blob URL exists
            // If deletion fails (e.g., file not found), log and continue - the file is likely already gone
            if (!string.IsNullOrEmpty(report.BlobURL))
            {
                bool isBlobDeleted = await _blobService.DeleteBlobAsync($"traffic/{report.ReportName}.pdf");
                if (!isBlobDeleted)
                {
                    _logger.LogWarning("Blob not found or could not be deleted for report {ReportId}: {BlobPath}. Proceeding with database deletion.", reportId, $"traffic/{report.ReportName}.pdf");
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
    private async Task ProcessReportsAsync(List<CustomerDetails> customerDetails, DateTime startDate, DateTime endDate, int userId, string reportType, string strButton = null)
    {

        var client = _httpClientFactory.CreateClient();
        var baseUrl = _configuration["ReportServer"] + "/api";
        // Send request to .NET 4.8 service

        var reportServiceUrl = reportType == "Traffic" ? $"{baseUrl}/Report" : $"{baseUrl}/StreetLight";

        ReportRequest customerReportRequest = new ReportRequest()
        {
            CustomerDetails = customerDetails,
            StartDate = startDate,
            EndDate = endDate,
            StrButton = strButton
        };
        try
        {
            client.Timeout = TimeSpan.FromMinutes(7);
            var response = await client.PostAsJsonAsync(reportServiceUrl, customerReportRequest);

            if (response.IsSuccessStatusCode)
            {

                // Get the report (e.g., PDF) from the .NET Framework service
                var customerReportResponseList = await response.Content.ReadFromJsonAsync<List<CustomerReportResponse>>();
                if (customerReportResponseList.Count > 0)
                {
                    foreach (var report in customerReportResponseList)
                    {
                        if (report.File != null)
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
                            await UpdateFailedStatus(customerReportRequest, userId, report.Message);
                        }
                    }
                }
                else
                {
                    string errorMessage = "No reports generated";
                    if (customerReportResponseList.Count > 0)
                    {
                        errorMessage = customerReportResponseList[0].Message;
                    }

                    await UpdateFailedStatus(customerReportRequest, userId, "No reports generated " + errorMessage);
                }


            }
            else
            {
                await UpdateFailedStatus(customerReportRequest, userId, "Report Generation Failed is status code false");
            }
        }
        catch (Exception ex)
        {
            await UpdateFailedStatus(customerReportRequest, userId, "Report Generation Failed http call exception" + ex.Message);
            throw ex;
        }

    }
    private async Task UpdateFailedStatus(ReportRequest customerReportRequest, int userId, string message = null)
    {
        foreach (var report in customerReportRequest.CustomerDetails)
        {
            await UpdateReportAsync(report.ReportId, "Failed", userId, null, false, message);
        }
    }
    private async Task<Report> AddReportAsync(Report report)
    {
        var sql = @"
            INSERT INTO Report (CustomerName, StartDate, EndDate, CreatedByID, GenerationStatus, ReportName, ReportType, StrButton, BlobURL, CreatedDate, UpdatedDate)
            VALUES (@CustomerName, @StartDate, @EndDate, @CreatedByID, @GenerationStatus, @ReportName,  @ReportType, @StrButton, @BlobURL, NOW(), NOW())
            RETURNING ReportId;";

        
            // Capture the inserted ReportId
            var reportId = await _dl.QuerySingleAsync<Guid>(sql, new
            {
                report.CustomerName,
                report.StartDate,
                report.EndDate,
                report.CreatedByID,
                report.GenerationStatus,
                report.ReportName,
                report.ReportType,
                report.StrButton,
                report.BlobURL
            });

            // Set the generated ReportId on the object
            report.ReportID = reportId;
            report.CreatedDate = DateTime.UtcNow;  // Assuming it's generated during insert
            return report;
        
    }
    private async Task OverrideReportAsync(Report report)
    {
        try
        {
            var sql = @"
                UPDATE Report
                SET GenerationStatus = @GenerationStatus,
                    BlobURL = @BlobURL,
                    CreatedDate = NOW(),
                    CreatedByID = @CreatedByID,
                    UpdatedDate = NOW(),
                    UpdatedByID = @CreatedByID,
                    IsDeleted = 0
                WHERE ReportID = @ReportID;";
 
                await _dl.ExecuteAsync(sql, new
                {
                    GenerationStatus = report.GenerationStatus,
                    BlobURL = report.BlobURL,
                    CreatedByID = report.CreatedByID,
                    ReportID = report.ReportID
                });
             
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Update report: {ReportID}", report.ReportID);
            throw;
        }

    }
    private async Task UpdateReportAsync(Guid reportId, string status, int updateById, string blobUrl = null, bool isDeleted = false, string message = null)
    {
        var sql = @"
        UPDATE Report
        SET GenerationStatus = @GenerationStatus,
            BlobURL = @BlobURL,
            IsDeleted = @IsDeleted,
            Message = @Message,
            UpdatedDate = NOW(),
            UpdatedByID = @UpdatedByID
        WHERE ReportID = @ReportID;";


            await _dl.ExecuteAsync(sql, new
            {
                GenerationStatus = status,
                BlobURL = blobUrl,
                IsDeleted = isDeleted ? 1 : 0,
                Message = message,
                ReportID = reportId,
                UpdatedByID = updateById
            });
      
    }
    private async Task<Report> GetReportByIdAsync(Guid reportId)
    {
        var sql = @"
            SELECT * 
            FROM Report 
            WHERE ReportId = @ReportId;";

     
            // Fetch the report with the given ReportId
            var report = await _dl.QuerySingleOrDefaultAsync<Report>(sql, new { ReportId = reportId });
            return report;
       
    }
    private async Task<Report> FetchReportByCustomerAndDateAsync(string customerName, DateTime startDate, DateTime endDate)
    {
        var sql = @"
        SELECT ReportId, CustomerName, StartDate, EndDate, CreatedByID, UpdatedByID, GenerationStatus, ReportName, BlobURL, ReportType, StrButton, CreatedDate, UpdatedDate
        FROM Report
        WHERE CustomerName = @CustomerName
        AND StartDate::DATE = @StartDate::DATE
        AND EndDate::DATE = @EndDate::DATE
        LIMIT 1;";  // Compare only the date portion

       
            var report = await _dl.QuerySingleOrDefaultAsync<Report>(sql, new
            {
                CustomerName = customerName,
                StartDate = startDate.Date,  // Pass only the date part of the provided StartDate
                EndDate = endDate.Date       // Pass only the date part of the provided EndDate
            });

            return report;
        
    }

    private User GetUserBySessionID(string sid)
    {
        string sql = $"SELECT * FROM fn_security_user_by_session_id('{sid}');";
       
            var userSet = _dl.Query<User>(sql);
            return userSet.First();
         
    }
}
