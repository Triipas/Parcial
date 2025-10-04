
set -o errexit

cd src/PortalAcademico

# Restore dependencies
dotnet restore

# Run migrations
dotnet ef database update --no-build || true

# Build and publish
dotnet publish -c Release -o out