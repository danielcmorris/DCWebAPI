-- Migration: Add GcsURL column to Report table for Google Cloud Storage dual-write support
-- Date: 2026-02-18
-- Description: Stores the GCS URL alongside the Azure BlobURL during migration period

ALTER TABLE Report ADD COLUMN IF NOT EXISTS GcsURL VARCHAR(500);

-- Optional: Add index if you need to query by GCS URL
-- CREATE INDEX IF NOT EXISTS idx_report_gcsurl ON Report(GcsURL) WHERE GcsURL IS NOT NULL;
