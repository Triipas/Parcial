# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project file
COPY src/PortalAcademico/*.csproj ./src/PortalAcademico/
RUN dotnet restore src/PortalAcademico/PortalAcademico.csproj

# Copy source code
COPY src/PortalAcademico/ ./src/PortalAcademico/

# Build and publish
WORKDIR /app/src/PortalAcademico
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser /app
USER appuser

# Expose port (Render asigna dinámicamente)
EXPOSE 8080

# Environment
ENV ASPNETCORE_URLS=http://+:8080

# Start application
ENTRYPOINT ["dotnet", "PortalAcademico.dll"]