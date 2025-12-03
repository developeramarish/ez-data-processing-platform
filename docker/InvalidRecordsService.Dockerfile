# InvalidRecordsService Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "./"]
COPY ["src/Services/InvalidRecordsService/InvalidRecordsService.csproj", "InvalidRecordsService/"]
COPY ["src/Services/Shared/DataProcessing.Shared.csproj", "Shared/"]
RUN dotnet restore "InvalidRecordsService/InvalidRecordsService.csproj"

COPY ["src/Services/InvalidRecordsService/", "InvalidRecordsService/"]
COPY ["src/Services/Shared/", "Shared/"]

WORKDIR "/src/InvalidRecordsService"
RUN dotnet build "InvalidRecordsService.csproj" -c Release -o /app/build
RUN dotnet publish "InvalidRecordsService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5006
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:5006/health || exit 1
ENTRYPOINT ["dotnet", "InvalidRecordsService.dll"]
