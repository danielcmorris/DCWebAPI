namespace DCElectricWebAPI.Models;

/// <summary>
/// Request model for generating Traffic Lights ticket billing reports
/// </summary>
public class TrafficTicketBillingRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string? DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Response containing all Traffic Lights ticket data for billing
/// </summary>
public class TrafficTicketBillingResponse
{
    public string CustomerName { get; set; } = string.Empty;
    public string? DivisionName { get; set; }
    public string PricingLevel { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public List<TrafficTicketData> Tickets { get; set; } = new();

    // Summary totals
    public int TicketCount { get; set; }
    public decimal TotalMaintenanceFees { get; set; }  // Routine service flat fees
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalMaterialsCost { get; set; }
    public decimal TotalEquipmentHours { get; set; }
    public decimal TotalEquipmentCost { get; set; }
    public decimal GrandTotal { get; set; }

    // Materials usage summary (aggregated across all tickets)
    public List<TrafficMaterialUsageSummary> MaterialsUsageSummary { get; set; } = new();

    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Individual Traffic Lights ticket data with related labor, materials, and equipment
/// </summary>
public class TrafficTicketData
{
    public string RecordId { get; set; } = string.Empty;
    public string TicketId { get; set; } = string.Empty;
    public string JobNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CallerName { get; set; } = string.Empty;
    public string CallerType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string AgencyId { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;  // "Routine" triggers flat maintenance fee
    public string ServiceType { get; set; } = string.Empty;
    public string ProblemType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public string Technician { get; set; } = string.Empty;

    // Billing flags
    public bool NotBillable { get; set; }
    public bool RemovePricing { get; set; }  // Suppress dollar amounts in PDF
    public bool LMBilledSeparately { get; set; }  // L&M Billed Separately flag
    public bool BillableOverride { get; set; }

    // Dates and times
    public DateTime? DateTimeOpened { get; set; }
    public DateTime? StartDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public DateTime? CompletionDate { get; set; }
    public string CompletionTime { get; set; } = string.Empty;

    // Related line items
    public List<TrafficLaborLineItem> LaborItems { get; set; } = new();
    public List<TrafficMaterialLineItem> MaterialItems { get; set; } = new();
    public List<TrafficEquipmentLineItem> EquipmentItems { get; set; } = new();

    // Flat maintenance fee (if Routine service)
    public decimal MaintenanceFee { get; set; }

    // Check if this is a routine maintenance ticket
    public bool IsRoutineMaintenance => !string.IsNullOrEmpty(ServiceCategory) &&
        ServiceCategory.Contains("Routine", StringComparison.OrdinalIgnoreCase);

    // Ticket totals — FIX #5: Round each line item to 2 decimal places using
    // MidpointRounding.AwayFromZero to match legacy system's rounding strategy.
    public decimal TicketLaborTotal => LaborItems.Sum(l =>
        Math.Round(l.Hours * l.Rate, 2, MidpointRounding.AwayFromZero));
    public decimal TicketMaterialsTotal => MaterialItems.Sum(m =>
        Math.Round(m.Quantity * m.Price, 2, MidpointRounding.AwayFromZero));
    public decimal TicketEquipmentTotal => EquipmentItems.Sum(e =>
        Math.Round(e.Hours * e.Rate, 2, MidpointRounding.AwayFromZero));
    // FIX #13: Routine tickets bill the flat maintenance fee PLUS any materials and equipment
    // used (labor is folded into the flat fee). Non-Routine tickets bill labor + materials +
    // equipment. Matches legacy DCDBEntry.cs fillTotals (InvoiceCosts = labor + equip + maint + matl).
    public decimal TicketTotal => IsRoutineMaintenance
        ? MaintenanceFee + TicketMaterialsTotal + TicketEquipmentTotal
        : TicketLaborTotal + TicketMaterialsTotal + TicketEquipmentTotal;
}

/// <summary>
/// Labor line item for a Traffic Lights ticket
/// </summary>
public class TrafficLaborLineItem
{
    public string TicketId { get; set; } = string.Empty;
    public string Technician { get; set; } = string.Empty;
    public string TypeOfHours { get; set; } = string.Empty;  // Regular Time, Overtime, Premium Time
    public string TypeOfLabor { get; set; } = string.Empty;  // Electrician, Laborer, Superintendent
    public DateTime? Date { get; set; }
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Cost => Hours * Rate;
}

/// <summary>
/// Material line item for a Traffic Lights ticket
/// </summary>
public class TrafficMaterialLineItem
{
    public string TicketId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public bool IsLumpSum { get; set; }
    public bool IsNonInventory { get; set; }
    public decimal Cost => Quantity * Price;
}

/// <summary>
/// Equipment line item for a Traffic Lights ticket
/// </summary>
public class TrafficEquipmentLineItem
{
    public string TicketId { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Cost => Hours * Rate;
}

/// <summary>
/// Material usage summary (aggregated across all Traffic Lights tickets)
/// </summary>
public class TrafficMaterialUsageSummary
{
    public string Description { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Traffic Lights customer data with pricing information
/// </summary>
public class TrafficCustomerData
{
    public string CustomerName { get; set; } = string.Empty;
    public bool LMBilledSeparately { get; set; }
    public bool HasBillingDivisions { get; set; }
}

/// <summary>
/// Traffic Lights customer division data
/// </summary>
public class TrafficDivisionData
{
    public string RecordId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DivisionName { get; set; } = string.Empty;
}

/// <summary>
/// Maintenance pricing data for Traffic Lights
/// </summary>
public class TrafficMaintenancePriceData
{
    public string ServiceType { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal MaintenancePrice { get; set; }
}

/// <summary>
/// Technician/Team Member data for Traffic Lights
/// </summary>
public class TrafficTechnicianData
{
    public string TechId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Read-only row for the open routine maintenance ticket list
/// (mirrors the QuickBase "All Open Routine Tickets" report)
/// </summary>
public class TrafficOpenRoutineTicket
{
    public string RecordId { get; set; } = string.Empty;  // QuickBase record id, used for deep links (rid=)
    public string TicketId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string JobNumber { get; set; } = string.Empty;
    public DateTime? DateTimeOpened { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string ProblemType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string DueTime { get; set; } = string.Empty;
    public string AssignedTeamMember { get; set; } = string.Empty;
    public DateTime? CompletionDate { get; set; }
}
