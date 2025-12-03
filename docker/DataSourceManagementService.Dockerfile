# DataSourceManagementService Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Services/DataSourceManagementService/DataProcessing.DataSourceManagement.csproj", "DataSourceManagementService/"]
COPY ["src/Services/Shared/DataProcessing.Shared.csproj", "Shared/"]
RUN dotnet restore "DataSourceManagementService/DataProcessing.DataSourceManagement.csproj"

COPY ["src/Services/DataSourceManagementService/", "DataSourceManagementService/"]
COPY ["src/Services/Shared/", "Shared/"]

WORKDIR "/src/DataSourceManagementService"
RUN dotnet publish "DataProcessing.DataSourceManagement.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5001
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:5001/health || exit 1
ENTRYPOINT ["dotnet", "DataProcessing.DataSourceManagement.dll"]
