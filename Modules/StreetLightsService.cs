using DCElectricWebAPI.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Text;
using WebSupergoo.ABCpdf13;
using static DCElectricWebAPI.Models.QuickBaseLibrary;

namespace DCElectricWebAPI.Modules;

public class StreetLightsService
{
    private readonly IOptions<QuickBaseSettings> _settings;
    private readonly ILogger<StreetLightsService> _logger;

    // QuickBase Table IDs for Streetlights app (bjrvqd33c)
    private const string CustomersTableId = "bjrvqd33q";           // Customers table
    private const string LocationsTableId = "bjrvqd338";           // Locations table
    private const string MaintenancePricingTableId = "bjrvqd35a";  // Maintenance Pricing Table
    private const string DivisionsTableId = "bmwe2vm9x";           // Customer Division Names table
    private const string TicketsTableId = "bjrvqd33t";             // Tickets table
    private const string LaborLineItemsTableId = "bjrvqd34z";      // Labor Line Items table
    private const string LaborPricingTableId = "bjrvqd346";        // Labor Pricing table
    private const string MaterialLineItemsTableId = "bjrvqd34t";   // Material Line Items table
    private const string MaterialPricingTableId = "bjrvqd343";     // Material Pricing table
    private const string EquipmentLineItemsTableId = "bjrvqd34w";  // Equipment Line Items table
    private const string EquipmentPricingTableId = "bjrvqd347";    // Equipment Pricing table
    private const string TeamMembersTableId = "bjrvqd337";         // Team Members table

    // Field IDs for Customers table
    private static class CustomerFields
    {
        public const int RecordId = 3;
        public const int CustomerName = 6;
        public const int GroupPricingLevel = 62;
    }

    // Field IDs for Locations table
    private static class LocationFields
    {
        public const int RecordId = 3;
        public const int CustomerName = 42;
        public const int DivisionId = 137;
        public const int StreetLightNumber = 45;
        public const int FixtureType = 7;
        public const int FixtureQuantity = 9;
    }

    // Field IDs for Maintenance Pricing table
    private static class MaintenancePricingFields
    {
        public const int RecordId = 3;
        public const int ServiceType = 12;      // Group Pricing Level (A, B, Z)
        public const int LocationType = 9;      // Fixture Type
        public const int MaintenancePrice = 7;
    }

    // Field IDs for Customer Division Names table
    private static class DivisionFields
    {
        public const int RecordId = 3;
        public const int CustomerLink = 7;
        public const int DivisionName = 6;
    }

    // Field IDs for Tickets table
    // Note: Field IDs verified against actual QuickBase schema 2026-02-02
    private static class TicketFields
    {
        public const int RecordId = 3;
        public const int TicketId = 27;           // Ticket ID (e.g., SL-022640)
        public const int CustomerName = 18;
        public const int CompletionDate = 45;
        public const int DoNotReport = 210;
        public const int DivisionId = 212;
        public const int ServiceType = 7;         // Service Type (Routine, Emergency Response, etc.)
        public const int CallerName = 99;         // Caller Name (text field)
        public const int CallerType = 21;         // Caller Type
        public const int FixtureType = 105;       // Fixture Type (lookup from location)
        public const int StreetLightNumber = 151; // Street Light # (text field)
        public const int AddressCalc = 201;       // Address (text-formula)
        public const int CrossStreet = 150;       // Cross Street (text field)
        public const int DateTimeOpened = 25;
        public const int JobNumber = 119;         // Job #
        public const int ProblemType = 8;         // Problem Type
        public const int Details = 10;            // Details (text-multi-line)
        public const int NotBillable = 167;       // Not Billable (checkbox)
        public const int BillableOverride = 204;  // Billable Override (checkbox)
        public const int StartDate = 43;          // Start Date (date)
        public const int StartTime = 44;          // Start Time (time-of-day)
        public const int CompletionTime = 46;
        public const int CompletedBy = 56;        // Completed By (user field - technician)
        public const int Analysis = 16;           // Analysis (text-multi-line)
    }

    // Field IDs for Labor Line Items table
    // Verified against QuickBase schema 2025-05-31
    private static class LaborFields
    {
        public const int RecordId = 3;
        public const int Date = 6;
        public const int Hours = 7;
        public const int TypeOfHours = 8;
        public const int TeamMember = 9;           // User field
        public const int RelatedTicket = 10;       // Numeric FK to Tickets table
        public const int TicketId = 11;            // Text lookup of Ticket ID
        public const int TypeOfLabor = 12;         // Electrician, Laborer, etc.
    }

    // Field IDs for Labor Pricing table
    private static class LaborPricingFields
    {
        public const int RecordId = 3;
        public const int Customer = 6;
        public const int TypeOfLabor = 7;
        public const int TypeOfHours = 8;
        public const int LaborPrice = 9;
    }

    // Field IDs for Material Line Items table
    // Corrected to match actual QuickBase schema per FIELD_MAPPINGS.md
    private static class MaterialFields
    {
        public const int RecordId = 3;
        public const int Quantity = 6;
        public const int RelatedTicket = 13;            // Ticket ID (text lookup for filtering)
        public const int ItemId = 24;                   // Item ID
        public const int ItemDescription = 25;          // Item Description (text lookup)
        public const int ItemIdListPrice = 26;          // Item ID - List Price (currency lookup)
        public const int UnitOfMeasure = 27;            // Unit of Measurement (text lookup)
        public const int NonInventory = 32;             // Non-Inventory Material (checkbox)
        public const int NonInventorySalePrice = 35;    // Non-Inventory Material SALE Price (currency)
        public const int MaterialDescriptionCalc = 36;  // Material Description CALC (formula)
    }

    // Field IDs for Material Pricing table
    // Corrected to match actual QuickBase schema per FIELD_MAPPINGS.md
    private static class MaterialPricingFields
    {
        public const int RecordId = 3;
        public const int SellPrice = 8;         // Sell Price (currency)
        public const int PricingGroup = 14;     // Group Pricing Level (A, B, E, etc.)
        public const int ItemId = 27;           // Item ID (e.g., 1201007001)
        public const int LumpSum = 32;          // Lump Sum (checkbox)
    }

    // Field IDs for Equipment Line Items table
    // Verified against QuickBase schema 2026-02-02
    private static class EquipmentFields
    {
        public const int RecordId = 3;
        public const int Hours = 6;               // Hours (numeric)
        public const int RelatedTicket = 11;      // Ticket ID (text lookup, e.g., "SL-022640")
        public const int Equipment = 9;           // Equipment (text lookup - equipment name)
    }

    // Field IDs for Equipment Pricing table (per FIELD_MAPPINGS.md)
    private static class EquipmentPricingFields
    {
        public const int RecordId = 3;
        public const int Customer = 6;      // Customer Name
        public const int Equipment = 7;     // Equipment
        public const int EquipmentRate = 9; // Price
    }

    // Field IDs for Team Members table
    private static class TeamMemberFields
    {
        public const int RecordId = 3;
        public const int UserId = 6;
        public const int FirstName = 7;
        public const int LastName = 8;
    }

