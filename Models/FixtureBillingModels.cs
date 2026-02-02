namespace DCElectricWebAPI.Models;

/// <summary>
/// Request model for generating fixture billing reports
/// </summary>
public class FixtureBillingRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string? DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Response containing all fixture data for billing
/// </summary>
public class FixtureBillingResponse
{
    public string CustomerName { get; set; } = string.Empty;
    public string? DivisionName { get; set; }
    public string PricingLevel { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public List<FixtureLocationData> Locations { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public int TotalFixtures { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Individual fixture location data
/// </summary>
public class FixtureLocationData
{
    public string RecordId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string StreetLightNumber { get; set; } = string.Empty;
    public string FixtureType { get; set; } = string.Empty;
    public int FixtureQuantity { get; set; } = 1;
    public string PoleType { get; set; } = string.Empty;
    public string Voltage { get; set; } = string.Empty;
    public string Wattage { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal LineTotal => Price * FixtureQuantity;
}

/// <summary>
/// Customer data with pricing level
/// </summary>
public class CustomerPricingData
{
    public string CustomerName { get; set; } = string.Empty;
    public string GroupPricingLevel { get; set; } = string.Empty;
}

/// <summary>
/// Maintenance pricing lookup data
/// </summary>
public class MaintenancePriceData
{
    public string LocationType { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public decimal MaintenancePrice { get; set; }
}

/// <summary>
/// Customer division data
/// </summary>
public class CustomerDivisionData
{
    public string RecordId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DivisionName { get; set; } = string.Empty;
}
