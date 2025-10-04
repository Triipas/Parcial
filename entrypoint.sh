#!/bin/bash
set -e

echo "Running database migrations..."
dotnet PortalAcademico.dll --migrate || true

echo "Starting application..."
exec dotnet PortalAcademico.dll