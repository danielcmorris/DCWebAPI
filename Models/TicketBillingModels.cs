namespace DCElectricWebAPI.Models;

/// <summary>
/// Request model for generating ticket billing reports
/// </summary>
public class TicketBillingRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string? DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Response containing all ticket data for billing
/// </summary>
public class TicketBillingResponse
{
    public string CustomerName { get; set; } = string.Empty;
    public string? DivisionName { get; set; }
    public string PricingLevel { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public List<TicketData> Tickets { get; set; } = new();

    // Summary totals
    public int TicketCount { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalMaterialsCost { get; set; }
    public decimal TotalEquipmentHours { get; set; }
    public decimal TotalEquipmentCost { get; set; }
    public decimal GrandTotal { get; set; }

    // Materials usage summary (aggregated across all tickets)
    public List<MaterialUsageSummary> MaterialsUsageSummary { get; set; } = new();

    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Individual ticket data with related labor, materials, and equipment
/// </summary>
public class TicketData
{
    public string RecordId { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public string JobNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CallerName { get; set; } = string.Empty;
    public string CallerType { get; set; } = string.Empty;
    public string StreetLightNumber { get; set; } = string.Empty;
    public string FixtureType { get; set; } = string.Empty;
    public string LocationAddress { get; set; } = string.Empty;
    public string LocationCrossStreet { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string ProblemType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public string Technician { get; set; } = string.Empty;
    public bool NotBillable { get; set; }
    public bool BillableOverride { get; set; }

    public DateTime? DateTimeOpened { get; set; }
    public DateTime? StartDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public DateTime? CompletionDate { get; set; }
    public string CompletionTime { get; set; } = string.Empty;

    // Related line items
    public List<LaborLineItem> LaborItems { get; set; } = new();
    public List<MaterialLineItem> MaterialItems { get; set; } = new();
    public List<EquipmentLineItem> EquipmentItems { get; set; } = new();

    // Ticket totals — kept as exact decimals; rounding happens at the invoice grand-total level
    public decimal TicketLaborTotal => LaborItems.Sum(l => l.Cost);
    public decimal TicketMaterialsTotal => MaterialItems.Sum(m => m.Cost);
    public decimal TicketEquipmentTotal => EquipmentItems.Sum(e => e.Cost);
    public decimal TicketTotal => TicketLaborTotal + TicketMaterialsTotal + TicketEquipmentTotal;
}

/// <summary>
/// Labor line item for a ticket
/// </summary>
public class LaborLineItem
{
    public string TicketId { get; set; } = string.Empty;
    public string Technician { get; set; } = string.Empty;
    public string TypeOfHours { get; set; } = string.Empty;  // Regular Time, Overtime, Premium Time
    public string TypeOfLabor { get; set; } = string.Empty;  // Electrician, Laborer
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Cost => Hours * Rate;
}

/// <summary>
/// Material line item for a ticket
/// </summary>
public class MaterialLineItem
{
    public string TicketId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public bool IsLumpSum { get; set; }
    public decimal Cost => Quantity * Price;
}

/// <summary>
/// Equipment line item for a ticket
/// </summary>
public class EquipmentLineItem
{
    public string TicketId { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Cost => Hours * Rate;
}

/// <summary>
/// Material usage summary (aggregated across all tickets)
/// </summary>
public class MaterialUsageSummary
{
    public string Description { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Technician/Team Member data
/// </summary>
public class TechnicianData
{
    public string TechId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
