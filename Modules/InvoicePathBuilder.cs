using System.Text.RegularExpressions;

namespace DCElectricWebAPI.Modules;

public static class InvoicePathBuilder
{
    /// <summary>
    /// Builds a GCS path for invoice storage: /invoices/{customer}/{type}/{yymm}/{filename}
    /// </summary>
    /// <param name="customerName">Customer name (will be sanitized for path)</param>
    /// <param name="invoiceType">Invoice type: "streetlights" or "trafficlights"</param>
    /// <param name="startDate">Report start date (used for yymm folder)</param>
    /// <param name="fileName">The PDF filename</param>
    /// <returns>Full GCS object path</returns>
    public static string BuildInvoicePath(string customerName, string invoiceType, DateTime startDate, string fileName)
    {
        var sanitizedCustomer = SanitizeForPath(customerName);
        var yymm = startDate.ToString("yyMM");

        return $"invoices/{sanitizedCustomer}/{invoiceType}/{yymm}/{fileName}";
    }

    /// <summary>
    /// Sanitizes a string for use in a file path by converting to lowercase,
    /// replacing spaces with hyphens, and removing invalid characters.
    /// </summary>
    private static string SanitizeForPath(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "unknown";

        // Convert to lowercase
        var result = input.ToLowerInvariant();

        // Replace spaces with hyphens
        result = result.Replace(" ", "-");

        // Remove any characters that aren't alphanumeric, hyphen, or underscore
        result = Regex.Replace(result, @"[^a-z0-9\-_]", "");

        // Replace multiple consecutive hyphens with single hyphen
        result = Regex.Replace(result, @"-+", "-");

        // Trim leading/trailing hyphens
        result = result.Trim('-');

        return string.IsNullOrEmpty(result) ? "unknown" : result;
    }
}
