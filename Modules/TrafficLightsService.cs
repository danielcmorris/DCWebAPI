using DCElectricWebAPI.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Text;
using WebSupergoo.ABCpdf13;
using static DCElectricWebAPI.Models.QuickBaseLibrary;

namespace DCElectricWebAPI.Modules;

public class TrafficLightsService
{
    private readonly IOptions<QuickBaseSettings> _settings;
    private readonly ILogger<TrafficLightsService> _logger;

    // QuickBase Table IDs for Traffic Signal app (bhrneweey)
    private const string CustomersTableId = "bhrnewemu";           // Customers table
    private const string TicketsTableId = "bhrnewena";             // Tickets table
    private const string LocationsTableId = "bhrnn3b2f";           // Locations table
    private const string TeamMembersTableId = "bhrnewene";         // Team Members table
    private const string LaborLineItemsTableId = "bhsskipr4";      // Labor Line Items table
    private const string LaborPricingTableId = "biyzvrrdq";        // Labor Pricing table
    private const string MaterialLineItemsTableId = "bhssiwmb4";   // Material Line Items table
    private const string MaterialPricingTableId = "biyzuxd3r";     // Material Pricing table
    private const string EquipmentLineItemsTableId = "bhssjuxqh";  // Equipment Line Items table
    private const string EquipmentPricingTableId = "bjakff65u";    // Equipment Pricing table
    private const string MaintenancePricingTableId = "bjakgf5mx";  // Maintenance Pricing Table
    private const string DivisionsTableId = "bmwexuk45";           // Customer Division Names table

    // Field IDs for Customers table (bhrnewemu)
    private static class CustomerFields
    {
        public const int RecordId = 3;
        public const int CustomerName = 6;            // PK
        public const int LMBilledSeparately = 62;     // L&M Billed Separately (checkbox)
        public const int HasBillingDivisions = 69;    // Customer has Billing Divisions? (checkbox)
    }

    // Field IDs for Tickets table (bhrnewena)
    private static class TicketFields
    {
        public const int RecordId = 3;
        public const int ServiceType = 7;             // Service Type
        public const int ProblemType = 8;             // Problem Type
        public const int Details = 10;                // Details (text-multi-line)
        public const int Analysis = 16;               // Analysis (text-multi-line)
        public const int CustomerName = 18;           // Customer Name
        public const int CallerType = 21;             // Caller Type
        public const int DateTimeOpened = 25;         // Date / Time Ticket Opened
        public const int TicketId = 27;               // Ticket ID (e.g., TS-000001)
        public const int StartDate = 43;              // Start Date
        public const int StartTime = 44;              // Start Time
        public const int CompletionDate = 45;         // Completion Date
        public const int CompletionTime = 46;         // Completion Time
        public const int CompletedBy = 56;            // Completed By
        public const int CallerName = 99;             // Caller Name
        public const int Location = 103;              // Location
        public const int LocationType = 105;          // Location Type
        public const int JobNumber = 119;             // Job #
        public const int ServiceCategory = 162;       // Service Category (Routine/Response/L&M)
        public const int NotBillable = 163;           // Not Billable (checkbox)
        public const int LMBilledSeparately = 173;    // L&M Billed Separately (checkbox - from customer)
        public const int AgencyId = 224;              // Location - Agency ID#
        public const int DoNotReport = 226;           // Do Not Report (checkbox)
        public const int DivisionId = 228;            // Location - Related Division
        public const int RemovePricing = 232;         // Remove Pricing (checkbox)
    }

    // Field IDs for Labor Line Items table (bhsskipr4)
    private static class LaborFields
    {
        public const int RecordId = 3;
        public const int Date = 6;
        public const int Hours = 7;
        public const int TypeOfHours = 8;
        public const int TeamMember = 9;              // User field
        public const int RelatedTicket = 10;          // Numeric FK to Tickets
        public const int TicketId = 11;               // Text lookup of Ticket ID
        public const int TypeOfLabor = 12;            // Type of Labor
    }

    // Field IDs for Labor Pricing table (biyzvrrdq)
    private static class LaborPricingFields
    {
        public const int RecordId = 3;
        public const int TypeOfLabor = 7;
        public const int TypeOfHours = 8;
        public const int LaborPrice = 9;
        public const int RelatedCustomer = 10;
    }

    // Field IDs for Material Line Items table (bhssiwmb4)
    private static class MaterialFields
    {
        public const int RecordId = 3;
        public const int Quantity = 6;
        public const int ItemDescription = 10;        // Item Description (lookup)
        public const int RelatedTicket = 12;          // Related Ticket (numeric FK)
        public const int TicketId = 13;               // Ticket ID (text lookup)
        public const int ItemId = 20;                 // Item ID
        public const int ItemIdListPrice = 26;        // Item ID - List Price (currency lookup)
        public const int NonInventory = 29;           // Non-Inventory Material (checkbox)
        public const int NonInventoryDescription = 30; // Non-Inventory Material Description
        public const int NonInventorySalePrice = 32;  // Non-Inventory Material SALE Price
        public const int MaterialDescriptionCalc = 33; // Material Description CALC
    }

    // Field IDs for Material Pricing table (biyzuxd3r)
    private static class MaterialPricingFields
    {
        public const int RecordId = 3;
        public const int SellPrice = 8;
        public const int RelatedCustomer = 9;
        public const int ItemId = 12;
        public const int LumpSum = 21;
        public const int ItemIdAndCustomerCalc = 22;  // Composite key
    }

    // Field IDs for Equipment Line Items table (bhssjuxqh)
    private static class EquipmentFields
    {
        public const int RecordId = 3;
        public const int Hours = 6;
        public const int Equipment = 9;               // Equipment (text lookup)
        public const int RelatedTicket = 10;          // Related Ticket (numeric FK)
        public const int TicketId = 11;               // Ticket ID (text lookup)
    }