    public StreetLightsService(
        IOptions<QuickBaseSettings> settings,
        ILogger<StreetLightsService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Get complete fixture billing data for a customer
    /// </summary>
    public async Task<FixtureBillingResponse> GetFixtureBillingDataAsync(
        FixtureBillingRequest request,
        bool details = true)
    {
        var qb = new QuickBaseConnector(_settings);

        // Step 1: Get customer's pricing level
        var pricingLevel = await GetCustomerPricingLevelAsync(request.CustomerName);
        if (string.IsNullOrEmpty(pricingLevel))
        {
            return new FixtureBillingResponse
            {
                CustomerName = request.CustomerName,
                DivisionName = request.DivisionName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                BillingPeriod = GetBillingPeriod(request.StartDate),
                ErrorMessage = $"Could not find pricing level for customer: {request.CustomerName}"
            };
        }

        _logger.LogInformation("Customer {Customer} has pricing level: {Level}", request.CustomerName, pricingLevel);

        // Step 2: Get maintenance prices for this pricing level
        var priceTable = await GetMaintenancePricesAsync(pricingLevel);
        if (priceTable.Count == 0)
        {
            return new FixtureBillingResponse
            {
                CustomerName = request.CustomerName,
                DivisionName = request.DivisionName,
                PricingLevel = pricingLevel,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                BillingPeriod = GetBillingPeriod(request.StartDate),
                ErrorMessage = $"No maintenance pricing found for pricing level: {pricingLevel}"
            };
        }

        _logger.LogInformation("Loaded {Count} maintenance price records", priceTable.Count);

        // Step 3: Get fixture locations for the customer
        var locations = await GetFixtureLocationsAsync(request.CustomerName, request.DivisionId);
        if (locations.Count == 0)
        {
            return new FixtureBillingResponse
            {
                CustomerName = request.CustomerName,
                DivisionName = request.DivisionName,
                PricingLevel = pricingLevel,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                BillingPeriod = GetBillingPeriod(request.StartDate),
                Locations = new List<FixtureLocationData>(),
                TotalAmount = 0,
                TotalFixtures = 0,
                ErrorMessage = "No fixture locations found for this customer"
            };
        }

        // Step 4: Apply pricing to each location
        foreach (var location in locations)
        {
            if (priceTable.TryGetValue(location.FixtureType, out var price))
            {
                location.Price = price;
            }
            else
            {
                _logger.LogWarning("No price found for fixture type: {Type}", location.FixtureType);
                location.Price = 0;
            }
        }

        // Step 5: Aggregate if details=false
        List<FixtureLocationData> resultLocations;
        if (details)
        {
            resultLocations = locations;
        }
        else
        {
            // Group by FixtureType and Price, sum quantities
            resultLocations = locations
                .GroupBy(l => new { l.FixtureType, l.Price })
                .Select(g => new FixtureLocationData
                {
                    RecordId = string.Empty,
                    CustomerName = request.CustomerName,
                    StreetLightNumber = string.Empty,
                    FixtureType = g.Key.FixtureType,
                    FixtureQuantity = g.Sum(x => x.FixtureQuantity),
                    Price = g.Key.Price
                })
                .OrderBy(l => l.FixtureType)
                .ToList();
        }

        // Step 6: Build response
        var response = new FixtureBillingResponse
        {
            CustomerName = request.CustomerName,
            DivisionName = request.DivisionName,
            PricingLevel = pricingLevel,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BillingPeriod = GetBillingPeriod(request.StartDate),
            Locations = resultLocations,
            TotalAmount = resultLocations.Sum(l => l.LineTotal),
            TotalFixtures = resultLocations.Sum(l => l.FixtureQuantity)
        };

        _logger.LogInformation("Fixture billing data complete: {Count} locations, Total: {Total:C}, Aggregated: {Aggregated}",
            resultLocations.Count, response.TotalAmount, !details);

        return response;
    }

    /// <summary>
    /// Get all customers with their pricing levels
    /// </summary>
    public async Task<List<CustomerPricingData>> GetCustomersAsync()
    {
        var qb = new QuickBaseConnector(_settings);
        var query = new QBQuery
        {
            from = CustomersTableId,
            select = new List<int>
            {
                CustomerFields.RecordId,
                CustomerFields.CustomerName,
                CustomerFields.GroupPricingLevel
            },
            options = new QBQueryOptions { top = 500 }
        };

        var result = await qb.Query(query);
        var customers = new List<CustomerPricingData>();

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                customers.Add(new CustomerPricingData
                {
                    CustomerName = GetStringValue(obj, CustomerFields.CustomerName),
                    GroupPricingLevel = GetStringValue(obj, CustomerFields.GroupPricingLevel)
                });
            }
        }

