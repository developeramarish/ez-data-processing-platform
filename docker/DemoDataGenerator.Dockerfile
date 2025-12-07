# Demo Data Generator Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY Directory.Packages.props ./
COPY Directory.Build.props ./
COPY global.json ./
COPY tools/DemoDataGenerator/DemoDataGenerator.csproj tools/DemoDataGenerator/
COPY src/Services/Shared/DataProcessing.Shared.csproj src/Services/Shared/
COPY src/Services/MetricsConfigurationService/MetricsConfigurationService.csproj src/Services/MetricsConfigurationService/

# Restore dependencies
RUN dotnet restore "tools/DemoDataGenerator/DemoDataGenerator.csproj"

# Copy source code
COPY tools/DemoDataGenerator/ tools/DemoDataGenerator/
COPY src/Services/Shared/ src/Services/Shared/
COPY src/Services/MetricsConfigurationService/ src/Services/MetricsConfigurationService/

# Build and publish
WORKDIR /src/tools/DemoDataGenerator
RUN dotnet publish "DemoDataGenerator.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DemoDataGenerator.dll"]