    // Field IDs for Equipment Pricing table (bjakff65u)
    private static class EquipmentPricingFields
    {
        public const int RecordId = 3;
        public const int Equipment = 7;
        public const int Price = 9;
        public const int RelatedCustomer = 10;
    }

    // Field IDs for Maintenance Pricing table (bjakgf5mx)
    private static class MaintenancePricingFields
    {
        public const int RecordId = 3;
        public const int ServiceType = 6;
        public const int MaintenancePrice = 7;
        public const int LocationType = 9;
        public const int RelatedCustomer = 11;
    }

    // Field IDs for Customer Division Names table (bmwexuk45)
    private static class DivisionFields
    {
        public const int RecordId = 3;
        public const int DivisionName = 6;
        public const int RelatedCustomer = 7;
    }

    // Field IDs for Team Members table (bhrnewene)
    private static class TeamMemberFields
    {
        public const int RecordId = 3;
        public const int Name = 6;                    // Team Member name
        public const int Email = 10;                  // Email
    }

    public TrafficLightsService(
        IOptions<QuickBaseSettings> settings,
        ILogger<TrafficLightsService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    #region Customer Methods

    /// <summary>
    /// Get all Traffic Light customers
    /// </summary>
    public async Task<List<TrafficCustomerData>> GetCustomersAsync()
    {
        var qb = new QuickBaseConnector(_settings);
        var query = new QBQuery
        {
            from = CustomersTableId,
            select = new List<int>
            {
                CustomerFields.RecordId,
                CustomerFields.CustomerName,
                CustomerFields.LMBilledSeparately,
                CustomerFields.HasBillingDivisions
            },
            options = new QBQueryOptions { top = 500 }
        };

        var result = await qb.Query(query);
        var customers = new List<TrafficCustomerData>();

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                customers.Add(new TrafficCustomerData
                {
                    CustomerName = GetStringValue(obj, CustomerFields.CustomerName),
                    LMBilledSeparately = GetStringValue(obj, CustomerFields.LMBilledSeparately) == "1",
                    HasBillingDivisions = GetStringValue(obj, CustomerFields.HasBillingDivisions) == "1"
                });
            }
        }

