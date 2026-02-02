using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;

namespace DCElectricWebAPI.Controllers.Pdf;

[Route("api/[controller]")]
[ApiController]
public class StreetlightsFixtureController : ControllerBase
{
    private readonly StreetLightsService _service;
    private readonly ILogger<StreetlightsFixtureController> _logger;

    public StreetlightsFixtureController(
        StreetLightsService service,
        ILogger<StreetlightsFixtureController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get fixture billing data for a customer
    /// </summary>
    /// <param name="details">If true (default), returns all individual locations. If false, aggregates by fixture type and price.</param>
    /// <param name="format">Output format: "json" (default) or "pdf"</param>
    [HttpPost]
    public async Task<IActionResult> GetFixtureBillingData(
        [FromHeader] string Authorization,
        [FromBody] FixtureBillingRequest request,
        [FromQuery] bool details = true,
        [FromQuery] string format = "json")
    {
        try
        {
            var um = new UserModule(Authorization);
            if (!um.Secured) return Unauthorized();

            _logger.LogInformation("Getting fixture billing data for customer: {Customer}, details: {Details}, format: {Format}",
                request.CustomerName, details, format);

            // For PDF, always use aggregated data (details=false)
            bool useDetails = format.Equals("pdf", StringComparison.OrdinalIgnoreCase) ? false : details;

            var response = await _service.GetFixtureBillingDataAsync(request, useDetails);

            // Check for errors that should return BadRequest
            if (!string.IsNullOrEmpty(response.ErrorMessage) && response.Locations.Count == 0)
            {
                if (response.ErrorMessage.Contains("Could not find pricing level") ||
                    response.ErrorMessage.Contains("No maintenance pricing found"))
                {
                    return BadRequest(response.ErrorMessage);
                }
            }

            // Return PDF if requested
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Generating PDF for customer: {Customer}", request.CustomerName);

                var pdfBytes = _service.GenerateFixtureBillingPdf(response);

                // Generate filename
                string safeCustomerName = string.Join("_", request.CustomerName.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"Fixture_{safeCustomerName}_{request.StartDate:M_d}_{request.EndDate:M-d}_{request.EndDate:yyyy}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fixture billing data for {Customer}", request.CustomerName);
            return StatusCode(500, $"Error retrieving fixture data: {ex.Message}");
        }
    }

    /// <summary>
    /// Get ticket billing data for a customer
    /// </summary>
    /// <param name="format">Output format: "json" (default) or "pdf"</param>
    [HttpPost("tickets")]
    public async Task<IActionResult> GetTicketBillingData(
        [FromHeader] string Authorization,
        [FromBody] TicketBillingRequest request,
        [FromQuery] string format = "json")
    {
        try
        {
            var um = new UserModule(Authorization);
            if (!um.Secured) return Unauthorized();

            _logger.LogInformation("Getting ticket billing data for customer: {Customer}, from {Start} to {End}, format: {Format}",
                request.CustomerName, request.StartDate, request.EndDate, format);

            var response = await _service.GetTicketBillingDataAsync(request);

            // Check for errors that should return BadRequest
            if (!string.IsNullOrEmpty(response.ErrorMessage) && response.Tickets.Count == 0)
            {
                return BadRequest(response.ErrorMessage);
            }

            // Return PDF if requested
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Generating Ticket PDF for customer: {Customer}", request.CustomerName);

                var pdfBytes = _service.GenerateTicketBillingPdf(response);

                // Generate filename
                string safeCustomerName = string.Join("_", request.CustomerName.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"Ticket_{safeCustomerName}_{request.StartDate:M_d}_{request.EndDate:M-d}_{request.EndDate:yyyy}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket billing data for {Customer}", request.CustomerName);
            return StatusCode(500, $"Error retrieving ticket data: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all customers with their pricing levels
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromHeader] string Authorization)
    {
        try
        {
            var um = new UserModule(Authorization);
            if (!um.Secured) return Unauthorized();

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
        [FromHeader] string Authorization,
        string customerName)
    {
        try
        {
            var um = new UserModule(Authorization);
            if (!um.Secured) return Unauthorized();

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
        [FromHeader] string Authorization,
        string pricingLevel)
    {
        try
        {
            var um = new UserModule(Authorization);
            if (!um.Secured) return Unauthorized();

            var priceList = await _service.GetMaintenancePriceListAsync(pricingLevel);
            return Ok(priceList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pricing for level {Level}", pricingLevel);
            return StatusCode(500, $"Error retrieving pricing: {ex.Message}");
        }
    }
}
