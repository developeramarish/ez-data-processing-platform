# SchedulingService Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/SchedulingService/DataProcessing.Scheduling.csproj", "SchedulingService/"]
COPY ["src/Services/Shared/DataProcessing.Shared.csproj", "Shared/"]
RUN dotnet restore "SchedulingService/DataProcessing.Scheduling.csproj"

COPY ["src/Services/SchedulingService/", "SchedulingService/"]
COPY ["src/Services/Shared/", "Shared/"]

WORKDIR "/src/SchedulingService"
RUN dotnet build "DataProcessing.Scheduling.csproj" -c Release -o /app/build
RUN dotnet publish "DataProcessing.Scheduling.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5004
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:5004/health || exit 1
ENTRYPOINT ["dotnet", "DataProcessing.Scheduling.dll"]
