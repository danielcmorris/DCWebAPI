# Google Cloud Run Deployment Script for DCElectricWebAPI (PowerShell)
#
# Prerequisites:
#   1. Install Google Cloud SDK: https://cloud.google.com/sdk/docs/install
#   2. Authenticate: gcloud auth login
#   3. Set project: gcloud config set project YOUR_PROJECT_ID
#   4. Enable APIs: gcloud services enable run.googleapis.com artifactregistry.googleapis.com
#
# Usage:
#   .\deploy-cloudrun.ps1 [-ProjectId PROJECT_ID] [-Region REGION] [-ServiceName SERVICE_NAME]

param(
    [string]$ProjectId = $env:GOOGLE_CLOUD_PROJECT,
    [string]$Region = "us-west1",
    [string]$ServiceName = "dcelectric-webapi",
    [string]$ImageName = "dcelectricwebapi",
    [string]$Repository = "docker-repo"
)

$ErrorActionPreference = "Stop"

# Validate project ID
if ([string]::IsNullOrEmpty($ProjectId)) {
    Write-Error "Please provide ProjectId via -ProjectId parameter or GOOGLE_CLOUD_PROJECT env var"
    exit 1
}

# Full image path in Artifact Registry
$ImagePath = "${Region}-docker.pkg.dev/${ProjectId}/${Repository}/${ImageName}"
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Google Cloud Run Deployment" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Project:  $ProjectId"
Write-Host "Region:   $Region"
Write-Host "Service:  $ServiceName"
Write-Host "Image:    $ImagePath"
Write-Host "==========================================" -ForegroundColor Cyan

# Step 1: Create Artifact Registry repository if it doesn't exist
Write-Host ""
Write-Host "[1/5] Ensuring Artifact Registry repository exists..." -ForegroundColor Yellow
$repoExists = $false
try {
    $ErrorActionPreference = "SilentlyContinue"
    $result = gcloud artifacts repositories describe $Repository --location=$Region --project=$ProjectId 2>&1
    if ($LASTEXITCODE -eq 0) {
        $repoExists = $true
        Write-Host "Repository already exists." -ForegroundColor Green
    }
    $ErrorActionPreference = "Stop"
} catch {
    $ErrorActionPreference = "Stop"
}

if (-not $repoExists) {
    Write-Host "Creating Artifact Registry repository..." -ForegroundColor Yellow
    gcloud artifacts repositories create $Repository `
        --repository-format=docker `
        --location=$Region `
        --project=$ProjectId `
        --description="Docker repository for DC Electric Web API"
}

# Step 2: Configure Docker to authenticate with Artifact Registry
Write-Host ""
Write-Host "[2/5] Configuring Docker authentication..." -ForegroundColor Yellow
gcloud auth configure-docker "${Region}-docker.pkg.dev" --quiet

# Step 3: Build the Docker image
Write-Host ""
Write-Host "[3/5] Building Docker image..." -ForegroundColor Yellow
docker build -t "${ImagePath}:latest" -t "${ImagePath}:${Timestamp}" .

# Step 4: Push to Artifact Registry
Write-Host ""
Write-Host "[4/5] Pushing image to Artifact Registry..." -ForegroundColor Yellow
docker push "${ImagePath}:latest"

# Step 5: Deploy to Cloud Run
Write-Host ""
Write-Host "[5/5] Deploying to Cloud Run..." -ForegroundColor Yellow
gcloud run deploy $ServiceName `
    --image="${ImagePath}:latest" `
    --region=$Region `
    --project=$ProjectId `
    --platform=managed `
    --port=8080 `
    --memory=2Gi `
    --cpu=2 `
    --timeout=300 `
    --concurrency=80 `
    --min-instances=0 `
    --max-instances=10 `
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production" `
    --allow-unauthenticated

# Get the service URL
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
$ServiceUrl = gcloud run services describe $ServiceName --region=$Region --project=$ProjectId --format='value(status.url)'
Write-Host "Service URL: $ServiceUrl"
Write-Host ""
Write-Host "Test with:"
Write-Host "  curl ${ServiceUrl}/swagger/index.html"
Write-Host ""
Write-Host "NOTE: You need to set environment variables for secrets." -ForegroundColor Yellow
Write-Host "Use Cloud Run console or run setup-cloudrun-secrets.ps1"
