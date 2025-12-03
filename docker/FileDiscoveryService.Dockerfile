# FileDiscoveryService Dockerfile
# Multi-stage build for .NET 10 service

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy .NET configuration files
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]

# Copy project files
COPY ["src/Services/FileDiscoveryService/DataProcessing.FileDiscovery.csproj", "FileDiscoveryService/"]
COPY ["src/Services/Shared/DataProcessing.Shared.csproj", "Shared/"]

# Restore dependencies
RUN dotnet restore "FileDiscoveryService/DataProcessing.FileDiscovery.csproj"

# Copy source code
COPY ["src/Services/FileDiscoveryService/", "FileDiscoveryService/"]
COPY ["src/Services/Shared/", "Shared/"]

# Build
WORKDIR "/src/FileDiscoveryService"
RUN dotnet build "DataProcessing.FileDiscovery.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "DataProcessing.FileDiscovery.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 5007

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:5007/health || exit 1

ENTRYPOINT ["dotnet", "DataProcessing.FileDiscovery.dll"]
