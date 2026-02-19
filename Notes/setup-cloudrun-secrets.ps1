# Setup secrets for Cloud Run deployment
#
# This script creates secrets in Google Secret Manager and configures
# the Cloud Run service to use them.
#
# Prerequisites:
#   1. gcloud CLI authenticated
#   2. Enable Secret Manager API: gcloud services enable secretmanager.googleapis.com
#
# Usage:
#   .\setup-cloudrun-secrets.ps1 -ProjectId YOUR_PROJECT -Region us-west1

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectId,
    [string]$Region = "us-west1",
    [string]$ServiceName = "dcelectric-webapi"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Cloud Run Secrets Setup" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Enable Secret Manager API
Write-Host "Enabling Secret Manager API..." -ForegroundColor Yellow
gcloud services enable secretmanager.googleapis.com --project=$ProjectId

# Function to create or update a secret
function Set-Secret {
    param(
        [string]$SecretName,
        [string]$SecretValue,
        [string]$Description
    )

    Write-Host "Setting secret: $SecretName" -ForegroundColor Yellow

    # Check if secret exists
    $exists = gcloud secrets describe $SecretName --project=$ProjectId 2>$null

    if ($exists) {
        # Add new version
        $SecretValue | gcloud secrets versions add $SecretName --data-file=- --project=$ProjectId
    } else {
        # Create new secret
        $SecretValue | gcloud secrets create $SecretName --data-file=- --project=$ProjectId --labels="app=dcelectric-webapi"
    }
}

# Prompt for secrets
Write-Host ""
Write-Host "Enter the required secrets (press Enter to skip if already configured):" -ForegroundColor Cyan
Write-Host ""

$qbToken = Read-Host "QuickBase Token (quickbase__token)" -AsSecureString
$qbJlToken = Read-Host "QuickBase JL Token (quickbase__jltoken)" -AsSecureString
$qbSafeToken = Read-Host "QuickBase Safe Token (quickbase__safetoken)" -AsSecureString
$qbTrafficToken = Read-Host "QuickBase Traffic Token (quickbase__traffictoken)" -AsSecureString
$dbConnection = Read-Host "Database Connection String" -AsSecureString
$azureBlob = Read-Host "Azure Blob Storage Connection String" -AsSecureString
$abcpdfLicense = Read-Host "ABCpdf License Key" -AsSecureString
$googleCreds = Read-Host "Google Cloud Storage Credentials JSON (base64 encoded)" -AsSecureString

# Convert secure strings and create secrets
function ConvertFrom-SecureStringPlain {
    param([SecureString]$SecureString)
    if ($SecureString.Length -eq 0) { return $null }
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

$secrets = @{
    "dcelectric-qb-token" = ConvertFrom-SecureStringPlain $qbToken
    "dcelectric-qb-jltoken" = ConvertFrom-SecureStringPlain $qbJlToken
    "dcelectric-qb-safetoken" = ConvertFrom-SecureStringPlain $qbSafeToken
    "dcelectric-qb-traffictoken" = ConvertFrom-SecureStringPlain $qbTrafficToken
    "dcelectric-db-connection" = ConvertFrom-SecureStringPlain $dbConnection
    "dcelectric-azure-blob" = ConvertFrom-SecureStringPlain $azureBlob
    "dcelectric-abcpdf-license" = ConvertFrom-SecureStringPlain $abcpdfLicense
    "dcelectric-google-creds" = ConvertFrom-SecureStringPlain $googleCreds
}

foreach ($secret in $secrets.GetEnumerator()) {
    if ($secret.Value) {
        Set-Secret -SecretName $secret.Key -SecretValue $secret.Value
    }
}

# Get the Cloud Run service account
$serviceAccount = gcloud run services describe $ServiceName --region=$Region --project=$ProjectId --format='value(spec.template.spec.serviceAccountName)' 2>$null
if (-not $serviceAccount) {
    $projectNumber = gcloud projects describe $ProjectId --format='value(projectNumber)'
    $serviceAccount = "${projectNumber}-compute@developer.gserviceaccount.com"
}

Write-Host ""
Write-Host "Granting secret access to service account: $serviceAccount" -ForegroundColor Yellow

# Grant access to secrets
foreach ($secret in $secrets.Keys) {
    if ($secrets[$secret]) {
        gcloud secrets add-iam-policy-binding $secret `
            --member="serviceAccount:$serviceAccount" `
            --role="roles/secretmanager.secretAccessor" `
            --project=$ProjectId 2>$null
    }
}

# Update Cloud Run service with secret references
Write-Host ""
Write-Host "Updating Cloud Run service with secret references..." -ForegroundColor Yellow

$secretMappings = @()
if ($secrets["dcelectric-qb-token"]) { $secretMappings += "quickbase__token=dcelectric-qb-token:latest" }
if ($secrets["dcelectric-qb-jltoken"]) { $secretMappings += "quickbase__jltoken=dcelectric-qb-jltoken:latest" }
if ($secrets["dcelectric-qb-safetoken"]) { $secretMappings += "quickbase__safetoken=dcelectric-qb-safetoken:latest" }
if ($secrets["dcelectric-qb-traffictoken"]) { $secretMappings += "quickbase__traffictoken=dcelectric-qb-traffictoken:latest" }
if ($secrets["dcelectric-db-connection"]) { $secretMappings += "ConnectionStrings__DefaultConnection=dcelectric-db-connection:latest" }
if ($secrets["dcelectric-azure-blob"]) { $secretMappings += "AzureBlobStorageConnectionString=dcelectric-azure-blob:latest" }
if ($secrets["dcelectric-abcpdf-license"]) { $secretMappings += "ABCpdf__LicenseKey=dcelectric-abcpdf-license:latest" }
if ($secrets["dcelectric-google-creds"]) { $secretMappings += "GOOGLE_APPLICATION_CREDENTIALS_JSON=dcelectric-google-creds:latest" }

if ($secretMappings.Count -gt 0) {
    $secretArg = $secretMappings -join ","
    gcloud run services update $ServiceName `
        --region=$Region `
        --project=$ProjectId `
        --set-secrets=$secretArg
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Secrets Setup Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
