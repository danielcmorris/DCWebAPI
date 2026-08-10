-- Key/value application settings store (QuickBase host/app id/token, etc.).
-- The QuickBase token is stored unencrypted for now, by decision 2026-08-10.
--
-- MUST BE RUN BY A PRIVILEGED ROLE: dc-website cannot CREATE in schema public.

CREATE TABLE IF NOT EXISTS settings (
    key         VARCHAR(100) PRIMARY KEY,
    value       TEXT NOT NULL DEFAULT '',
    updateddate TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedbyid INTEGER
);

GRANT SELECT, INSERT, UPDATE ON settings TO "dc-website";
