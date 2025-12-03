# FileProcessorService Dockerfile
# Multi-stage build for .NET 10 service

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Services/FileProcessorService/DataProcessing.FileProcessor.csproj", "FileProcessorService/"]
COPY ["src/Services/Shared/DataProcessing.Shared.csproj", "Shared/"]

RUN dotnet restore "FileProcessorService/DataProcessing.FileProcessor.csproj"

COPY ["src/Services/FileProcessorService/", "FileProcessorService/"]
COPY ["src/Services/Shared/", "Shared/"]

WORKDIR "/src/FileProcessorService"
RUN dotnet build "DataProcessing.FileProcessor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataProcessing.FileProcessor.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5008

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:5008/health || exit 1

ENTRYPOINT ["dotnet", "DataProcessing.FileProcessor.dll"]
