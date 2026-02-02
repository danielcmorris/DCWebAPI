<# 
.SYNOPSIS
    Fetches Quickbase field mappings and generates markdown documentation

.DESCRIPTION
    This script queries the Quickbase API to get field definitions for specified tables
    and generates a markdown file with the field mappings.

.NOTES
    Run from the DCElectricWebAPI directory:
    powershell -ExecutionPolicy Bypass -File .\.claude\Get-QuickbaseFields.ps1
#>

# Configuration
$realm = "dcelectricgroup.quickbase.com"
$token = "***REMOVED***"
$baseUrl = "https://api.quickbase.com/v1"

# Tables to fetch fields for (Name = TableID)
# The Tickets table ID is known, others need to be discovered
$tables = [ordered]@{
    "Tickets" = "bjrvqd33t"
}

# First, let's get all tables from the Street Lights app to find other table IDs
$appId = "bjrvqd33c"  # Street Lights app

$headers = @{
    "QB-Realm-Hostname" = $realm
    "Authorization" = "QB-USER-TOKEN $token"
    "Content-Type" = "application/json"
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Quickbase Field Mapping Generator" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Get all tables in the app first
Write-Host "Fetching tables from Street Lights app ($appId)..." -ForegroundColor Yellow
try {
    $tablesResponse = Invoke-RestMethod -Uri "$baseUrl/tables?appId=$appId" -Headers $headers -Method Get
    
    Write-Host "Found $($tablesResponse.Count) tables:" -ForegroundColor Green
    foreach ($table in $tablesResponse) {
        Write-Host "  - $($table.name) (ID: $($table.id))" -ForegroundColor Gray
        
        # Add to our tables dictionary if not already there
        if (-not $tables.Contains($table.name)) {
            $tables[$table.name] = $table.id
        }
    }
    Write-Host ""
}
catch {
    Write-Host "Error fetching tables: $_" -ForegroundColor Red
}

# Build output
$output = @"
# DC Electric - Quickbase Field Mappings (Auto-Generated)

Generated on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Street Lights App (``$appId``)

"@

# Fetch fields for each table
foreach ($tableName in $tables.Keys) {
    $tableId = $tables[$tableName]
    
    Write-Host "Fetching fields for $tableName ($tableId)..." -ForegroundColor Cyan
    
    try {
        $fieldsResponse = Invoke-RestMethod -Uri "$baseUrl/fields?tableId=$tableId" -Headers $headers -Method Get
        
        $output += @"

---

### $tableName (``$tableId``)

| Field ID | Label | Type | Mode | Default | Required |
|----------|-------|------|------|---------|----------|
"@
        
        $sortedFields = $fieldsResponse | Sort-Object { $_.id }
        
        foreach ($field in $sortedFields) {
            $id = $field.id
            $label = if ($field.label) { $field.label -replace '\|', '\|' } else { "" }
            $type = $field.fieldType
            $mode = $field.mode
            $default = if ($field.appearsByDefault) { "Yes" } else { "No" }
            $required = if ($field.required) { "Yes" } else { "No" }
            
            $output += "| $id | $label | $type | $mode | $default | $required |`n"
        }
        
        Write-Host "  Found $($fieldsResponse.Count) fields" -ForegroundColor Green
    }
    catch {
        Write-Host "  Error: $_" -ForegroundColor Red
        $output += "`n*Error fetching fields for this table*`n"
    }
}

# Write output file
$outputPath = Join-Path $PSScriptRoot "FIELD_MAPPINGS_GENERATED.md"
$output | Out-File -FilePath $outputPath -Encoding UTF8

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Output written to: $outputPath" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Review the generated file" -ForegroundColor White
Write-Host "2. Copy relevant sections to FIELD_MAPPINGS.md" -ForegroundColor White
Write-Host "3. Add notes about which fields are used in your code" -ForegroundColor White
