// Quick Quickbase Field Fetcher
// Run with: dotnet script FetchQuickbaseFields.csx
// Or copy into a .NET console app

#r "nuget: Newtonsoft.Json, 13.0.3"

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

// Configuration
var realm = "dcelectricgroup.quickbase.com";
var token = "***REMOVED***";
var baseUrl = "https://api.quickbase.com/v1";

// Tables to fetch - add more as needed
var tables = new Dictionary<string, string>
{
    { "Tickets", "bjrvqd33t" },
    // Add other table IDs here after discovering them
};

class QBFieldDetails
{
    public int id { get; set; }
    public string label { get; set; }
    public string fieldType { get; set; }
    public string mode { get; set; }
    public bool appearsByDefault { get; set; }
    public bool required { get; set; }
}

async Task<List<QBFieldDetails>> GetFields(string tableId)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("QB-Realm-Hostname", realm);
    client.DefaultRequestHeaders.Add("Authorization", $"QB-USER-TOKEN {token}");
    
    var response = await client.GetAsync($"{baseUrl}/fields?tableId={tableId}");
    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<List<QBFieldDetails>>(json);
}

var output = new System.Text.StringBuilder();
output.AppendLine("# DC Electric - Quickbase Field Mappings (Auto-Generated)");
output.AppendLine();
output.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
output.AppendLine();

foreach (var table in tables)
{
    Console.WriteLine($"Fetching fields for {table.Key} ({table.Value})...");
    
    try
    {
        var fields = await GetFields(table.Value);
        
        output.AppendLine("---");
        output.AppendLine();
        output.AppendLine($"## {table.Key} Table (`{table.Value}`)");
        output.AppendLine();
        output.AppendLine("| Field ID | Label | Type | Mode | Default | Required |");
        output.AppendLine("|----------|-------|------|------|---------|----------|");
        
        foreach (var field in fields.OrderBy(f => f.id))
        {
            var defaultVal = field.appearsByDefault ? "Yes" : "No";
            var required = field.required ? "Yes" : "No";
            var label = field.label?.Replace("|", "\\|") ?? "";
            output.AppendLine($"| {field.id} | {label} | {field.fieldType} | {field.mode} | {defaultVal} | {required} |");
        }
        
        output.AppendLine();
        Console.WriteLine($"  Found {fields.Count} fields");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
        output.AppendLine($"*Error fetching fields: {ex.Message}*");
        output.AppendLine();
    }
}

// Write to file
var outputPath = @".claude\FIELD_MAPPINGS_GENERATED.md";
File.WriteAllText(outputPath, output.ToString());
Console.WriteLine($"\nOutput written to: {outputPath}");
