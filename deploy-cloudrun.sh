#!/bin/bash
# Google Cloud Run Deployment Script for DCElectricWebAPI
#
# Prerequisites:
#   1. Install Google Cloud SDK: https://cloud.google.com/sdk/docs/install
#   2. Authenticate: gcloud auth login
#   3. Set project: gcloud config set project YOUR_PROJECT_ID
#   4. Enable APIs: gcloud services enable run.googleapis.com artifactregistry.googleapis.com
#
# Usage:
#   ./deploy-cloudrun.sh [--project PROJECT_ID] [--region REGION] [--service SERVICE_NAME]

set -e

# Default configuration - customize these
PROJECT_ID="${GOOGLE_CLOUD_PROJECT:-your-project-id}"
REGION="${GOOGLE_CLOUD_REGION:-us-west1}"
SERVICE_NAME="dcelectric-webapi"
IMAGE_NAME="dcelectricwebapi"
REPOSITORY="docker-repo"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --project)
            PROJECT_ID="$2"
            shift 2
            ;;
        --region)
            REGION="$2"
            shift 2
            ;;
        --service)
            SERVICE_NAME="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Validate project ID
if [ "$PROJECT_ID" == "your-project-id" ]; then
    echo "Error: Please set PROJECT_ID via --project flag or GOOGLE_CLOUD_PROJECT env var"
    exit 1
fi

# Full image path in Artifact Registry
IMAGE_PATH="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPOSITORY}/${IMAGE_NAME}"

echo "=========================================="
echo "Google Cloud Run Deployment"
echo "=========================================="
echo "Project:  $PROJECT_ID"
echo "Region:   $REGION"
echo "Service:  $SERVICE_NAME"
echo "Image:    $IMAGE_PATH"
echo "=========================================="

# Step 1: Create Artifact Registry repository if it doesn't exist
echo ""
echo "[1/5] Ensuring Artifact Registry repository exists..."
gcloud artifacts repositories describe $REPOSITORY \
    --location=$REGION \
    --project=$PROJECT_ID 2>/dev/null || \
gcloud artifacts repositories create $REPOSITORY \
    --repository-format=docker \
    --location=$REGION \
    --project=$PROJECT_ID \
    --description="Docker repository for DC Electric Web API"

# Step 2: Configure Docker to authenticate with Artifact Registry
echo ""
echo "[2/5] Configuring Docker authentication..."
gcloud auth configure-docker ${REGION}-docker.pkg.dev --quiet

# Step 3: Build the Docker image
echo ""
echo "[3/5] Building Docker image..."
docker build -t ${IMAGE_PATH}:latest -t ${IMAGE_PATH}:$(date +%Y%m%d-%H%M%S) .

# Step 4: Push to Artifact Registry
echo ""
echo "[4/5] Pushing image to Artifact Registry..."
docker push ${IMAGE_PATH}:latest

# Step 5: Deploy to Cloud Run
echo ""
echo "[5/5] Deploying to Cloud Run..."
gcloud run deploy $SERVICE_NAME \
    --image=${IMAGE_PATH}:latest \
    --region=$REGION \
    --project=$PROJECT_ID \
    --platform=managed \
    --port=8080 \
    --memory=2Gi \
    --cpu=2 \
    --timeout=300 \
    --concurrency=80 \
    --min-instances=0 \
    --max-instances=10 \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production" \
    --allow-unauthenticated

# Get the service URL
echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
SERVICE_URL=$(gcloud run services describe $SERVICE_NAME --region=$REGION --project=$PROJECT_ID --format='value(status.url)')
echo "Service URL: $SERVICE_URL"
echo ""
echo "Test with:"
echo "  curl ${SERVICE_URL}/swagger/index.html"
echo ""
echo "NOTE: You need to set environment variables for secrets."
echo "Use Cloud Run console or:"
echo "  gcloud run services update $SERVICE_NAME --region=$REGION \\"
echo "    --set-env-vars=\"quickbase__token=YOUR_TOKEN\" \\"
echo "    --set-env-vars=\"ConnectionStrings__DefaultConnection=YOUR_CONN_STRING\""