        return customers.OrderBy(c => c.CustomerName).ToList();
    }

    /// <summary>
    /// Get pricing level for a specific customer
    /// </summary>
    public async Task<string> GetCustomerPricingLevelAsync(string customerName)
    {
        var qb = new QuickBaseConnector(_settings);
        var query = new QBQuery
        {
            from = CustomersTableId,
            select = new List<int>
            {
                CustomerFields.CustomerName,
                CustomerFields.GroupPricingLevel
            },
            where = $"{{{CustomerFields.CustomerName}.EX.'{EscapeQueryValue(customerName)}'}}",
            options = new QBQueryOptions { top = 1 }
        };

        _logger.LogInformation("GetCustomerPricingLevelAsync: Looking up pricing level for customer '{CustomerName}'", customerName);

        var result = await qb.Query(query);

        _logger.LogInformation("GetCustomerPricingLevelAsync: Query returned {Count} records", result?.data?.Count ?? 0);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];

            // Log the raw data to see what we got
            _logger.LogInformation("GetCustomerPricingLevelAsync: Raw record data: {Data}", obj.ToString());

            var foundName = GetStringValue(obj, CustomerFields.CustomerName);
            var pricingLevel = GetStringValue(obj, CustomerFields.GroupPricingLevel);
            _logger.LogInformation("GetCustomerPricingLevelAsync: Found customer '{FoundName}' with pricing level '{PricingLevel}' (field {FieldId})",
                foundName, pricingLevel, CustomerFields.GroupPricingLevel);
            return pricingLevel;
        }

        // Try a contains search to find similar customer names for debugging
        var containsQuery = new QBQuery
        {
            from = CustomersTableId,
            select = new List<int>
            {
                CustomerFields.CustomerName,
                CustomerFields.GroupPricingLevel
            },
            where = $"{{{CustomerFields.CustomerName}.CT.'{EscapeQueryValue(customerName.Split(' ')[0])}'}}",
            options = new QBQueryOptions { top = 10 }
        };

        var containsResult = await qb.Query(containsQuery);
        if (containsResult?.data != null && containsResult.data.Count > 0)
        {
            var similarNames = containsResult.data.Select(d => GetStringValue((JObject)d, CustomerFields.CustomerName)).ToList();
            _logger.LogWarning("GetCustomerPricingLevelAsync: Customer '{CustomerName}' not found. Similar names: {SimilarNames}",
                customerName, string.Join(", ", similarNames));
        }
        else
        {
            _logger.LogWarning("GetCustomerPricingLevelAsync: Customer '{CustomerName}' not found and no similar names found", customerName);
        }

        return string.Empty;
    }

    /// <summary>
    /// Get maintenance prices for a pricing level
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetMaintenancePricesAsync(string pricingLevel)
    {
        var qb = new QuickBaseConnector(_settings);
        var priceTable = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var query = new QBQuery
        {
            from = MaintenancePricingTableId,
            select = new List<int>
            {
                MaintenancePricingFields.LocationType,
                MaintenancePricingFields.ServiceType,
                MaintenancePricingFields.MaintenancePrice
            },
            where = $"{{{MaintenancePricingFields.ServiceType}.EX.'{EscapeQueryValue(pricingLevel)}'}}",
            options = new QBQueryOptions { top = 500 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var locationType = GetStringValue(obj, MaintenancePricingFields.LocationType);
                var price = GetDecimalValue(obj, MaintenancePricingFields.MaintenancePrice);

                if (!string.IsNullOrEmpty(locationType) && !priceTable.ContainsKey(locationType))
                {
                    priceTable[locationType] = price;
                }
            }
        }

        return priceTable;
    }

    /// <summary>
    /// Get maintenance prices as a list
    /// </summary>
    public async Task<List<MaintenancePriceData>> GetMaintenancePriceListAsync(string pricingLevel)
    {
        var prices = await GetMaintenancePricesAsync(pricingLevel);
        return prices.Select(p => new MaintenancePriceData
        {
            LocationType = p.Key,
            ServiceType = pricingLevel,
            MaintenancePrice = p.Value
        }).ToList();
    }

    /// <summary>
    /// Get fixture locations for a customer
    /// </summary>
    public async Task<List<FixtureLocationData>> GetFixtureLocationsAsync(
        string customerName,
        string? divisionId = null)
    {
        var qb = new QuickBaseConnector(_settings);
        var locations = new List<FixtureLocationData>();

        // Build where clause
        string whereClause;
        if (!string.IsNullOrEmpty(divisionId))
        {
            whereClause = $"{{{LocationFields.CustomerName}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{LocationFields.DivisionId}.EX.'{divisionId}'}}AND{{{LocationFields.FixtureQuantity}.GT.'0'}}";
        }
        else
        {
            whereClause = $"{{{LocationFields.CustomerName}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{LocationFields.FixtureQuantity}.XEX.'0'}}AND{{{LocationFields.FixtureType}.XEX.''}}";
        }

        var query = new QBQuery
        {
            from = LocationsTableId,
            select = new List<int>
            {
                LocationFields.RecordId,
                LocationFields.CustomerName,
                LocationFields.StreetLightNumber,
                LocationFields.FixtureType,
                LocationFields.FixtureQuantity
            },
            where = whereClause,
            options = new QBQueryOptions { top = 10000 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;

                var quantityStr = GetStringValue(obj, LocationFields.FixtureQuantity);
                int quantity = 1;
                if (!string.IsNullOrEmpty(quantityStr) && int.TryParse(quantityStr, out var parsedQty))
                {
                    quantity = parsedQty > 0 ? parsedQty : 1;
                }

                locations.Add(new FixtureLocationData
                {
                    RecordId = GetStringValue(obj, LocationFields.RecordId),
                    CustomerName = GetStringValue(obj, LocationFields.CustomerName),
                    StreetLightNumber = GetStringValue(obj, LocationFields.StreetLightNumber),
                    FixtureType = GetStringValue(obj, LocationFields.FixtureType),
                    FixtureQuantity = quantity
                });
            }
        }

        return locations;
    }

    /// <summary>
    /// Get divisions for a customer
    /// </summary>
    public async Task<List<CustomerDivisionData>> GetCustomerDivisionsAsync(string customerName)
    {
        var qb = new QuickBaseConnector(_settings);
        var divisions = new List<CustomerDivisionData>();

        var query = new QBQuery
        {
            from = DivisionsTableId,
            select = new List<int>
            {
                DivisionFields.RecordId,
                DivisionFields.CustomerLink,
                DivisionFields.DivisionName
            },
            where = $"{{{DivisionFields.CustomerLink}.EX.'{EscapeQueryValue(customerName)}'}}",
            options = new QBQueryOptions { top = 100 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                divisions.Add(new CustomerDivisionData
                {
                    RecordId = GetStringValue(obj, DivisionFields.RecordId),
                    CustomerName = customerName,
                    DivisionName = GetStringValue(obj, DivisionFields.DivisionName)
                });
            }
        }

        return divisions;
    }

    /// <summary>
    /// Generate a PDF maintenance invoice from fixture billing data
    /// </summary>
    public byte[] GenerateFixtureBillingPdf(FixtureBillingResponse data)
    {
        using var doc = new Doc();
        // Use Chrome123 engine on Linux (ABCChrome), MSHtml on Windows
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            doc.HtmlOptions.Engine = EngineType.Chrome123;
        }
        else
        {
            doc.HtmlOptions.Engine = EngineType.MSHtml;
        }

        // Set up page
        doc.MediaBox.String = "A4";
        doc.Rect.String = doc.MediaBox.String;
        doc.Rect.Inset(30, 30);

        // Add logo
        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "logo.jpg");
        if (File.Exists(logoPath))
        {
            doc.Rect.String = "40 740 180 790";
            doc.AddImageFile(logoPath, 1);
        }

        // Build HTML content
        var html = BuildFixtureInvoiceHtml(data);

        // Set content area below logo
        doc.Rect.String = "40 50 555 720";

        // Add HTML content
        int theId = doc.AddImageHtml(html);

        // Handle multiple pages if needed
        while (doc.Chainable(theId))
        {
            doc.Page = doc.AddPage();
            doc.Rect.String = "40 50 555 780";
            theId = doc.AddImageToChain(theId);
        }

        // Flatten and return
        for (int i = 1; i <= doc.PageCount; i++)
        {
            doc.PageNumber = i;
            doc.Flatten();
        }

        return doc.GetData();
    }

    private string BuildFixtureInvoiceHtml(FixtureBillingResponse data)
    {
        var sb = new StringBuilder();

        // CSS styles
        sb.Append(@"
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset=""UTF-8"">
            <style>
                body { font-family: 'Liberation Sans', Arial, sans-serif; font-size: 11pt; margin: 0; padding: 0; }
                .title { font-size: 24pt; font-weight: bold; text-align: center; margin-bottom: 5px; text-decoration: underline; }
                .customer { font-size: 14pt; font-weight: bold; text-align: center; margin-bottom: 5px; }
                .period { font-size: 14pt; font-weight: bold; text-align: center; margin-bottom: 20px; }
                table { width: 100%; border-collapse: collapse; margin-top: 10px; }
                th { background-color: #FFFFFF; font-weight: bold; padding: 8px; border-bottom: 2px solid #000; text-align: left; }
                th.right { text-align: right; }
                td { padding: 8px; border-bottom: 1px solid #CCC; }
                td.right { text-align: right; }
                .total-row td { border-top: 2px solid #000; font-weight: bold; padding-top: 12px; }
            </style>
            </head>
            <body>");

        // Title
        sb.Append("<div class='title'>Maintenance Invoice</div>");

        // Customer name
        string customerDisplay = data.CustomerName;
        if (!string.IsNullOrEmpty(data.DivisionName))
        {
            customerDisplay += $" - {data.DivisionName}";
        }
        sb.Append($"<div class='customer'>{System.Net.WebUtility.HtmlEncode(customerDisplay)}</div>");

        // Billing period
        sb.Append($"<div class='period'>{System.Net.WebUtility.HtmlEncode(data.BillingPeriod)}</div>");

        // Table header
        sb.Append(@"
            <table>
                <tr>
                    <th style='width: 40%;'>Fixture Type</th>
                    <th class='right' style='width: 20%;'>Quantity</th>
                    <th class='right' style='width: 20%;'>Price</th>
                    <th class='right' style='width: 20%;'>Cost</th>
                </tr>");

        // Line items
        foreach (var location in data.Locations.OrderBy(l => l.FixtureType))
        {
            sb.Append($@"
                <tr>
                    <td>{System.Net.WebUtility.HtmlEncode(location.FixtureType)}</td>
                    <td class='right'>{location.FixtureQuantity:N0}</td>
                    <td class='right'>{location.Price:C}</td>
                    <td class='right'>{location.LineTotal:C}</td>
                </tr>");
        }

        // Total row
        sb.Append($@"
                <tr class='total-row'>
                    <td><strong>Total Due:</strong></td>
                    <td class='right'><strong>{data.TotalFixtures:N0}</strong></td>
                    <td></td>
                    <td class='right'><strong>{data.TotalAmount:C}</strong></td>
                </tr>
            </table>");

        sb.Append("</body></html>");

        return sb.ToString();
    }

    #region Ticket Billing Methods

    /// <summary>
    /// Get complete ticket billing data for a customer
    /// </summary>
    public async Task<TicketBillingResponse> GetTicketBillingDataAsync(TicketBillingRequest request)
    {
        _logger.LogInformation("Getting ticket billing data for customer: {Customer}, from {Start} to {End}",
            request.CustomerName, request.StartDate, request.EndDate);

        var qb = new QuickBaseConnector(_settings);

        // Get customer's pricing level
        var pricingLevel = await GetCustomerPricingLevelAsync(request.CustomerName);
        if (string.IsNullOrEmpty(pricingLevel))
        {
            pricingLevel = "A"; // Default pricing level
            _logger.LogWarning("No pricing level found for customer {Customer}, defaulting to 'A'", request.CustomerName);
        }

        // Get technicians list for name lookups
        var technicians = await GetTechniciansAsync();

        // Get tickets for the customer and date range
        var tickets = await GetTicketsAsync(request.CustomerName, request.StartDate, request.EndDate, request.DivisionId);

        if (tickets.Count == 0)
        {
            return new TicketBillingResponse
            {
                CustomerName = request.CustomerName,
                DivisionName = request.DivisionName,
                PricingLevel = pricingLevel,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                BillingPeriod = GetBillingPeriod(request.StartDate),
                Tickets = new List<TicketData>(),
                ErrorMessage = "No tickets found for this customer in the specified date range"
            };
        }

        _logger.LogInformation("Found {Count} tickets for customer {Customer}", tickets.Count, request.CustomerName);

        // For each ticket, get labor, materials, and equipment
        foreach (var ticket in tickets)
        {
            // Skip billing if ticket is marked as Job or Not Billable
            bool skipBilling = ticket.NotBillable || ticket.ServiceType == "Job";

            // Get materials first (needed to determine if labor/equipment should be billed)
            // Pass billableOverride to handle special pricing when sell price is 0
            ticket.MaterialItems = await GetMaterialsForTicketAsync(ticket.TicketId, pricingLevel, ticket.BillableOverride);

            // Determine if we should bill labor and equipment
            // BillableOverride forces billing regardless of materials
            // Otherwise, check based on materials (lump sum vs priced materials)
            bool shouldBillLaborEquipment = !skipBilling &&
                (ticket.BillableOverride || ShouldBillLaborAndEquipment(ticket.MaterialItems));

            // Get labor
            ticket.LaborItems = await GetLaborForTicketAsync(ticket.TicketId, request.CustomerName, technicians, shouldBillLaborEquipment);

            // Get equipment
            ticket.EquipmentItems = await GetEquipmentForTicketAsync(ticket.TicketId, request.CustomerName, shouldBillLaborEquipment);
        }

        // Build materials usage summary
        var materialsUsage = tickets
            .SelectMany(t => t.MaterialItems)
            .GroupBy(m => m.Description)
            .Select(g => new MaterialUsageSummary
            {
                Description = g.Key,
                TotalQuantity = g.Sum(m => m.Quantity),
                TotalCost = g.Sum(m => m.Cost)
            })
            .OrderByDescending(m => m.TotalQuantity)
            .ToList();

        // Build response
        var response = new TicketBillingResponse
        {
            CustomerName = request.CustomerName,
            DivisionName = request.DivisionName,
            PricingLevel = pricingLevel,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BillingPeriod = GetBillingPeriod(request.StartDate),
            Tickets = tickets,
            TicketCount = tickets.Count,
            TotalLaborHours = tickets.Sum(t => t.LaborItems.Sum(l => l.Hours)),
            TotalLaborCost = tickets.Sum(t => t.TicketLaborTotal),
            TotalMaterialsCost = tickets.Sum(t => t.TicketMaterialsTotal),
            TotalEquipmentHours = tickets.Sum(t => t.EquipmentItems.Sum(e => e.Hours)),
            TotalEquipmentCost = tickets.Sum(t => t.TicketEquipmentTotal),
            GrandTotal = tickets.Sum(t => t.TicketTotal),
            MaterialsUsageSummary = materialsUsage
        };

        _logger.LogInformation("Ticket billing data complete: {Count} tickets, Total: {Total:C}",
            tickets.Count, response.GrandTotal);

        return response;
    }

    /// <summary>
    /// Get tickets for a customer within a date range
    /// </summary>
    private async Task<List<TicketData>> GetTicketsAsync(
        string customerName,
        DateTime startDate,
        DateTime endDate,
        string? divisionId)
    {
        var qb = new QuickBaseConnector(_settings);
        var tickets = new List<TicketData>();

        // Build where clause (field IDs should not be quoted)
        string whereClause;
        if (!string.IsNullOrEmpty(divisionId))
        {
            whereClause = $"{{{TicketFields.CustomerName}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{TicketFields.CompletionDate}.GTE.'{startDate:yyyy-MM-dd}'}}AND{{{TicketFields.CompletionDate}.LTE.'{endDate:yyyy-MM-dd}'}}AND{{{TicketFields.DivisionId}.EX.'{divisionId}'}}AND{{{TicketFields.DoNotReport}.EX.'0'}}";
        }
        else
        {
            whereClause = $"{{{TicketFields.CustomerName}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{TicketFields.CompletionDate}.GTE.'{startDate:yyyy-MM-dd}'}}AND{{{TicketFields.CompletionDate}.LTE.'{endDate:yyyy-MM-dd}'}}AND{{{TicketFields.DoNotReport}.EX.'0'}}";
        }

        var query = new QBQuery
        {
            from = TicketsTableId,
            select = new List<int>
            {
                TicketFields.RecordId,
                TicketFields.TicketId,
                TicketFields.JobNumber,
                TicketFields.CustomerName,
                TicketFields.CallerName,
                TicketFields.CallerType,
                TicketFields.FixtureType,
                TicketFields.StreetLightNumber,
                TicketFields.AddressCalc,
                TicketFields.CrossStreet,
                TicketFields.DateTimeOpened,
                TicketFields.ServiceType,
                TicketFields.ProblemType,
                TicketFields.Details,
                TicketFields.StartDate,
                TicketFields.StartTime,
                TicketFields.CompletionDate,
                TicketFields.CompletionTime,
                TicketFields.CompletedBy,
                TicketFields.Analysis,
                TicketFields.NotBillable,
                TicketFields.BillableOverride
            },
            where = whereClause,
            options = new QBQueryOptions { top = 5000 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var ticket = new TicketData
                {
                    RecordId = GetStringValue(obj, TicketFields.RecordId),
                    TicketId = GetStringValue(obj, TicketFields.TicketId),
                    JobNumber = GetStringValue(obj, TicketFields.JobNumber),
                    CustomerName = GetStringValue(obj, TicketFields.CustomerName),
                    CallerName = GetStringValue(obj, TicketFields.CallerName),
                    CallerType = GetStringValue(obj, TicketFields.CallerType),
                    FixtureType = GetStringValue(obj, TicketFields.FixtureType),
                    StreetLightNumber = GetStringValue(obj, TicketFields.StreetLightNumber),
                    LocationAddress = GetStringValue(obj, TicketFields.AddressCalc),
                    LocationCrossStreet = GetStringValue(obj, TicketFields.CrossStreet),
                    ServiceType = GetStringValue(obj, TicketFields.ServiceType),
                    ProblemType = GetStringValue(obj, TicketFields.ProblemType),
                    Details = GetStringValue(obj, TicketFields.Details),
                    Analysis = GetStringValue(obj, TicketFields.Analysis),
                    Technician = GetStringValue(obj, TicketFields.CompletedBy),
                    DateTimeOpened = ParseQuickBaseDate(GetStringValue(obj, TicketFields.DateTimeOpened)),
                    StartDate = ParseQuickBaseDate(GetStringValue(obj, TicketFields.StartDate)),
                    StartTime = ParseQuickBaseTime(GetStringValue(obj, TicketFields.StartTime)),
                    CompletionDate = ParseQuickBaseDate(GetStringValue(obj, TicketFields.CompletionDate)),
                    CompletionTime = ParseQuickBaseTime(GetStringValue(obj, TicketFields.CompletionTime)),
                    NotBillable = GetStringValue(obj, TicketFields.NotBillable) == "1",
                    BillableOverride = GetStringValue(obj, TicketFields.BillableOverride) == "1"
                };

                tickets.Add(ticket);
            }
        }

        return tickets;
    }

    /// <summary>
    /// Get all tickets across all customers in a date range
    /// </summary>
    public async Task<List<TicketData>> GetAllTicketsAsync(DateTime startDate, DateTime endDate)
    {
        var qb = new QuickBaseConnector(_settings);
        var tickets = new List<TicketData>();

        // Query all tickets in date range (no customer filter)
        string whereClause = $"{{{TicketFields.CompletionDate}.GTE.'{startDate:yyyy-MM-dd}'}}AND{{{TicketFields.CompletionDate}.LTE.'{endDate:yyyy-MM-dd}'}}AND{{{TicketFields.DoNotReport}.EX.'0'}}";

        var query = new QBQuery
        {
            from = TicketsTableId,
            select = new List<int>
            {
                TicketFields.RecordId,
                TicketFields.TicketId,
                TicketFields.JobNumber,
                TicketFields.CustomerName,
                TicketFields.CallerName,
                TicketFields.CallerType,
                TicketFields.FixtureType,
                TicketFields.StreetLightNumber,
                TicketFields.AddressCalc,
                TicketFields.CrossStreet,
                TicketFields.DateTimeOpened,
                TicketFields.ServiceType,
                TicketFields.ProblemType,
                TicketFields.Details,
                TicketFields.StartDate,
                TicketFields.StartTime,
                TicketFields.CompletionDate,
                TicketFields.CompletionTime,
                TicketFields.CompletedBy,
                TicketFields.Analysis,
                TicketFields.NotBillable,
                TicketFields.BillableOverride
            },
            where = whereClause,
            sortBy = new List<QBFieldSet>
            {
                new QBFieldSet { fieldId = TicketFields.CustomerName, order = "ASC" },
                new QBFieldSet { fieldId = TicketFields.CompletionDate, order = "ASC" }
            },
            options = new QBQueryOptions { top = 10000 }
        };

        _logger.LogInformation("Querying all tickets from {Start} to {End}", startDate, endDate);

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var ticket = new TicketData
                {
                    RecordId = GetStringValue(obj, TicketFields.RecordId),
                    TicketId = GetStringValue(obj, TicketFields.TicketId),
                    JobNumber = GetStringValue(obj, TicketFields.JobNumber),
                    CustomerName = GetStringValue(obj, TicketFields.CustomerName),
                    CallerName = GetStringValue(obj, TicketFields.CallerName),
                    CallerType = GetStringValue(obj, TicketFields.CallerType),
                    FixtureType = GetStringValue(obj, TicketFields.FixtureType),
                    StreetLightNumber = GetStringValue(obj, TicketFields.StreetLightNumber),
                    LocationAddress = GetStringValue(obj, TicketFields.AddressCalc),
                    LocationCrossStreet = GetStringValue(obj, TicketFields.CrossStreet),
                    ServiceType = GetStringValue(obj, TicketFields.ServiceType),
                    ProblemType = GetStringValue(obj, TicketFields.ProblemType),
                    Details = GetStringValue(obj, TicketFields.Details),
                    Analysis = GetStringValue(obj, TicketFields.Analysis),
                    Technician = GetStringValue(obj, TicketFields.CompletedBy),
                    DateTimeOpened = ParseQuickBaseDate(GetStringValue(obj, TicketFields.DateTimeOpened)),
                    StartDate = ParseQuickBaseDate(GetStringValue(obj, TicketFields.StartDate)),
                    StartTime = ParseQuickBaseTime(GetStringValue(obj, TicketFields.StartTime)),
                    CompletionDate = ParseQuickBaseDate(GetStringValue(obj, TicketFields.CompletionDate)),
                    CompletionTime = ParseQuickBaseTime(GetStringValue(obj, TicketFields.CompletionTime)),
                    NotBillable = GetStringValue(obj, TicketFields.NotBillable) == "1",
                    BillableOverride = GetStringValue(obj, TicketFields.BillableOverride) == "1"
                };

                tickets.Add(ticket);
            }
        }

        _logger.LogInformation("Found {Count} tickets across all customers", tickets.Count);

        return tickets;
    }

    /// <summary>
    /// Get labor line items for a ticket
    /// </summary>
    private async Task<List<LaborLineItem>> GetLaborForTicketAsync(
        string ticketId,
        string customerName,
        List<TechnicianData> technicians,
        bool shouldBill)
    {
        var qb = new QuickBaseConnector(_settings);
        var laborItems = new List<LaborLineItem>();

        var query = new QBQuery
        {
            from = LaborLineItemsTableId,
            select = new List<int>
            {
                LaborFields.RecordId,
                LaborFields.TicketId,
                LaborFields.TeamMember,
                LaborFields.Hours,
                LaborFields.TypeOfHours,
                LaborFields.TypeOfLabor
            },
            where = $"{{{LaborFields.TicketId}.EX.'{EscapeQueryValue(ticketId)}'}}",
            options = new QBQueryOptions { top = 100 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                // TeamMember is a user field - extract name from the object
                var teamMemberName = GetUserNameValue(obj, LaborFields.TeamMember);
                var typeOfHours = GetStringValue(obj, LaborFields.TypeOfHours);
                var typeOfLabor = GetStringValue(obj, LaborFields.TypeOfLabor);

                _logger.LogInformation("Labor item: TicketId={TicketId}, TypeOfLabor={TypeOfLabor}, TypeOfHours={TypeOfHours}, Hours={Hours}",
                    ticketId, typeOfLabor, typeOfHours, GetDecimalValue(obj, LaborFields.Hours));

                var labor = new LaborLineItem
                {
                    TicketId = ticketId,
                    Technician = teamMemberName,
                    Hours = GetDecimalValue(obj, LaborFields.Hours),
                    TypeOfHours = typeOfHours,
                    TypeOfLabor = typeOfLabor,
                    Rate = shouldBill ? await GetLaborRateAsync(customerName, typeOfLabor, typeOfHours) : 0
                };

                laborItems.Add(labor);
            }
        }

        return laborItems;
    }

    /// <summary>
    /// Get material line items for a ticket
    /// </summary>
    private async Task<List<MaterialLineItem>> GetMaterialsForTicketAsync(string ticketId, string pricingLevel, bool billableOverride = false)
    {
        var qb = new QuickBaseConnector(_settings);
        var materialItems = new List<MaterialLineItem>();

        var query = new QBQuery
        {
            from = MaterialLineItemsTableId,
            select = new List<int>
            {
                MaterialFields.RecordId,
                MaterialFields.RelatedTicket,
                MaterialFields.ItemId,
                MaterialFields.ItemDescription,
                MaterialFields.Quantity,
                MaterialFields.UnitOfMeasure,
                MaterialFields.NonInventory,
                MaterialFields.MaterialDescriptionCalc,
                MaterialFields.NonInventorySalePrice,
                MaterialFields.ItemIdListPrice
            },
            where = $"{{{MaterialFields.RelatedTicket}.EX.'{EscapeQueryValue(ticketId)}'}}",
            options = new QBQueryOptions { top = 100 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var isNonInventory = GetStringValue(obj, MaterialFields.NonInventory) == "1" ;
               
                //this is because sometimes quickbase returns a bool instead of a bit.  no idea why.
                if (GetStringValue(obj, MaterialFields.NonInventory) == "True") isNonInventory = true;

                var itemId = GetStringValue(obj, MaterialFields.ItemId);

                var material = new MaterialLineItem
                {
                    TicketId = ticketId,
                    ItemId = itemId,
                    Description = isNonInventory
                        ? GetStringValue(obj, MaterialFields.MaterialDescriptionCalc)
                        : GetStringValue(obj, MaterialFields.ItemDescription),
                    Quantity = GetDecimalValue(obj, MaterialFields.Quantity),
                    UnitOfMeasure = GetStringValue(obj, MaterialFields.UnitOfMeasure)
                };

                // Get price
                if (isNonInventory)
                {
                    material.Price = GetDecimalValue(obj, MaterialFields.NonInventorySalePrice);
                }
                else
                {
                    var listPrice = GetDecimalValue(obj, MaterialFields.ItemIdListPrice);
                    var (sellPrice, isLumpSum) = await GetMaterialPriceAsync(itemId, pricingLevel, listPrice, billableOverride);
                    material.Price = sellPrice;
                    material.IsLumpSum = isLumpSum;
                }

                materialItems.Add(material);
            }
        }

        return materialItems;
    }

    /// <summary>
    /// Get equipment line items for a ticket
    /// </summary>
    private async Task<List<EquipmentLineItem>> GetEquipmentForTicketAsync(
        string ticketId,
        string customerName,
        bool shouldBill)
    {
        var qb = new QuickBaseConnector(_settings);
        var equipmentItems = new List<EquipmentLineItem>();

        var query = new QBQuery
        {
            from = EquipmentLineItemsTableId,
            select = new List<int>
            {
                EquipmentFields.RecordId,
                EquipmentFields.RelatedTicket,
                EquipmentFields.Equipment,
                EquipmentFields.Hours
            },
            where = $"{{{EquipmentFields.RelatedTicket}.EX.'{EscapeQueryValue(ticketId)}'}}",
            options = new QBQueryOptions { top = 100 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var equipmentName = GetStringValue(obj, EquipmentFields.Equipment);

                var equipment = new EquipmentLineItem
                {
                    TicketId = ticketId,
                    Equipment = equipmentName,
                    Hours = GetDecimalValue(obj, EquipmentFields.Hours),
                    Rate = shouldBill ? await GetEquipmentRateAsync(customerName, equipmentName) : 0
                };

                equipmentItems.Add(equipment);
            }
        }

        return equipmentItems;
    }

    /// <summary>
    /// Get labor rate for a customer
    /// </summary>
    private async Task<decimal> GetLaborRateAsync(string customerName, string typeOfLabor, string typeOfHours)
    {
        var qb = new QuickBaseConnector(_settings);

        var query = new QBQuery
        {
            from = LaborPricingTableId,
            select = new List<int>
            {
                LaborPricingFields.Customer,
                LaborPricingFields.TypeOfLabor,
                LaborPricingFields.TypeOfHours,
                LaborPricingFields.LaborPrice
            },
            where = $"{{{LaborPricingFields.Customer}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{LaborPricingFields.TypeOfLabor}.EX.'{EscapeQueryValue(typeOfLabor)}'}}AND{{{LaborPricingFields.TypeOfHours}.EX.'{EscapeQueryValue(typeOfHours)}'}}",
            options = new QBQueryOptions { top = 1 }
        };

        _logger.LogInformation("GetLaborRateAsync: Customer={Customer}, TypeOfLabor={TypeOfLabor}, TypeOfHours={TypeOfHours}",
            customerName, typeOfLabor, typeOfHours);

        var result = await qb.Query(query);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];
            var rate = GetDecimalValue(obj, LaborPricingFields.LaborPrice);
            _logger.LogInformation("GetLaborRateAsync: Found rate {Rate} for {Customer}/{TypeOfLabor}/{TypeOfHours}",
                rate, customerName, typeOfLabor, typeOfHours);
            return rate;
        }

        _logger.LogWarning("GetLaborRateAsync: No labor pricing found for {Customer}/{TypeOfLabor}/{TypeOfHours}",
            customerName, typeOfLabor, typeOfHours);
        return 0;
    }

    /// <summary>
    /// Get material price and lump sum flag
    /// </summary>
    private async Task<(decimal price, bool isLumpSum)> GetMaterialPriceAsync(string itemId, string pricingLevel, decimal listPrice, bool billableOverride = false)
    {
        var qb = new QuickBaseConnector(_settings);

        var query = new QBQuery
        {
            from = MaterialPricingTableId,
            select = new List<int>
            {
                MaterialPricingFields.SellPrice,
                MaterialPricingFields.LumpSum
            },
            where = $"{{{MaterialPricingFields.ItemId}.EX.'{EscapeQueryValue(itemId)}'}}AND{{{MaterialPricingFields.PricingGroup}.EX.'{EscapeQueryValue(pricingLevel)}'}}",
            options = new QBQueryOptions { top = 1 }
        };

        _logger.LogInformation("GetMaterialPriceAsync: ItemId={ItemId}, PricingLevel={PricingLevel}, ListPrice={ListPrice}",
            itemId, pricingLevel, listPrice);

        var result = await qb.Query(query);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];
            var sellPrice = GetDecimalValue(obj, MaterialPricingFields.SellPrice);
            var isLumpSum = GetStringValue(obj, MaterialPricingFields.LumpSum) == "1";

            // If sell price is 0, fall back to list price (legacy app behavior)
            decimal finalPrice = sellPrice;
            if (sellPrice == 0 && listPrice > 0)
            {
                finalPrice = listPrice;
                _logger.LogInformation("GetMaterialPriceAsync: SellPrice is 0, using ListPrice {ListPrice} for ItemId={ItemId}",
                    listPrice, itemId);
            }

            _logger.LogInformation("GetMaterialPriceAsync: Found price {Price}, IsLumpSum={IsLumpSum} for ItemId={ItemId}",
                finalPrice, isLumpSum, itemId);
            return (finalPrice, isLumpSum);
        }

        _logger.LogWarning("GetMaterialPriceAsync: No pricing record found for ItemId={ItemId}, PricingLevel={PricingLevel}, using ListPrice={ListPrice}",
            itemId, pricingLevel, listPrice);
        // No pricing record found - use list price
        return (listPrice, false);
    }

    /// <summary>
    /// Get equipment rate for a customer
    /// </summary>
    private async Task<decimal> GetEquipmentRateAsync(string customerName, string equipmentName)
    {
        var qb = new QuickBaseConnector(_settings);

        var query = new QBQuery
        {
            from = EquipmentPricingTableId,
            select = new List<int>
            {
                EquipmentPricingFields.EquipmentRate
            },
            where = $"{{{EquipmentPricingFields.Customer}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{EquipmentPricingFields.Equipment}.EX.'{EscapeQueryValue(equipmentName)}'}}",
            options = new QBQueryOptions { top = 1 }
        };

        _logger.LogInformation("GetEquipmentRateAsync: Customer={Customer}, Equipment={Equipment}",
            customerName, equipmentName);

        var result = await qb.Query(query);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];
            var rate = GetDecimalValue(obj, EquipmentPricingFields.EquipmentRate);
            _logger.LogInformation("GetEquipmentRateAsync: Found rate {Rate} for {Customer}/{Equipment}",
                rate, customerName, equipmentName);
            return rate;
        }

        _logger.LogWarning("GetEquipmentRateAsync: No equipment pricing found for {Customer}/{Equipment}",
            customerName, equipmentName);
        return 0;
    }

    /// <summary>
    /// Get all technicians
    /// </summary>
    private async Task<List<TechnicianData>> GetTechniciansAsync()
    {
        var qb = new QuickBaseConnector(_settings);
        var technicians = new List<TechnicianData>();

        var query = new QBQuery
        {
            from = TeamMembersTableId,
            select = new List<int>
            {
                TeamMemberFields.RecordId,
                TeamMemberFields.UserId,
                TeamMemberFields.FirstName,
                TeamMemberFields.LastName
            },
            options = new QBQueryOptions { top = 500 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var firstName = GetStringValue(obj, TeamMemberFields.FirstName);
                var lastName = GetStringValue(obj, TeamMemberFields.LastName);

                technicians.Add(new TechnicianData
                {
                    TechId = GetStringValue(obj, TeamMemberFields.RecordId),
                    Name = $"{firstName} {lastName}".Trim()
                });
            }
        }

        return technicians;
    }

    /// <summary>
    /// Get technician name by ID
    /// </summary>
    private string GetTechnicianName(List<TechnicianData> technicians, string techId)
    {
        if (string.IsNullOrEmpty(techId)) return "Unknown";
        var tech = technicians.FirstOrDefault(t => t.TechId == techId);
        return tech?.Name ?? "Unknown";
    }

    /// <summary>
    /// Determine if labor and equipment should be billed based on materials
    /// </summary>
    private bool ShouldBillLaborAndEquipment(List<MaterialLineItem> materials)
    {
        // If any material is marked as lump sum, don't bill labor/equipment
        // (lump sum means labor/equipment is included in the material price)
        if (materials.Any(m => m.IsLumpSum))
            return false;

        // Otherwise, always bill labor/equipment
        // The legacy app bills unless there's a lump sum material
        return true;
    }

    /// <summary>
    /// Generate a PDF ticket invoice from ticket billing data
    /// </summary>
    public byte[] GenerateTicketBillingPdf(TicketBillingResponse data)
    {
        using var doc = new Doc();
        // Use Chrome123 engine on Linux (ABCChrome), MSHtml on Windows
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            doc.HtmlOptions.Engine = EngineType.Chrome123;
        }
        else
        {
            doc.HtmlOptions.Engine = EngineType.MSHtml;
        }

        // Set up page
        doc.MediaBox.String = "A4";
        doc.Rect.String = doc.MediaBox.String;
        doc.Rect.Inset(30, 30);

        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "logo.jpg");

        // First page - Title
        if (File.Exists(logoPath))
        {
            doc.Rect.String = "40 740 180 790";
            doc.AddImageFile(logoPath, 1);
        }

        // Build each ticket page
        bool isFirstPage = true;
        foreach (var ticket in data.Tickets)
        {
            if (!isFirstPage)
            {
                doc.Page = doc.AddPage();
            }

            // Add logo on each page
            if (File.Exists(logoPath))
            {
                doc.Rect.String = "40 740 180 790";
                doc.AddImageFile(logoPath, 1);
            }

            // Build ticket HTML
            var ticketHtml = BuildTicketPageHtml(ticket, data.CustomerName, data.BillingPeriod, isFirstPage);

            // Set content area
            doc.Rect.String = isFirstPage ? "40 50 555 720" : "40 50 555 720";

            // Add HTML content
            int theId = doc.AddImageHtml(ticketHtml);

            // Handle overflow to additional pages
            while (doc.Chainable(theId))
            {
                doc.Page = doc.AddPage();
                doc.Rect.String = "40 50 555 780";
                theId = doc.AddImageToChain(theId);
            }

            isFirstPage = false;
        }

        // Add summary page
        doc.Page = doc.AddPage();
        if (File.Exists(logoPath))
        {
            doc.Rect.String = "40 740 180 790";
            doc.AddImageFile(logoPath, 1);
        }

        var summaryHtml = BuildSummaryPageHtml(data);
        doc.Rect.String = "40 50 555 720";
        doc.AddImageHtml(summaryHtml);

        // Add page numbers to each page
        int totalPages = doc.PageCount;
        for (int i = 1; i <= totalPages; i++)
        {
            doc.PageNumber = i;
            doc.Rect.String = "40 20 555 40";
            doc.HPos = 0.5;
            doc.VPos = 0.5;
            doc.FontSize = 9;
            doc.AddText($"Page {i} of {totalPages}");
        }

        // Flatten and return
        for (int i = 1; i <= doc.PageCount; i++)
        {
            doc.PageNumber = i;
            doc.Flatten();
        }

        return doc.GetData();
    }

    private string BuildTicketPageHtml(TicketData ticket, string customerName, string billingPeriod, bool showTitle)
    {
        var sb = new StringBuilder();

        sb.Append(@"
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset=""UTF-8"">
            <style>
                body { font-family: 'Liberation Sans', Arial, sans-serif; font-size: 10pt; margin: 0; padding: 0; }
                .title { font-size: 20pt; font-weight: bold; text-align: center; margin-bottom: 5px; text-decoration: underline; }
                .customer { font-size: 12pt; font-weight: bold; text-align: center; margin-bottom: 3px; }
                .period { font-size: 12pt; font-weight: bold; text-align: center; margin-bottom: 15px; }
                .ticket-header { font-weight: bold; margin-bottom: 5px; border-bottom: 2px solid #000; padding-bottom: 3px; }
                .info-row { margin-bottom: 3px; }
                .info-label { font-weight: bold; display: inline-block; width: 120px; }
                .section-title { font-weight: bold; text-decoration: underline; margin-top: 10px; margin-bottom: 5px; }
                table { width: 100%; border-collapse: collapse; margin-top: 5px; margin-bottom: 10px; font-size: 9pt; }
                th { font-weight: bold; padding: 4px; border-bottom: 1px solid #000; text-align: left; }
                th.right { text-align: right; }
                td { padding: 4px; }
                td.right { text-align: right; }
                .total-row { border-top: 1px solid #000; font-weight: bold; }
                .analysis { margin-top: 10px; }
            </style>
            </head>
            <body>");

        if (showTitle)
        {
            sb.Append("<div class='title'>Ticket Invoice</div>");
            sb.Append($"<div class='customer'>{System.Net.WebUtility.HtmlEncode(customerName)}</div>");
            sb.Append($"<div class='period'>{System.Net.WebUtility.HtmlEncode(billingPeriod)}</div>");
        }

        // Ticket header
        sb.Append($"<div class='ticket-header'>Street Light #: {System.Net.WebUtility.HtmlEncode(ticket.StreetLightNumber)}<br/>Ticket #: {System.Net.WebUtility.HtmlEncode(ticket.TicketId)}</div>");

        // Info rows - two columns
        sb.Append("<table style='border:none;'><tr><td style='width:50%;vertical-align:top;border:none;padding:0;'>");
        sb.Append($"<div class='info-row'><span class='info-label'>Address:</span> {System.Net.WebUtility.HtmlEncode(ticket.LocationAddress)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Job #:</span> {System.Net.WebUtility.HtmlEncode(ticket.JobNumber)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Caller Name:</span> {System.Net.WebUtility.HtmlEncode(ticket.CallerName)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Street Light #:</span> {System.Net.WebUtility.HtmlEncode(ticket.StreetLightNumber)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Start Date:</span> {ticket.StartDate?.ToShortDateString() ?? ""}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Start Time:</span> {System.Net.WebUtility.HtmlEncode(ticket.StartTime)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Completion Date:</span> {ticket.CompletionDate?.ToShortDateString() ?? ""}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Completion Time:</span> {System.Net.WebUtility.HtmlEncode(ticket.CompletionTime)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Problem Type:</span> {System.Net.WebUtility.HtmlEncode(ticket.ProblemType)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Technician:</span> {System.Net.WebUtility.HtmlEncode(ticket.Technician)}</div>");
        sb.Append("</td><td style='width:50%;vertical-align:top;border:none;padding:0;'>");
        sb.Append($"<div class='info-row'><span class='info-label'>Cross Street:</span> {System.Net.WebUtility.HtmlEncode(ticket.LocationCrossStreet)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Date Opened:</span> {ticket.DateTimeOpened?.ToShortDateString() ?? ""}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>CallerType:</span> {System.Net.WebUtility.HtmlEncode(ticket.CallerType)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Fixture Type:</span> {System.Net.WebUtility.HtmlEncode(ticket.FixtureType)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Service Type:</span> {System.Net.WebUtility.HtmlEncode(ticket.ServiceType)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Details:</span> {System.Net.WebUtility.HtmlEncode(ticket.Details)}</div>");
        sb.Append("</td></tr></table>");

        // Analysis
        sb.Append($"<div class='analysis'><span class='info-label' style='text-decoration:underline;'>Analysis:</span> {System.Net.WebUtility.HtmlEncode(ticket.Analysis)}</div>");

        // Labor table
        if (ticket.LaborItems.Count > 0)
        {
            sb.Append("<table><tr><th style='text-decoration:underline;'>Technician</th><th style='text-decoration:underline;'>Type of Hours</th><th style='text-decoration:underline;'>Type of Labor</th><th class='right' style='text-decoration:underline;'>Hours</th><th class='right' style='text-decoration:underline;'>Rate</th><th class='right' style='text-decoration:underline;'>Cost</th></tr>");
            foreach (var labor in ticket.LaborItems)
            {
                sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(labor.Technician)}</td><td>{System.Net.WebUtility.HtmlEncode(labor.TypeOfHours)}</td><td>{System.Net.WebUtility.HtmlEncode(labor.TypeOfLabor)}</td><td class='right'>{labor.Hours:N2}</td><td class='right'>{labor.Rate:C}</td><td class='right'>{labor.Cost:C}</td></tr>");
            }
            // Add subtotals by Type of Hours
            var laborByType = ticket.LaborItems.GroupBy(l => l.TypeOfHours);
            foreach (var group in laborByType)
            {
                sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(group.Key)} :</td><td></td><td></td><td class='right'>{group.Sum(l => l.Hours):N2}</td><td></td><td class='right'>{group.Sum(l => l.Cost):C}</td></tr>");
            }
            sb.Append($"<tr class='total-row'><td><strong>Total Technician Hours:</strong></td><td></td><td></td><td class='right'><strong>{ticket.LaborItems.Sum(l => l.Hours):N2}</strong></td><td></td><td class='right'><strong>{ticket.TicketLaborTotal:C}</strong></td></tr>");
            sb.Append("</table>");
        }

        // Materials table
        if (ticket.MaterialItems.Count > 0)
        {
            sb.Append("<table><tr><th style='text-decoration:underline;'>Materials</th><th style='text-decoration:underline;'>Unit of Measure</th><th class='right' style='text-decoration:underline;'>Quantity</th><th class='right' style='text-decoration:underline;'>Price</th><th class='right' style='text-decoration:underline;'>Cost</th></tr>");
            foreach (var material in ticket.MaterialItems)
            {
                sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(material.ItemId)} - {System.Net.WebUtility.HtmlEncode(material.Description)}</td><td>{System.Net.WebUtility.HtmlEncode(material.UnitOfMeasure)}</td><td class='right'>{material.Quantity:N2}</td><td class='right'>{material.Price:N2}</td><td class='right'>{material.Cost:C}</td></tr>");
            }
            sb.Append($"<tr class='total-row'><td colspan='4'><strong>Total Materials Cost:</strong></td><td class='right'><strong>{ticket.TicketMaterialsTotal:C}</strong></td></tr>");
            sb.Append("</table>");
        }

        // Equipment table
        if (ticket.EquipmentItems.Count > 0)
        {
            sb.Append("<table><tr><th style='text-decoration:underline;'>Equipment</th><th class='right' style='text-decoration:underline;'>Hours</th><th class='right' style='text-decoration:underline;'>Rate</th><th class='right' style='text-decoration:underline;'>Cost</th></tr>");
            foreach (var equipment in ticket.EquipmentItems)
            {
                sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(equipment.Equipment)}</td><td class='right'>{equipment.Hours:N2}</td><td class='right'>{equipment.Rate:C}</td><td class='right'>{equipment.Cost:C}</td></tr>");
            }
            sb.Append($"<tr class='total-row'><td><strong>Total Equipment Hours:</strong></td><td class='right'><strong>{ticket.EquipmentItems.Sum(e => e.Hours):N2}</strong></td><td></td><td class='right'><strong>{ticket.TicketEquipmentTotal:C}</strong></td></tr>");
            sb.Append("</table>");
        }

        // Ticket Total
        sb.Append($"<div style='margin-top: 15px; text-align: right; font-size: 11pt;'><strong>Total: {ticket.TicketTotal:C}</strong></div>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private string BuildSummaryPageHtml(TicketBillingResponse data)
    {
        var sb = new StringBuilder();

        sb.Append(@"
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset=""UTF-8"">
            <style>
                body { font-family: 'Liberation Sans', Arial, sans-serif; font-size: 11pt; margin: 0; padding: 0; }
                .summary-title { font-size: 14pt; font-weight: bold; margin-bottom: 15px; }
                .total-row { margin-bottom: 8px; }
                .total-label { display: inline-block; width: 250px; }
                .total-value { font-weight: bold; }
                .grand-total { font-size: 14pt; border-top: 2px solid #000; padding-top: 10px; margin-top: 15px; }
                table { width: 100%; border-collapse: collapse; margin-top: 10px; }
                th { font-weight: bold; padding: 5px; border-bottom: 1px solid #000; text-align: left; }
                th.right { text-align: right; }
                td { padding: 5px; }
                td.right { text-align: right; }
            </style>
            </head>
            <body>");

        // Cost summary
        sb.Append($"<div class='total-row'><span class='total-label'>Total Labor Costs:</span> <span class='total-value'>{data.TotalLaborCost:C}</span></div>");
        sb.Append($"<div class='total-row'><span class='total-label'>Total Equipment Costs:</span> <span class='total-value'>{data.TotalEquipmentCost:C}</span></div>");
        sb.Append($"<div class='total-row'><span class='total-label'>Total Materials Costs:</span> <span class='total-value'>{data.TotalMaterialsCost:C}</span></div>");
        sb.Append($"<div class='grand-total'><span class='total-label'></span> <span class='total-value'>{data.GrandTotal:C}</span></div>");

        // Usage summary
        if (data.MaterialsUsageSummary.Count > 0)
        {
            sb.Append("<div class='summary-title' style='margin-top: 30px; text-decoration: underline;'>Usage Summary:</div>");
            sb.Append("<table><tr><th>Material</th><th class='right'>Quantity</th><th class='right'>Cost</th></tr>");
            foreach (var material in data.MaterialsUsageSummary)
            {
                sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(material.Description)}</td><td class='right'>{material.TotalQuantity:N2}</td><td class='right'>{material.TotalCost:C}</td></tr>");
            }
            sb.Append("</table>");
        }

        // Hours summary
        sb.Append($"<div class='total-row' style='margin-top: 20px;'><span class='total-label'>Total Labor Hours:</span> <span class='total-value'>{data.TotalLaborHours:N2}</span></div>");
        sb.Append($"<div class='total-row'><span class='total-label'>Total Equipment Hours:</span> <span class='total-value'>{data.TotalEquipmentHours:N2}</span></div>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    #endregion

    #region Helper Methods

    private static string GetStringValue(JObject obj, int fieldId)
    {
        var field = obj.GetValue(fieldId.ToString());
        if (field == null) return string.Empty;

        var valueToken = field["value"];
        return valueToken?.ToString() ?? string.Empty;
    }

    private static string GetUserNameValue(JObject obj, int fieldId)
    {
        var field = obj.GetValue(fieldId.ToString());
        if (field == null) return string.Empty;

        var valueToken = field["value"];
        if (valueToken == null) return string.Empty;

        // User fields return an object with {email, id, name}
        if (valueToken is JObject userObj)
        {
            var name = userObj["name"]?.ToString();
            if (!string.IsNullOrEmpty(name)) return name;

            var email = userObj["email"]?.ToString();
            if (!string.IsNullOrEmpty(email)) return email;
        }

        return valueToken.ToString();
    }

    private static decimal GetDecimalValue(JObject obj, int fieldId)
    {
        var strValue = GetStringValue(obj, fieldId);
        if (string.IsNullOrEmpty(strValue)) return 0;

        return decimal.TryParse(strValue, out var result) ? result : 0;
    }

    private static string EscapeQueryValue(string value)
    {
        // Escape single quotes for QuickBase query syntax
        return value.Replace("'", "\\'");
    }

    private static string GetBillingPeriod(DateTime date)
    {
        return date.ToString("MMMM yyyy");
    }

    private static DateTime? ParseQuickBaseDate(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        // QuickBase returns dates as milliseconds since epoch or ISO format
        if (long.TryParse(value, out var milliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime;
        }

        if (DateTime.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    private static string ParseQuickBaseTime(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        // QuickBase may return time as milliseconds since midnight or as a time string
        if (long.TryParse(value, out var milliseconds))
        {
            var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            var dateTime = DateTime.Today.Add(timeSpan);
            return dateTime.ToString("h:mm tt");
        }

        // Try to parse as a time string and reformat with AM/PM
        if (DateTime.TryParse(value, out var parsedTime))
        {
            return parsedTime.ToString("h:mm tt");
        }

        return value;
    }

    #endregion
}