        return customers.OrderBy(c => c.CustomerName).ToList();
    }

    /// <summary>
    /// Get divisions for a customer
    /// </summary>
    public async Task<List<TrafficDivisionData>> GetCustomerDivisionsAsync(string customerName)
    {
        var qb = new QuickBaseConnector(_settings);
        var divisions = new List<TrafficDivisionData>();

        var query = new QBQuery
        {
            from = DivisionsTableId,
            select = new List<int>
            {
                DivisionFields.RecordId,
                DivisionFields.RelatedCustomer,
                DivisionFields.DivisionName
            },
            where = $"{{{DivisionFields.RelatedCustomer}.EX.'{EscapeQueryValue(customerName)}'}}",
            options = new QBQueryOptions { top = 100 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                divisions.Add(new TrafficDivisionData
                {
                    RecordId = GetStringValue(obj, DivisionFields.RecordId),
                    CustomerName = customerName,
                    DivisionName = GetStringValue(obj, DivisionFields.DivisionName)
                });
            }
        }

        return divisions;
    }

    #endregion

    #region Ticket Billing Methods

    /// <summary>
    /// Get complete ticket billing data for a Traffic Light customer
    /// </summary>
    public async Task<TrafficTicketBillingResponse> GetTicketBillingDataAsync(TrafficTicketBillingRequest request)
    {
        _logger.LogInformation("Getting Traffic Light ticket billing data for customer: {Customer}, from {Start} to {End}",
            request.CustomerName, request.StartDate, request.EndDate);

        var qb = new QuickBaseConnector(_settings);

        // Get technicians list for name lookups
        var technicians = await GetTechniciansAsync();

        // Get tickets for the customer and date range
        var tickets = await GetTicketsAsync(request.CustomerName, request.StartDate, request.EndDate, request.DivisionId);

        if (tickets.Count == 0)
        {
            return new TrafficTicketBillingResponse
            {
                CustomerName = request.CustomerName,
                DivisionName = request.DivisionName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                BillingPeriod = GetBillingPeriod(request.StartDate),
                Tickets = new List<TrafficTicketData>(),
                ErrorMessage = "No tickets found for this customer in the specified date range"
            };
        }

        _logger.LogInformation("Found {Count} tickets for customer {Customer}", tickets.Count, request.CustomerName);

        // Process each ticket
        foreach (var ticket in tickets)
        {
            // Skip if L&M Billed Separately and Service Category is L&M
            if (ticket.LMBilledSeparately &&
                !string.IsNullOrEmpty(ticket.ServiceCategory) &&
                ticket.ServiceCategory.Equals("L&M", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping ticket {TicketId} - L&M Billed Separately", ticket.TicketId);
                continue;
            }

            // Check if this is a routine maintenance ticket
            if (ticket.IsRoutineMaintenance)
            {
                // Get flat maintenance fee
                ticket.MaintenanceFee = await GetMaintenanceFeeAsync(
                    request.CustomerName,
                    ticket.ServiceType,
                    ticket.LocationType);

                _logger.LogInformation("Ticket {TicketId} is Routine maintenance, fee: {Fee:C}",
                    ticket.TicketId, ticket.MaintenanceFee);
            }
            else
            {
                // Standard billing - get labor, materials, equipment
                bool skipBilling = ticket.NotBillable;

                // Get materials
                ticket.MaterialItems = await GetMaterialsForTicketAsync(ticket.TicketId, request.CustomerName);

                // Determine if we should bill labor and equipment
                bool shouldBillLaborEquipment = !skipBilling && ShouldBillLaborAndEquipment(ticket.MaterialItems);

                // Get labor
                ticket.LaborItems = await GetLaborForTicketAsync(
                    ticket.TicketId,
                    request.CustomerName,
                    technicians,
                    shouldBillLaborEquipment);

                // Get equipment
                ticket.EquipmentItems = await GetEquipmentForTicketAsync(
                    ticket.TicketId,
                    request.CustomerName,
                    shouldBillLaborEquipment);
            }
        }

        // Filter out skipped L&M tickets
        tickets = tickets.Where(t =>
            !(t.LMBilledSeparately &&
              !string.IsNullOrEmpty(t.ServiceCategory) &&
              t.ServiceCategory.Equals("L&M", StringComparison.OrdinalIgnoreCase))).ToList();

        // Sort tickets by LocationType (custom order), then Location, then TicketId (to match legacy report)
        // Custom LocationType order: Signalized Intersection first, then Other, then Caltrans
        Func<string, int> getLocationTypeSortOrder = (locationType) =>
        {
            if (string.IsNullOrEmpty(locationType)) return 99;
            if (locationType.Contains("Signalized", StringComparison.OrdinalIgnoreCase)) return 1;
            if (locationType.Contains("Other", StringComparison.OrdinalIgnoreCase)) return 2;
            if (locationType.Contains("Caltrans", StringComparison.OrdinalIgnoreCase)) return 3;
            return 50; // Unknown types sort in the middle
        };

        tickets = tickets
            .OrderBy(t => getLocationTypeSortOrder(t.LocationType))
            .ThenBy(t => t.Location)
            .ThenBy(t => t.TicketId)
            .ToList();

        // Build materials usage summary
        var materialsUsage = tickets
            .SelectMany(t => t.MaterialItems)
            .GroupBy(m => m.Description)
            .Select(g => new TrafficMaterialUsageSummary
            {
                Description = g.Key,
                TotalQuantity = g.Sum(m => m.Quantity),
                TotalCost = g.Sum(m => m.Cost)
            })
            .OrderByDescending(m => m.TotalQuantity)
            .ToList();

        // Build response
        var response = new TrafficTicketBillingResponse
        {
            CustomerName = request.CustomerName,
            DivisionName = request.DivisionName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            BillingPeriod = GetBillingPeriod(request.StartDate),
            Tickets = tickets,
            TicketCount = tickets.Count,
            TotalMaintenanceFees = tickets.Where(t => t.IsRoutineMaintenance).Sum(t => t.MaintenanceFee),
            TotalLaborHours = tickets.Sum(t => t.LaborItems.Sum(l => l.Hours)),
            TotalLaborCost = tickets.Sum(t => t.TicketLaborTotal),
            TotalMaterialsCost = tickets.Sum(t => t.TicketMaterialsTotal),
            TotalEquipmentHours = tickets.Sum(t => t.EquipmentItems.Sum(e => e.Hours)),
            TotalEquipmentCost = tickets.Sum(t => t.TicketEquipmentTotal),
            GrandTotal = tickets.Sum(t => t.TicketTotal),
            MaterialsUsageSummary = materialsUsage
        };

        _logger.LogInformation("Traffic Light ticket billing data complete: {Count} tickets, Total: {Total:C}",
            tickets.Count, response.GrandTotal);

        return response;
    }

    /// <summary>
    /// Get tickets for a customer within a date range
    /// </summary>
    private async Task<List<TrafficTicketData>> GetTicketsAsync(
        string customerName,
        DateTime startDate,
        DateTime endDate,
        string? divisionId)
    {
        var qb = new QuickBaseConnector(_settings);
        var tickets = new List<TrafficTicketData>();

        // Build where clause
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
                TicketFields.Location,
                TicketFields.LocationType,
                TicketFields.AgencyId,
                TicketFields.DateTimeOpened,
                TicketFields.ServiceCategory,
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
                TicketFields.RemovePricing,
                TicketFields.LMBilledSeparately
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
                var ticket = new TrafficTicketData
                {
                    RecordId = GetStringValue(obj, TicketFields.RecordId),
                    TicketId = GetStringValue(obj, TicketFields.TicketId),
                    JobNumber = GetStringValue(obj, TicketFields.JobNumber),
                    CustomerName = GetStringValue(obj, TicketFields.CustomerName),
                    CallerName = GetStringValue(obj, TicketFields.CallerName),
                    CallerType = GetStringValue(obj, TicketFields.CallerType),
                    Location = GetStringValue(obj, TicketFields.Location),
                    LocationType = GetStringValue(obj, TicketFields.LocationType),
                    AgencyId = GetStringValue(obj, TicketFields.AgencyId),
                    ServiceCategory = GetStringValue(obj, TicketFields.ServiceCategory),
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
                    RemovePricing = GetStringValue(obj, TicketFields.RemovePricing) == "1",
                    LMBilledSeparately = GetStringValue(obj, TicketFields.LMBilledSeparately) == "1"
                };

                tickets.Add(ticket);
            }
        }

        return tickets;
    }

    /// <summary>
    /// Get all tickets across all customers in a date range
    /// </summary>
    public async Task<List<TrafficTicketData>> GetAllTicketsAsync(DateTime startDate, DateTime endDate)
    {
        var qb = new QuickBaseConnector(_settings);
        var tickets = new List<TrafficTicketData>();

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
                TicketFields.Location,
                TicketFields.LocationType,
                TicketFields.AgencyId,
                TicketFields.DateTimeOpened,
                TicketFields.ServiceCategory,
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
                TicketFields.RemovePricing,
                TicketFields.LMBilledSeparately
            },
            where = whereClause,
            sortBy = new List<QBFieldSet>
            {
                new QBFieldSet { fieldId = TicketFields.CustomerName, order = "ASC" },
                new QBFieldSet { fieldId = TicketFields.CompletionDate, order = "ASC" }
            },
            options = new QBQueryOptions { top = 10000 }
        };

        _logger.LogInformation("Querying all Traffic Light tickets from {Start} to {End}", startDate, endDate);

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var ticket = new TrafficTicketData
                {
                    RecordId = GetStringValue(obj, TicketFields.RecordId),
                    TicketId = GetStringValue(obj, TicketFields.TicketId),
                    JobNumber = GetStringValue(obj, TicketFields.JobNumber),
                    CustomerName = GetStringValue(obj, TicketFields.CustomerName),
                    CallerName = GetStringValue(obj, TicketFields.CallerName),
                    CallerType = GetStringValue(obj, TicketFields.CallerType),
                    Location = GetStringValue(obj, TicketFields.Location),
                    LocationType = GetStringValue(obj, TicketFields.LocationType),
                    AgencyId = GetStringValue(obj, TicketFields.AgencyId),
                    ServiceCategory = GetStringValue(obj, TicketFields.ServiceCategory),
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
                    RemovePricing = GetStringValue(obj, TicketFields.RemovePricing) == "1",
                    LMBilledSeparately = GetStringValue(obj, TicketFields.LMBilledSeparately) == "1"
                };

                tickets.Add(ticket);
            }
        }

        _logger.LogInformation("Found {Count} Traffic Light tickets across all customers", tickets.Count);

        return tickets;
    }

    #endregion

    #region Line Item Methods

    /// <summary>
    /// Get labor line items for a ticket
    /// </summary>
    private async Task<List<TrafficLaborLineItem>> GetLaborForTicketAsync(
        string ticketId,
        string customerName,
        List<TrafficTechnicianData> technicians,
        bool shouldBill)
    {
        var qb = new QuickBaseConnector(_settings);
        var laborItems = new List<TrafficLaborLineItem>();

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
                LaborFields.TypeOfLabor,
                LaborFields.Date
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
                var teamMemberName = GetUserNameValue(obj, LaborFields.TeamMember);
                var typeOfHours = GetStringValue(obj, LaborFields.TypeOfHours);
                var typeOfLabor = GetStringValue(obj, LaborFields.TypeOfLabor);

                var labor = new TrafficLaborLineItem
                {
                    TicketId = ticketId,
                    Technician = teamMemberName,
                    Hours = GetDecimalValue(obj, LaborFields.Hours),
                    TypeOfHours = typeOfHours,
                    TypeOfLabor = typeOfLabor,
                    Date = ParseQuickBaseDate(GetStringValue(obj, LaborFields.Date)),
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
    private async Task<List<TrafficMaterialLineItem>> GetMaterialsForTicketAsync(string ticketId, string customerName)
    {
        var qb = new QuickBaseConnector(_settings);
        var materialItems = new List<TrafficMaterialLineItem>();

        var query = new QBQuery
        {
            from = MaterialLineItemsTableId,
            select = new List<int>
            {
                MaterialFields.RecordId,
                MaterialFields.TicketId,
                MaterialFields.ItemId,
                MaterialFields.ItemDescription,
                MaterialFields.Quantity,
                MaterialFields.NonInventory,
                MaterialFields.NonInventoryDescription,
                MaterialFields.NonInventorySalePrice,
                MaterialFields.ItemIdListPrice,
                MaterialFields.MaterialDescriptionCalc
            },
            where = $"{{{MaterialFields.TicketId}.EX.'{EscapeQueryValue(ticketId)}'}}",
            options = new QBQueryOptions { top = 100 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var isNonInventory = GetStringValue(obj, MaterialFields.NonInventory) == "1";
                var itemId = GetStringValue(obj, MaterialFields.ItemId);

                var material = new TrafficMaterialLineItem
                {
                    TicketId = ticketId,
                    ItemId = itemId,
                    Description = isNonInventory
                        ? GetStringValue(obj, MaterialFields.MaterialDescriptionCalc)
                        : GetStringValue(obj, MaterialFields.ItemDescription),
                    Quantity = GetDecimalValue(obj, MaterialFields.Quantity),
                    IsNonInventory = isNonInventory
                };

                // Get price
                if (isNonInventory)
                {
                    material.Price = GetDecimalValue(obj, MaterialFields.NonInventorySalePrice);
                }
                else
                {
                    var listPrice = GetDecimalValue(obj, MaterialFields.ItemIdListPrice);
                    var (sellPrice, isLumpSum) = await GetMaterialPriceAsync(itemId, customerName, listPrice);
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
    private async Task<List<TrafficEquipmentLineItem>> GetEquipmentForTicketAsync(
        string ticketId,
        string customerName,
        bool shouldBill)
    {
        var qb = new QuickBaseConnector(_settings);
        var equipmentItems = new List<TrafficEquipmentLineItem>();

        var query = new QBQuery
        {
            from = EquipmentLineItemsTableId,
            select = new List<int>
            {
                EquipmentFields.RecordId,
                EquipmentFields.TicketId,
                EquipmentFields.Equipment,
                EquipmentFields.Hours
            },
            where = $"{{{EquipmentFields.TicketId}.EX.'{EscapeQueryValue(ticketId)}'}}",
            options = new QBQueryOptions { top = 100 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                var equipmentName = GetStringValue(obj, EquipmentFields.Equipment);

                var equipment = new TrafficEquipmentLineItem
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

    #endregion

    #region Pricing Methods

    /// <summary>
    /// Get maintenance fee for Routine service
    /// </summary>
    public async Task<decimal> GetMaintenanceFeeAsync(string customerName, string serviceType, string locationType)
    {
        var qb = new QuickBaseConnector(_settings);

        // Try to find exact match first
        var query = new QBQuery
        {
            from = MaintenancePricingTableId,
            select = new List<int>
            {
                MaintenancePricingFields.MaintenancePrice
            },
            where = $"{{{MaintenancePricingFields.RelatedCustomer}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{MaintenancePricingFields.ServiceType}.EX.'{EscapeQueryValue(serviceType)}'}}AND{{{MaintenancePricingFields.LocationType}.EX.'{EscapeQueryValue(locationType)}'}}",
            options = new QBQueryOptions { top = 1 }
        };

        var result = await qb.Query(query);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];
            return GetDecimalValue(obj, MaintenancePricingFields.MaintenancePrice);
        }

        // Try without location type
        query.where = $"{{{MaintenancePricingFields.RelatedCustomer}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{MaintenancePricingFields.ServiceType}.EX.'{EscapeQueryValue(serviceType)}'}}";
        result = await qb.Query(query);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];
            return GetDecimalValue(obj, MaintenancePricingFields.MaintenancePrice);
        }

        _logger.LogWarning("No maintenance pricing found for {Customer}/{ServiceType}/{LocationType}",
            customerName, serviceType, locationType);
        return 0;
    }

    /// <summary>
    /// Get maintenance prices list for a customer
    /// </summary>
    public async Task<List<TrafficMaintenancePriceData>> GetMaintenancePriceListAsync(string customerName)
    {
        var qb = new QuickBaseConnector(_settings);
        var prices = new List<TrafficMaintenancePriceData>();

        var query = new QBQuery
        {
            from = MaintenancePricingTableId,
            select = new List<int>
            {
                MaintenancePricingFields.ServiceType,
                MaintenancePricingFields.LocationType,
                MaintenancePricingFields.MaintenancePrice,
                MaintenancePricingFields.RelatedCustomer
            },
            where = $"{{{MaintenancePricingFields.RelatedCustomer}.EX.'{EscapeQueryValue(customerName)}'}}",
            options = new QBQueryOptions { top = 500 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                prices.Add(new TrafficMaintenancePriceData
                {
                    ServiceType = GetStringValue(obj, MaintenancePricingFields.ServiceType),
                    LocationType = GetStringValue(obj, MaintenancePricingFields.LocationType),
                    CustomerName = GetStringValue(obj, MaintenancePricingFields.RelatedCustomer),
                    MaintenancePrice = GetDecimalValue(obj, MaintenancePricingFields.MaintenancePrice)
                });
            }
        }

        return prices;
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
                LaborPricingFields.LaborPrice
            },
            where = $"{{{LaborPricingFields.RelatedCustomer}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{LaborPricingFields.TypeOfLabor}.EX.'{EscapeQueryValue(typeOfLabor)}'}}AND{{{LaborPricingFields.TypeOfHours}.EX.'{EscapeQueryValue(typeOfHours)}'}}",
            options = new QBQueryOptions { top = 1 }
        };

        var result = await qb.Query(query);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];
            return GetDecimalValue(obj, LaborPricingFields.LaborPrice);
        }

        _logger.LogWarning("No labor pricing found for {Customer}/{TypeOfLabor}/{TypeOfHours}",
            customerName, typeOfLabor, typeOfHours);
        return 0;
    }

    /// <summary>
    /// Get material price and lump sum flag
    /// </summary>
    private async Task<(decimal price, bool isLumpSum)> GetMaterialPriceAsync(string itemId, string customerName, decimal listPrice)
    {
        var qb = new QuickBaseConnector(_settings);

        // Query by item ID and customer name composite key
        var compositeKey = itemId + customerName;
        var query = new QBQuery
        {
            from = MaterialPricingTableId,
            select = new List<int>
            {
                MaterialPricingFields.SellPrice,
                MaterialPricingFields.LumpSum
            },
            where = $"{{{MaterialPricingFields.ItemIdAndCustomerCalc}.EX.'{EscapeQueryValue(compositeKey)}'}}",
            options = new QBQueryOptions { top = 1 }
        };

        var result = await qb.Query(query);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];
            var sellPrice = GetDecimalValue(obj, MaterialPricingFields.SellPrice);
            var isLumpSum = GetStringValue(obj, MaterialPricingFields.LumpSum) == "1";

            // If sell price is 0, fall back to list price
            if (sellPrice == 0 && listPrice > 0)
            {
                return (listPrice, isLumpSum);
            }

            return (sellPrice, isLumpSum);
        }

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
                EquipmentPricingFields.Price
            },
            where = $"{{{EquipmentPricingFields.RelatedCustomer}.EX.'{EscapeQueryValue(customerName)}'}}AND{{{EquipmentPricingFields.Equipment}.EX.'{EscapeQueryValue(equipmentName)}'}}",
            options = new QBQueryOptions { top = 1 }
        };

        var result = await qb.Query(query);

        if (result?.data != null && result.data.Count > 0)
        {
            JObject obj = result.data[0];
            return GetDecimalValue(obj, EquipmentPricingFields.Price);
        }

        _logger.LogWarning("No equipment pricing found for {Customer}/{Equipment}",
            customerName, equipmentName);
        return 0;
    }

    /// <summary>
    /// Get all technicians
    /// </summary>
    private async Task<List<TrafficTechnicianData>> GetTechniciansAsync()
    {
        var qb = new QuickBaseConnector(_settings);
        var technicians = new List<TrafficTechnicianData>();

        var query = new QBQuery
        {
            from = TeamMembersTableId,
            select = new List<int>
            {
                TeamMemberFields.RecordId,
                TeamMemberFields.Name,
                TeamMemberFields.Email
            },
            options = new QBQueryOptions { top = 500 }
        };

        var result = await qb.Query(query);

        if (result?.data != null)
        {
            foreach (var record in result.data)
            {
                JObject obj = record;
                technicians.Add(new TrafficTechnicianData
                {
                    TechId = GetStringValue(obj, TeamMemberFields.RecordId),
                    Name = GetStringValue(obj, TeamMemberFields.Name),
                    Email = GetStringValue(obj, TeamMemberFields.Email)
                });
            }
        }

        return technicians;
    }

    /// <summary>
    /// Determine if labor and equipment should be billed based on materials
    /// </summary>
    private bool ShouldBillLaborAndEquipment(List<TrafficMaterialLineItem> materials)
    {
        // If any material is marked as lump sum, don't bill labor/equipment
        if (materials.Any(m => m.IsLumpSum))
            return false;

        return true;
    }

    #endregion

    #region PDF Generation

    /// <summary>
    /// Generate a PDF ticket invoice from Traffic Light billing data
    /// Groups tickets by LocationType (section headers) and Location (with subtotals)
    /// Summary page appears first (matching legacy report)
    /// </summary>
    public byte[] GenerateTicketBillingPdf(TrafficTicketBillingResponse data)
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

        // ============ PAGE 1: Summary page (first, matching legacy) ============
        if (File.Exists(logoPath))
        {
            doc.Rect.String = "40 740 180 790";
            doc.AddImageFile(logoPath, 1);
        }

        var summaryHtml = BuildSummaryPageHtml(data);
        doc.Rect.String = "40 50 555 720";
        int summaryId = doc.AddImageHtml(summaryHtml);

        // Handle overflow of summary page
        while (doc.Chainable(summaryId))
        {
            doc.Page = doc.AddPage();
            doc.Rect.String = "40 50 555 780";
            summaryId = doc.AddImageToChain(summaryId);
        }

        // ============ TICKET PAGES: Grouped by LocationType, then Location ============
        // Group tickets by LocationType first (for section headers)
        // Custom LocationType order: Signalized Intersection first, then Other, then Caltrans
        Func<string, int> getLocationTypeSortOrder = (locationType) =>
        {
            if (string.IsNullOrEmpty(locationType)) return 99;
            if (locationType.Contains("Signalized", StringComparison.OrdinalIgnoreCase)) return 1;
            if (locationType.Contains("Other", StringComparison.OrdinalIgnoreCase)) return 2;
            if (locationType.Contains("Caltrans", StringComparison.OrdinalIgnoreCase)) return 3;
            return 50; // Unknown types sort in the middle
        };

        var ticketsByLocationType = data.Tickets
            .GroupBy(t => t.LocationType ?? "Other")
            .OrderBy(g => getLocationTypeSortOrder(g.Key));

        foreach (var locationTypeGroup in ticketsByLocationType)
        {
            string locationType = locationTypeGroup.Key;

            // Group by Location within this LocationType
            var ticketsByLocation = locationTypeGroup
                .GroupBy(t => t.Location ?? "Unknown")
                .OrderBy(g => g.Key);

            foreach (var locationGroup in ticketsByLocation)
            {
                // Start a new page for each location group
                doc.Page = doc.AddPage();

                // Build HTML for this location group (all tickets at this location + subtotals)
                var locationHtml = BuildLocationGroupHtml(
                    locationType,
                    locationGroup.Key,
                    locationGroup.ToList(),
                    data.CustomerName,
                    data.BillingPeriod);

                // Set content area
                doc.Rect.String = "40 50 555 720";

                // Add HTML content
                int theId = doc.AddImageHtml(locationHtml);

                // Handle overflow to additional pages
                while (doc.Chainable(theId))
                {
                    doc.Page = doc.AddPage();
                    doc.Rect.String = "40 50 555 720";
                    theId = doc.AddImageToChain(theId);
                }
            }
        }

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

    /// <summary>
    /// Build HTML for a location group (all tickets at one location with location subtotals)
    /// Matches legacy report format with LocationType header and location subtotals
    /// </summary>
    private string BuildLocationGroupHtml(
        string locationType,
        string location,
        List<TrafficTicketData> tickets,
        string customerName,
        string billingPeriod)
    {
        var sb = new StringBuilder();

        sb.Append(@"
            <html>
            <head>
            <style>
                body { font-family: Arial, sans-serif; font-size: 10pt; margin: 0; padding: 0; }
                .location-type-header { font-size: 14pt; font-weight: bold; text-decoration: underline; margin-bottom: 15px; }
                .location-header { font-weight: bold; margin-bottom: 5px; border-bottom: 2px solid #000; padding-bottom: 3px; }
                .ticket-section { margin-bottom: 20px; border-bottom: 1px solid #ccc; padding-bottom: 10px; }
                .info-row { margin-bottom: 3px; }
                .info-label { font-weight: bold; display: inline-block; width: 120px; }
                .section-title { font-weight: bold; text-decoration: underline; margin-top: 10px; margin-bottom: 5px; }
                table { width: 100%; border-collapse: collapse; margin-top: 5px; margin-bottom: 10px; font-size: 9pt; }
                th { font-weight: bold; padding: 4px; border-bottom: 1px solid #000; text-align: left; }
                th.right { text-align: right; }
                td { padding: 4px; }
                td.right { text-align: right; }
                .total-row { border-top: 1px solid #000; font-weight: bold; }
                .subtotal-row { margin-bottom: 5px; }
                .subtotal-label { display: inline-block; width: 200px; }
                .subtotal-value { font-weight: bold; }
                .location-totals { margin-top: 20px; padding-top: 10px; border-top: 2px solid #000; }
                .maintenance-fee { margin-top: 10px; font-size: 10pt; }
                .ticket-total { margin-top: 10px; text-align: right; font-size: 10pt; }
            </style>
            </head>
            <body>");

        // Location Type section header (e.g., "Signalized Intersection", "Caltrans", "Other and Other")
        sb.Append($"<div class='location-type-header'>{System.Net.WebUtility.HtmlEncode(locationType)}</div>");

        // Location header (e.g., "American Canyon Rd & Flosden Rd/Newell Dr")
        sb.Append($"<div class='location-header'>{System.Net.WebUtility.HtmlEncode(location)}</div>");

        // Render each ticket at this location
        foreach (var ticket in tickets)
        {
            sb.Append(BuildTicketSectionHtml(ticket));
        }

        // Calculate and render location subtotals
        decimal locationLaborCost = tickets.Sum(t => t.TicketLaborTotal);
        decimal locationMaterialsCost = tickets.Sum(t => t.TicketMaterialsTotal);
        decimal locationEquipmentCost = tickets.Sum(t => t.TicketEquipmentTotal);
        decimal locationMaintenanceCost = tickets.Where(t => t.IsRoutineMaintenance).Sum(t => t.MaintenanceFee);
        decimal locationTotal = tickets.Sum(t => t.TicketTotal);

        sb.Append("<div class='location-totals'>");
        sb.Append($"<div class='subtotal-row'><span class='subtotal-label'>{System.Net.WebUtility.HtmlEncode(location)} Totals:</span></div>");
        sb.Append($"<div class='subtotal-row'><span class='subtotal-label' style='margin-left:20px;'>Labor Cost:</span> <span class='subtotal-value'>{locationLaborCost:C}</span></div>");
        sb.Append($"<div class='subtotal-row'><span class='subtotal-label' style='margin-left:20px;'>Materials Cost:</span> <span class='subtotal-value'>{locationMaterialsCost:C}</span></div>");
        sb.Append($"<div class='subtotal-row'><span class='subtotal-label' style='margin-left:20px;'>Equipment Cost:</span> <span class='subtotal-value'>{locationEquipmentCost:C}</span></div>");
        if (locationMaintenanceCost > 0)
        {
            sb.Append($"<div class='subtotal-row'><span class='subtotal-label' style='margin-left:20px;'>Maintenance Cost:</span> <span class='subtotal-value'>{locationMaintenanceCost:C}</span></div>");
        }
        sb.Append($"<div class='subtotal-row' style='border-top: 1px solid #000; padding-top: 5px; margin-top: 5px;'><span class='subtotal-label' style='margin-left:20px;'>Total:</span> <span class='subtotal-value'>{locationTotal:C}</span></div>");
        sb.Append("</div>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Build HTML for a single ticket section (without page wrapper - used within location groups)
    /// </summary>
    private string BuildTicketSectionHtml(TrafficTicketData ticket)
    {
        var sb = new StringBuilder();
        bool showPricing = !ticket.RemovePricing;

        sb.Append("<div class='ticket-section'>");

        // Ticket header line
        sb.Append($"<div style='margin-bottom:10px;'><strong>Ticket #:</strong> {System.Net.WebUtility.HtmlEncode(ticket.TicketId)}</div>");

        // Info rows - two columns
        sb.Append("<table style='border:none;'><tr><td style='width:50%;vertical-align:top;border:none;padding:0;'>");
        sb.Append($"<div class='info-row'><span class='info-label'>Date:</span> {ticket.DateTimeOpened?.ToString("M/d/yyyy h:mm:sstt") ?? ""}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Service Type:</span> {System.Net.WebUtility.HtmlEncode(ticket.ServiceType)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Caller:</span> {System.Net.WebUtility.HtmlEncode(ticket.CallerName)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Technician:</span> {System.Net.WebUtility.HtmlEncode(ticket.Technician)}</div>");
        sb.Append("</td><td style='width:50%;vertical-align:top;border:none;padding:0;'>");
        sb.Append($"<div class='info-row'><span class='info-label'>End Date/Time:</span> {ticket.CompletionDate?.ToShortDateString() ?? ""} {System.Net.WebUtility.HtmlEncode(ticket.CompletionTime)}</div>");
        sb.Append($"<div class='info-row'><span class='info-label'>Problem:</span> {System.Net.WebUtility.HtmlEncode(ticket.ProblemType)}</div>");
        sb.Append("</td></tr></table>");

        // Details and Notes
        sb.Append($"<div class='info-row'><span class='info-label'>Details:</span> {System.Net.WebUtility.HtmlEncode(ticket.Details)}</div>");
        if (!string.IsNullOrEmpty(ticket.Analysis))
        {
            sb.Append($"<div class='info-row'><span class='info-label'>Notes:</span> {System.Net.WebUtility.HtmlEncode(ticket.Analysis)}</div>");
        }

        // Show Maintenance Fee if Routine, otherwise show line items
        if (ticket.IsRoutineMaintenance)
        {
            if (showPricing)
            {
                // Show LocationType/ServiceType/Price table like legacy
                sb.Append("<table style='margin-top:10px;'>");
                sb.Append("<tr><th style='text-decoration:underline;'>LocationType</th><th style='text-decoration:underline;'>ServiceType</th><th class='right' style='text-decoration:underline;'>Price</th></tr>");
                sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(ticket.LocationType)}</td><td>{System.Net.WebUtility.HtmlEncode(ticket.ServiceType)}</td><td class='right'>{ticket.MaintenanceFee:C}</td></tr>");
                sb.Append($"<tr><td colspan='2' style='text-align:right;'><strong>Total Cost:</strong></td><td class='right'><strong>{ticket.MaintenanceFee:C}</strong></td></tr>");
                sb.Append("</table>");
            }
        }
        else
        {
            // Labor table
            if (ticket.LaborItems.Count > 0 && showPricing)
            {
                sb.Append("<table style='margin-top:10px;'><tr><th style='text-decoration:underline;'>Technician</th><th style='text-decoration:underline;'>Type of Hours</th><th style='text-decoration:underline;'>Type of Labor</th><th class='right' style='text-decoration:underline;'>Hours</th><th class='right' style='text-decoration:underline;'>Rate</th><th class='right' style='text-decoration:underline;'>Cost</th></tr>");
                foreach (var labor in ticket.LaborItems)
                {
                    sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(labor.Technician)}</td><td>{System.Net.WebUtility.HtmlEncode(labor.TypeOfHours)}</td><td>{System.Net.WebUtility.HtmlEncode(labor.TypeOfLabor)}</td><td class='right'>{labor.Hours:N2}</td><td class='right'>{labor.Rate:C}</td><td class='right'>{labor.Cost:C}</td></tr>");
                }
                // Add subtotals by Type of Hours
                var laborByType = ticket.LaborItems.GroupBy(l => l.TypeOfHours);
                foreach (var group in laborByType)
                {
                    sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(group.Key)} :</td><td></td><td></td><td></td><td></td><td class='right'>{group.Sum(l => l.Cost):C}</td></tr>");
                }
                sb.Append($"<tr class='total-row'><td><strong>Total Hours:</strong></td><td></td><td></td><td></td><td></td><td class='right'><strong>{ticket.TicketLaborTotal:C}</strong></td></tr>");
                sb.Append("</table>");
            }

            // Materials table
            if (ticket.MaterialItems.Count > 0 && showPricing)
            {
                sb.Append("<div class='section-title'>Materials Used:</div>");
                sb.Append("<table><tr><th style='text-decoration:underline;'>Description</th><th class='right' style='text-decoration:underline;'>Quantity</th><th class='right' style='text-decoration:underline;'>Price</th><th class='right' style='text-decoration:underline;'>Cost</th></tr>");
                foreach (var material in ticket.MaterialItems)
                {
                    sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(material.ItemId)} - {System.Net.WebUtility.HtmlEncode(material.Description)}</td><td class='right'>{material.Quantity:N2}</td><td class='right'>{material.Price:C}</td><td class='right'>{material.Cost:C}</td></tr>");
                }
                sb.Append($"<tr class='total-row'><td><strong>Total Materials:</strong></td><td class='right'>{ticket.MaterialItems.Sum(m => m.Quantity):N2}</td><td></td><td class='right'><strong>{ticket.TicketMaterialsTotal:C}</strong></td></tr>");
                sb.Append("</table>");
            }

            // Equipment table
            if (ticket.EquipmentItems.Count > 0 && showPricing)
            {
                sb.Append("<table style='margin-top:10px;'><tr><th style='text-decoration:underline;'>Equipment</th><th class='right' style='text-decoration:underline;'>Hours</th><th class='right' style='text-decoration:underline;'>Price</th><th class='right' style='text-decoration:underline;'>Cost</th></tr>");
                foreach (var equipment in ticket.EquipmentItems)
                {
                    sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(equipment.Equipment)}</td><td class='right'>{equipment.Hours:N2}</td><td class='right'>{equipment.Rate:N2}</td><td class='right'>{equipment.Cost:C}</td></tr>");
                }
                sb.Append($"<tr class='total-row'><td><strong>Total Equipment Cost:</strong></td><td class='right'>{ticket.EquipmentItems.Sum(e => e.Hours):N2}</td><td></td><td class='right'><strong>{ticket.TicketEquipmentTotal:C}</strong></td></tr>");
                sb.Append("</table>");
            }
        }

        // Ticket Total
        if (showPricing && !ticket.IsRoutineMaintenance)
        {
            sb.Append($"<div class='ticket-total'><strong>Total: {ticket.TicketTotal:C}</strong></div>");
        }

        sb.Append("</div>"); // end ticket-section
        return sb.ToString();
    }

    private string BuildSummaryPageHtml(TrafficTicketBillingResponse data)
    {
        var sb = new StringBuilder();

        // Calculate Total Extras (Labor + Equipment + Materials)
        decimal totalExtras = data.TotalLaborCost + data.TotalEquipmentCost + data.TotalMaterialsCost;

        sb.Append(@"
            <html>
            <head>
            <style>
                body { font-family: Arial, sans-serif; font-size: 11pt; margin: 0; padding: 0; }
                .report-title { font-size: 16pt; font-weight: bold; text-align: center; margin-bottom: 5px; }
                .customer-name { font-size: 14pt; font-weight: bold; text-align: center; margin-bottom: 3px; }
                .date-range { font-size: 12pt; font-weight: bold; text-align: center; margin-bottom: 25px; }
                .total-section { margin-bottom: 20px; }
                .total-row { margin-bottom: 8px; }
                .total-label { display: inline-block; width: 180px; font-weight: bold; }
                .total-value { font-weight: bold; }
                .extras-section { margin-left: 30px; margin-top: 5px; }
                .extras-label { display: inline-block; width: 150px; }
                .materials-title { font-weight: bold; margin-top: 25px; margin-bottom: 10px; }
                .material-row { margin-bottom: 5px; margin-left: 20px; }
                .material-name { display: inline-block; width: 280px; }
            </style>
            </head>
            <body>");

        // Title and header - matching legacy "Monthly Billing Report" format
        sb.Append("<div class='report-title'>Monthly Billing Report</div>");
        sb.Append($"<div class='date-range'>{data.StartDate:MM/dd/yyyy} to {data.EndDate:MM/dd/yyyy}</div>");
        sb.Append($"<div class='customer-name'>{System.Net.WebUtility.HtmlEncode(data.CustomerName)}</div>");

        // Main totals section - matching legacy format
        sb.Append("<div class='total-section'>");
        sb.Append($"<div class='total-row'><span class='total-label'>Total Maintenance:</span> <span class='total-value'>{data.TotalMaintenanceFees:C}</span></div>");
        sb.Append($"<div class='total-row'><span class='total-label'>Total Extras:</span> <span class='total-value'>{totalExtras:C}</span></div>");

        // Extras subtotals
        sb.Append("<div class='extras-section'>");
        sb.Append("<div>Extras subtotals:</div>");
        sb.Append($"<div class='total-row'><span class='extras-label'>Labor:</span> <span>{data.TotalLaborCost:C}</span></div>");
        sb.Append($"<div class='total-row'><span class='extras-label'>Equipment:</span> <span>{data.TotalEquipmentCost:C}</span></div>");
        sb.Append($"<div class='total-row'><span class='extras-label'>Materials:</span> <span>{data.TotalMaterialsCost:C}</span></div>");
        sb.Append("</div>");
        sb.Append("</div>");

        // Materials Used list - matching legacy format (name + quantity only on summary)
        if (data.MaterialsUsageSummary.Count > 0)
        {
            sb.Append("<div class='materials-title'>Materials Used:</div>");
            foreach (var material in data.MaterialsUsageSummary)
            {
                sb.Append($"<div class='material-row'><span class='material-name'>{System.Net.WebUtility.HtmlEncode(material.Description)}</span> <span>{material.TotalQuantity:N0}</span></div>");
            }
        }

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
        return value.Replace("'", "\\'");
    }

    private static string GetBillingPeriod(DateTime date)
    {
        return date.ToString("MMMM yyyy");
    }

    private static DateTime? ParseQuickBaseDate(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

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

        if (long.TryParse(value, out var milliseconds))
        {
            var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            var dateTime = DateTime.Today.Add(timeSpan);
            return dateTime.ToString("h:mm tt");
        }

        if (DateTime.TryParse(value, out var parsedTime))
        {
            return parsedTime.ToString("h:mm tt");
        }

        return value;
    }

    #endregion
}
