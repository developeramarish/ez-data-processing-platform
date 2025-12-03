# MetricsConfigurationService Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Services/MetricsConfigurationService/MetricsConfigurationService.csproj", "MetricsConfigurationService/"]
COPY ["src/Services/Shared/DataProcessing.Shared.csproj", "Shared/"]
RUN dotnet restore "MetricsConfigurationService/MetricsConfigurationService.csproj"

COPY ["src/Services/MetricsConfigurationService/", "MetricsConfigurationService/"]
COPY ["src/Services/Shared/", "Shared/"]

WORKDIR "/src/MetricsConfigurationService"
RUN dotnet publish "MetricsConfigurationService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5002
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:5002/health || exit 1
ENTRYPOINT ["dotnet", "MetricsConfigurationService.dll"]
