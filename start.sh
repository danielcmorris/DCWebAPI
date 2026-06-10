#!/bin/bash
# Start the DCElectricWebAPI dev server locally.
# Runs on https://localhost:7001 and http://localhost:5012 (see Properties/launchSettings.json).
set -e
cd "$(dirname "$0")"
export ASPNETCORE_ENVIRONMENT=Development
exec dotnet run --launch-profile DCElectricWebAPI
