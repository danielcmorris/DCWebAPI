using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;

namespace DCElectricWebAPI.Controllers.Pdf;

[Route("api/pdf/trafficlights/invoice")]
[ApiController]
public class TrafficLightsInvoiceController : ControllerBase
{
    private readonly TrafficLightsService _service;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AzureBlobService _blobService;
    private readonly ILogger<TrafficLightsInvoiceController> _logger;
    private readonly DataLayerBase _dl;

    // API key for bypassing standard auth (for testing/internal use)
    private const string API_KEY = "dcelectric-sl-2025";

    public TrafficLightsInvoiceController(
        TrafficLightsService service,
        IServiceScopeFactory scopeFactory,
        AzureBlobService blobService,
        DataLayerBase dl,
        ILogger<TrafficLightsInvoiceController> logger)
    {
        _dl = dl;
        _service = service;
        _scopeFactory = scopeFactory;
        _blobService = blobService;
        _logger = logger;
    }

    /// <summary>
    /// Check if request is authorized via API key or standard auth
    /// </summary>
    private bool IsAuthorized(string? authorization, string? apiKey)
    {
        // Check API key first (header or query param)
        if (!string.IsNullOrEmpty(apiKey) && apiKey == API_KEY)
            return true;

        // Check X-Api-Key header
        if (Request.Headers.TryGetValue("X-Api-Key", out var headerKey) && headerKey == API_KEY)
            return true;

        // Fall back to standard UserModule auth
        if (!string.IsNullOrEmpty(authorization))
        {
            try
            {
                var um = new UserModule(authorization, _dl);
                return um.Secured;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Get user from session ID
    /// </summary>
    private User? GetUserBySessionID(string sid)
    {
        string sql = $"SELECT * FROM fn_security_user_by_session_id('{sid}');";
        var userSet = _dl.Query<User>(sql);
        return userSet.FirstOrDefault();
    }

    /// <summary>
    /// Get user ID from authorization header, returns 0 for API key auth
    /// </summary>
    private int GetUserId(string? authorization)
    {
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            return 0;

        var sid = authorization.Substring("Bearer ".Length).Trim();
        var user = GetUserBySessionID(sid);
        return user?.UserId ?? 0;
    }

    #region Database Methods

    private async Task<Report> AddReportAsync(Report report)
    {
        var sql = @"
            INSERT INTO Report (CustomerName, StartDate, EndDate, CreatedByID, GenerationStatus, ReportName, ReportType, StrButton, BlobURL, TicketCount, CreatedDate, UpdatedDate)
            VALUES (@CustomerName, @StartDate, @EndDate, @CreatedByID, @GenerationStatus, @ReportName, @ReportType, @StrButton, @BlobURL, @TicketCount, NOW(), NOW())
            RETURNING ReportId;";

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
            report.BlobURL,
            report.TicketCount
        });

        report.ReportID = reportId;
        report.CreatedDate = DateTime.UtcNow;
        return report;
    }

    private async Task UpdateReportAsync(Guid reportId, string status, int updateById, string? blobUrl = null, bool isDeleted = false, string? message = null, int? ticketCount = null)
    {
        int del = isDeleted ? 1 : 0;

        var sql = @"
        UPDATE Report
        SET GenerationStatus = @status,
            BlobURL = @blobUrl,
            IsDeleted = @isDeleted,
            Message = @message,
            TicketCount = COALESCE(@ticketCount, TicketCount),
            UpdatedDate = NOW(),
            UpdatedByID = @updateById
        WHERE ReportID = @reportId";

        await _dl.ExecuteAsync(sql, new
        {
            reportId,
            status,
            updateById,
            blobUrl,
            isDeleted,
            message,
            ticketCount
        });
    }

    #endregion

    #region Background Processing

    /// <summary>
    /// Process ticket reports in the background
    /// </summary>
    private async Task ProcessTicketReportsAsync(List<TrafficReportTask> reportTasks, int userId)
    {
        // Create a new scope for the background task to safely use scoped services
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TrafficLightsService>();

        foreach (var task in reportTasks)
        {
            try
            {
                _logger.LogInformation("Processing Traffic Light ticket report for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);

                // Build the ticket billing data
                _logger.LogInformation("Fetching Traffic Light ticket billing data for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);
                var response = await service.GetTicketBillingDataAsync(task.Request);
                _logger.LogInformation("Fetched Traffic Light ticket billing data for {Customer}, ReportID: {ReportId}, Tickets: {Count}",
                    task.Request.CustomerName, task.ReportId, response.Tickets.Count);

                if (!string.IsNullOrEmpty(response.ErrorMessage) && response.Tickets.Count == 0)
                {
                    await UpdateReportAsync(task.ReportId, "Failed", userId, message: response.ErrorMessage);
                    continue;
                }

                // Generate PDF
                _logger.LogInformation("Generating Traffic Light ticket PDF for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);
                var pdfBytes = service.GenerateTicketBillingPdf(response);
                _logger.LogInformation("Generated Traffic Light ticket PDF for {Customer}, ReportID: {ReportId}, Size: {Size} bytes",
                    task.Request.CustomerName, task.ReportId, pdfBytes.Length);

                // Generate filename
                string strStart = $"{task.Request.StartDate.Year}{task.Request.StartDate.Month:00}{task.Request.StartDate.Day:00}_";
                string strEnd = $"{task.Request.EndDate.Year}{task.Request.EndDate.Month:00}{task.Request.EndDate.Day:00}";
                string fileName = $"{task.Request.CustomerName}_{strStart}_{strEnd}.pdf";

                // Upload to Azure
                _logger.LogInformation("Uploading Traffic Light ticket PDF to Azure for {Customer}, ReportID: {ReportId}, FileName: {FileName}",
                    task.Request.CustomerName, task.ReportId, fileName);
                string blobUrl = await _blobService.UploadFileAsync(pdfBytes, fileName, "trafficlights");
                _logger.LogInformation("Uploaded Traffic Light ticket PDF to Azure for {Customer}, ReportID: {ReportId}, BlobUrl: {BlobUrl}",
                    task.Request.CustomerName, task.ReportId, blobUrl);

                // Update report status to completed/uploaded with ticket count
                _logger.LogInformation("Updating report status to Uploaded for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);
                await UpdateReportAsync(task.ReportId, "Uploaded", userId, blobUrl, ticketCount: response.TicketCount);

                _logger.LogInformation("Completed Traffic Light ticket report for {Customer}, ReportID: {ReportId}, TicketCount: {Count}",
                    task.Request.CustomerName, task.ReportId, response.TicketCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Traffic Light ticket report for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);
                try
                {
                    await UpdateReportAsync(task.ReportId, "Failed", userId,
                        message: $"{ex.GetType().Name}: {ex.Message}");
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update report status to Failed for ReportID: {ReportId}", task.ReportId);
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// Generate Traffic Light ticket billing reports for multiple customers (batch processing)
    /// </summary>
    /// <remarks>
    /// Accepts an array of report requests, creates report records in the database,
    /// starts background tasks to generate PDFs and upload to Azure, then returns
    /// the report IDs immediately for status polling.
    ///
    /// Use format=pdf for synchronous PDF generation (first request only).
    /// Use format=store (default) for async batch processing.
    /// </remarks>
    [HttpPost("tickets")]
    public async Task<IActionResult> GenerateTicketReports(
        [FromHeader] string? Authorization,
        [FromBody] List<TrafficTicketBillingRequest> requests,
        [FromQuery] string? apiKey = null,
        [FromQuery] string? format = "store")
    {
        try
        {
            if (!IsAuthorized(Authorization, apiKey)) return Unauthorized();

            var userId = GetUserId(Authorization);

            // format=pdf: process only the first request synchronously and return the PDF directly
            if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                var request = requests.First();
                _logger.LogInformation("Generating synchronous Traffic Light ticket PDF for {Customer}", request.CustomerName);

                string strStart = $"{request.StartDate.Year}{request.StartDate.Month:00}{request.StartDate.Day:00}_";
                string strEnd = $"{request.EndDate.Year}{request.EndDate.Month:00}{request.EndDate.Day:00}";
                string fileName = $"{request.CustomerName}_{strStart}_{strEnd}.pdf";

                var report = new Report
                {
                    CustomerName = request.CustomerName,
                    ReportType = "Traffic",
                    StrButton = "Ticket",
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedByID = userId,
                    GenerationStatus = "In Progress",
                    CreatedDate = DateTime.UtcNow,
                    ReportName = $"{request.CustomerName}_{strStart}_{strEnd}"
                };
                var createdReport = await AddReportAsync(report);

                try
                {
                    var response = await _service.GetTicketBillingDataAsync(request);

                    if (!string.IsNullOrEmpty(response.ErrorMessage) && response.Tickets.Count == 0)
                    {
                        await UpdateReportAsync(createdReport.ReportID, "Failed", userId, message: response.ErrorMessage);
                        return BadRequest(new { error = response.ErrorMessage });
                    }

                    var pdfBytes = _service.GenerateTicketBillingPdf(response);

                    string blobUrl = await _blobService.UploadFileAsync(pdfBytes, fileName, "trafficlights");
                    await UpdateReportAsync(createdReport.ReportID, "Uploaded", userId, blobUrl, ticketCount: response.TicketCount);

                    _logger.LogInformation("Completed synchronous Traffic Light ticket PDF for {Customer}, Size: {Size} bytes",
                        request.CustomerName, pdfBytes.Length);

                    return File(pdfBytes, "application/pdf", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate synchronous Traffic Light ticket PDF for {Customer}", request.CustomerName);
                    try
                    {
                        await UpdateReportAsync(createdReport.ReportID, "Failed", userId,
                            message: $"{ex.GetType().Name}: {ex.Message}");
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx, "Failed to update report status to Failed for ReportID: {ReportId}", createdReport.ReportID);
                    }
                    return StatusCode(500, $"Error generating PDF: {ex.Message}");
                }
            }

            // format=store (default): batch processing with background task
            _logger.LogInformation("Initiating batch Traffic Light ticket report generation for {Count} customers", requests.Count);

            var reportTasks = new List<TrafficReportTask>();
            var reportResponses = new List<TrafficReportResponse>();

            // Create report records for each request
            foreach (var request in requests)
            {
                string strStart = $"{request.StartDate.Year}{request.StartDate.Month:00}{request.StartDate.Day:00}_";
                string strEnd = $"{request.EndDate.Year}{request.EndDate.Month:00}{request.EndDate.Day:00}";

                var report = new Report
                {
                    CustomerName = request.CustomerName,
                    ReportType = "Traffic",
                    StrButton = "Ticket",
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedByID = userId,
                    GenerationStatus = "In Progress",
                    CreatedDate = DateTime.UtcNow,
                    ReportName = $"{request.CustomerName}_{strStart}_{strEnd}"
                };

                var createdReport = await AddReportAsync(report);

                reportTasks.Add(new TrafficReportTask
                {
                    ReportId = createdReport.ReportID,
                    Request = request
                });

                reportResponses.Add(new TrafficReportResponse
                {
                    ReportId = createdReport.ReportID,
                    CustomerName = request.CustomerName,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    GenerationStatus = "In Progress"
                });

                _logger.LogInformation("Created Traffic Light ticket report record for {Customer}, ReportID: {ReportId}",
                    request.CustomerName, createdReport.ReportID);
            }

            // Start background processing without waiting
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessTicketReportsAsync(reportTasks, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in background Traffic Light ticket report processing");
                    try
                    {
                        foreach (var task in reportTasks)
                            await UpdateReportAsync(task.ReportId, "Failed", userId, message: $"Background task error: {ex.Message}");
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Failed to update report status after background task error");
                    }
                }
            });

            return Ok(reportResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating batch Traffic Light ticket report generation");
            return StatusCode(500, $"Error initiating report generation: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all Traffic Light customers
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(
        [FromHeader] string? Authorization,
        [FromQuery] string? apiKey = null)
    {
        try
        {
            if (!IsAuthorized(Authorization, apiKey)) return Unauthorized();

            var customers = await _service.GetCustomersAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Traffic Light customers");
            return StatusCode(500, $"Error retrieving customers: {ex.Message}");
        }
    }

    /// <summary>
    /// Get divisions for a Traffic Light customer
    /// </summary>
    [HttpGet("divisions/{customerName}")]
    public async Task<IActionResult> GetDivisions(
        [FromHeader] string? Authorization,
        string customerName,
        [FromQuery] string? apiKey = null)
    {
        try
        {
            if (!IsAuthorized(Authorization, apiKey)) return Unauthorized();

            var divisions = await _service.GetCustomerDivisionsAsync(customerName);
            return Ok(divisions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting divisions for Traffic Light customer {Customer}", customerName);
            return StatusCode(500, $"Error retrieving divisions: {ex.Message}");
        }
    }

    /// <summary>
    /// Get maintenance pricing table for a Traffic Light customer
    /// </summary>
    [HttpGet("pricing/{customerName}")]
    public async Task<IActionResult> GetPricing(
        [FromHeader] string? Authorization,
        string customerName,
        [FromQuery] string? apiKey = null)
    {
        try
        {
            if (!IsAuthorized(Authorization, apiKey)) return Unauthorized();

            var priceList = await _service.GetMaintenancePriceListAsync(customerName);
            return Ok(priceList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance pricing for Traffic Light customer {Customer}", customerName);
            return StatusCode(500, $"Error retrieving pricing: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all Traffic Light tickets across all customers in a date range
    /// </summary>
    /// <param name="startDate">Start date (completion date)</param>
    /// <param name="endDate">End date (completion date)</param>
    [HttpGet("tickets/all")]
    public async Task<IActionResult> GetAllTickets(
        [FromHeader] string? Authorization,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? apiKey = null)
    {
        try
        {
            if (!IsAuthorized(Authorization, apiKey)) return Unauthorized();

            _logger.LogInformation("Getting all Traffic Light tickets from {Start} to {End}", startDate, endDate);

            var tickets = await _service.GetAllTicketsAsync(startDate, endDate);

            return Ok(new
            {
                startDate,
                endDate,
                totalCount = tickets.Count,
                tickets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all Traffic Light tickets");
            return StatusCode(500, $"Error retrieving tickets: {ex.Message}");
        }
    }
}

#region Internal Models

/// <summary>
/// Internal class to track Traffic Light report tasks for background processing
/// </summary>
internal class TrafficReportTask
{
    public Guid ReportId { get; set; }
    public TrafficTicketBillingRequest Request { get; set; } = null!;
}

/// <summary>
/// Response model for batch Traffic Light report generation
/// </summary>
public class TrafficReportResponse
{
    public Guid ReportId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GenerationStatus { get; set; } = string.Empty;
}

#endregion
