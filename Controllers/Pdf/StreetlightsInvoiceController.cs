using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;

namespace DCElectricWebAPI.Controllers.Pdf;

[Route("api/pdf/streetlights/invoice")]
[ApiController]
public class StreetlightsInvoiceController : ControllerBase
{
    private readonly StreetLightsService _service;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AzureBlobService _blobService;
    private readonly ILogger<StreetlightsInvoiceController> _logger;

    // API key for bypassing standard auth (for testing/internal use)
    private const string API_KEY = "dcelectric-sl-2025";

    public StreetlightsInvoiceController(
        StreetLightsService service,
        IServiceScopeFactory scopeFactory,
        AzureBlobService blobService,
        ILogger<StreetlightsInvoiceController> logger)
    {
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
                var um = new UserModule(authorization);
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
        string sql = $"SELECT * FROM [dbo].[fnSecurity_UserBySessionId]('{sid}');";
        using (var dl = new DataLayerBase())
        {
            var userSet = dl.Query<User>(sql);
            return userSet.FirstOrDefault();
        }
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
            OUTPUT INSERTED.ReportId
            VALUES (@CustomerName, @StartDate, @EndDate, @CreatedByID, @GenerationStatus, @ReportName, @ReportType, @StrButton, @BlobURL, @TicketCount, GETDATE(), GETDATE());";

        using (var dl = new DataLayerBase())
        {
            var reportId = await dl.QuerySingleAsync<Guid>(sql, new
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
    }

    private async Task UpdateReportAsync(Guid reportId, string status, int updateById, string? blobUrl = null, bool isDeleted = false, string? message = null, int? ticketCount = null)
    {
        var sql = @"
            UPDATE Report
            SET GenerationStatus = @GenerationStatus,
                BlobURL = @BlobURL,
                IsDeleted = @IsDeleted,
                Message = @Message,
                TicketCount = COALESCE(@TicketCount, TicketCount),
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
                Message = message,
                TicketCount = ticketCount,
                ReportID = reportId,
                UpdatedByID = updateById
            });
        }
    }

    #endregion

    #region Background Processing

    /// <summary>
    /// Process fixture reports in the background
    /// </summary>
    private async Task ProcessFixtureReportsAsync(List<ReportTask> reportTasks, int userId)
    {
        // Create a new scope for the background task to safely use scoped services
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<StreetLightsService>();

        foreach (var task in reportTasks)
        {
            try
            {
                _logger.LogInformation("Processing fixture report for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);

                // Build the fixture billing data
                var response = await service.GetFixtureBillingDataAsync(task.Request, details: false);

                if (!string.IsNullOrEmpty(response.ErrorMessage) && response.Locations.Count == 0)
                {
                    await UpdateReportAsync(task.ReportId, "Failed", userId, message: response.ErrorMessage);
                    continue;
                }

                // Generate PDF
                var pdfBytes = service.GenerateFixtureBillingPdf(response);

                // Generate filename using report naming convention (include Fixture to avoid collision with Tickets)
                string strStart = $"{task.Request.StartDate.Year}{task.Request.StartDate.Month:00}{task.Request.StartDate.Day:00}_";
                string strEnd = $"{task.Request.EndDate.Year}{task.Request.EndDate.Month:00}{task.Request.EndDate.Day:00}";
                string fileName = $"Fixture_{task.Request.CustomerName}_{strStart}_{strEnd}.pdf";

                // Upload to Azure
                string blobUrl = await _blobService.UploadFileAsync(pdfBytes, fileName, "streetlights");

                // Update report status to completed/uploaded with fixture count
                await UpdateReportAsync(task.ReportId, "Uploaded", userId, blobUrl, ticketCount: response.TotalFixtures);

                _logger.LogInformation("Completed fixture report for {Customer}, ReportID: {ReportId}, FixtureCount: {Count}",
                    task.Request.CustomerName, task.ReportId, response.TotalFixtures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process fixture report for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);
                await UpdateReportAsync(task.ReportId, "Failed", userId, message: ex.Message);
            }
        }
    }

    /// <summary>
    /// Process ticket reports in the background
    /// </summary>
    private async Task ProcessTicketReportsAsync(List<ReportTask> reportTasks, int userId)
    {
        // Create a new scope for the background task to safely use scoped services
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<StreetLightsService>();

        foreach (var task in reportTasks)
        {
            try
            {
                _logger.LogInformation("Processing ticket report for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);

                // Build the ticket billing data
                var ticketRequest = new TicketBillingRequest
                {
                    CustomerName = task.Request.CustomerName,
                    StartDate = task.Request.StartDate,
                    EndDate = task.Request.EndDate,
                    DivisionId = task.Request.DivisionId,
                    DivisionName = task.Request.DivisionName
                };

                var response = await service.GetTicketBillingDataAsync(ticketRequest);

                if (!string.IsNullOrEmpty(response.ErrorMessage) && response.Tickets.Count == 0)
                {
                    await UpdateReportAsync(task.ReportId, "Failed", userId, message: response.ErrorMessage);
                    continue;
                }

                // Generate PDF
                var pdfBytes = service.GenerateTicketBillingPdf(response);

                // Generate filename using report naming convention (include Tickets to avoid collision with Fixture)
                string strStart = $"{task.Request.StartDate.Year}{task.Request.StartDate.Month:00}{task.Request.StartDate.Day:00}_";
                string strEnd = $"{task.Request.EndDate.Year}{task.Request.EndDate.Month:00}{task.Request.EndDate.Day:00}";
                string fileName = $"Tickets_{task.Request.CustomerName}_{strStart}_{strEnd}.pdf";

                // Upload to Azure
                string blobUrl = await _blobService.UploadFileAsync(pdfBytes, fileName, "streetlights");

                // Update report status to completed/uploaded with ticket count
                await UpdateReportAsync(task.ReportId, "Uploaded", userId, blobUrl, ticketCount: response.TicketCount);

                _logger.LogInformation("Completed ticket report for {Customer}, ReportID: {ReportId}, TicketCount: {Count}",
                    task.Request.CustomerName, task.ReportId, response.TicketCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ticket report for {Customer}, ReportID: {ReportId}",
                    task.Request.CustomerName, task.ReportId);
                await UpdateReportAsync(task.ReportId, "Failed", userId, message: ex.Message);
            }
        }
    }

    #endregion

    /// <summary>
    /// Generate fixture billing reports for multiple customers (batch processing)
    /// </summary>
    /// <remarks>
    /// Accepts an array of report requests, creates report records in the database,
    /// starts background tasks to generate PDFs and upload to Azure, then returns
    /// the report IDs immediately for status polling.
    /// </remarks>
    [HttpPost("fixture")]
    public async Task<IActionResult> GenerateFixtureReports(
        [FromHeader] string? Authorization,
        [FromBody] List<FixtureBillingRequest> requests,
        [FromQuery] string? apiKey = null)
    {
        try
        {
            if (!IsAuthorized(Authorization, apiKey)) return Unauthorized();

            var userId = GetUserId(Authorization);

            _logger.LogInformation("Initiating batch fixture report generation for {Count} customers", requests.Count);

            var reportTasks = new List<ReportTask>();
            var reportResponses = new List<ReportResponse>();

            // Create report records for each request
            foreach (var request in requests)
            {
                string strStart = $"{request.StartDate.Year}{request.StartDate.Month:00}{request.StartDate.Day:00}_";
                string strEnd = $"{request.EndDate.Year}{request.EndDate.Month:00}{request.EndDate.Day:00}";

                var report = new Report
                {
                    CustomerName = request.CustomerName,
                    ReportType = "Streetlight",
                    StrButton = "Fixture",
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedByID = userId,
                    GenerationStatus = "In Progress",
                    CreatedDate = DateTime.UtcNow,
                    ReportName = $"Fixture_{request.CustomerName}_{strStart}_{strEnd}"
                };

                var createdReport = await AddReportAsync(report);

                reportTasks.Add(new ReportTask
                {
                    ReportId = createdReport.ReportID,
                    Request = request
                });

                reportResponses.Add(new ReportResponse
                {
                    ReportId = createdReport.ReportID,
                    CustomerName = request.CustomerName,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    GenerationStatus = "In Progress"
                });

                _logger.LogInformation("Created fixture report record for {Customer}, ReportID: {ReportId}",
                    request.CustomerName, createdReport.ReportID);
            }

            // Start background processing without waiting
            Task.Run(() => ProcessFixtureReportsAsync(reportTasks, userId));

            return Ok(reportResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating batch fixture report generation");
            return StatusCode(500, $"Error initiating report generation: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate ticket billing reports for multiple customers (batch processing)
    /// </summary>
    /// <remarks>
    /// Accepts an array of report requests, creates report records in the database,
    /// starts background tasks to generate PDFs and upload to Azure, then returns
    /// the report IDs immediately for status polling.
    /// </remarks>
    [HttpPost("tickets")]
    public async Task<IActionResult> GenerateTicketReports(
        [FromHeader] string? Authorization,
        [FromBody] List<FixtureBillingRequest> requests,
        [FromQuery] string? apiKey = null)
    {
        try
        {
            if (!IsAuthorized(Authorization, apiKey)) return Unauthorized();

            var userId = GetUserId(Authorization);

            _logger.LogInformation("Initiating batch ticket report generation for {Count} customers", requests.Count);

            var reportTasks = new List<ReportTask>();
            var reportResponses = new List<ReportResponse>();

            // Create report records for each request
            foreach (var request in requests)
            {
                string strStart = $"{request.StartDate.Year}{request.StartDate.Month:00}{request.StartDate.Day:00}_";
                string strEnd = $"{request.EndDate.Year}{request.EndDate.Month:00}{request.EndDate.Day:00}";

                var report = new Report
                {
                    CustomerName = request.CustomerName,
                    ReportType = "Streetlight",
                    StrButton = "Ticket",
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    CreatedByID = userId,
                    GenerationStatus = "In Progress",
                    CreatedDate = DateTime.UtcNow,
                    ReportName = $"Tickets_{request.CustomerName}_{strStart}_{strEnd}"
                };

                var createdReport = await AddReportAsync(report);

                reportTasks.Add(new ReportTask
                {
                    ReportId = createdReport.ReportID,
                    Request = request
                });

                reportResponses.Add(new ReportResponse
                {
                    ReportId = createdReport.ReportID,
                    CustomerName = request.CustomerName,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    GenerationStatus = "In Progress"
                });

                _logger.LogInformation("Created ticket report record for {Customer}, ReportID: {ReportId}",
                    request.CustomerName, createdReport.ReportID);
            }

            // Start background processing without waiting
            Task.Run(() => ProcessTicketReportsAsync(reportTasks, userId));

            return Ok(reportResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating batch ticket report generation");
            return StatusCode(500, $"Error initiating report generation: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all customers with their pricing levels
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
            _logger.LogError(ex, "Error getting customers");
            return StatusCode(500, $"Error retrieving customers: {ex.Message}");
        }
    }

    /// <summary>
    /// Get divisions for a customer
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
            _logger.LogError(ex, "Error getting divisions for {Customer}", customerName);
            return StatusCode(500, $"Error retrieving divisions: {ex.Message}");
        }
    }

    /// <summary>
    /// Get maintenance pricing table
    /// </summary>
    [HttpGet("pricing/{pricingLevel}")]
    public async Task<IActionResult> GetPricing(
        [FromHeader] string? Authorization,
        string pricingLevel,
        [FromQuery] string? apiKey = null)
    {
        try
        {
            if (!IsAuthorized(Authorization, apiKey)) return Unauthorized();

            var priceList = await _service.GetMaintenancePriceListAsync(pricingLevel);
            return Ok(priceList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pricing for level {Level}", pricingLevel);
            return StatusCode(500, $"Error retrieving pricing: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all tickets across all customers in a date range
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

            _logger.LogInformation("Getting all tickets from {Start} to {End}", startDate, endDate);

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
            _logger.LogError(ex, "Error getting all tickets");
            return StatusCode(500, $"Error retrieving tickets: {ex.Message}");
        }
    }
}

#region Internal Models

/// <summary>
/// Internal class to track report tasks for background processing
/// </summary>
internal class ReportTask
{
    public Guid ReportId { get; set; }
    public FixtureBillingRequest Request { get; set; } = null!;
}

/// <summary>
/// Response model for batch report generation
/// </summary>
public class ReportResponse
{
    public Guid ReportId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GenerationStatus { get; set; } = string.Empty;
}

#endregion
