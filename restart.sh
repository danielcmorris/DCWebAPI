#!/bin/bash
# Stop any running DCElectricWebAPI dev server, then start it again.
set -e
cd "$(dirname "$0")"
pkill -f "DCElectricWebAPI" 2>/dev/null && echo "Stopped running instance." || echo "No running instance found."
exec ./start.sh
