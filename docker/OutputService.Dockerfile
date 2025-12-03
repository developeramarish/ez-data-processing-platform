# OutputService Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "./"]
COPY ["src/Services/OutputService/DataProcessing.Output.csproj", "OutputService/"]
COPY ["src/Services/Shared/DataProcessing.Shared.csproj", "Shared/"]
RUN dotnet restore "OutputService/DataProcessing.Output.csproj"

COPY ["src/Services/OutputService/", "OutputService/"]
COPY ["src/Services/Shared/", "Shared/"]

WORKDIR "/src/OutputService"
RUN dotnet build "DataProcessing.Output.csproj" -c Release -o /app/build
RUN dotnet publish "DataProcessing.Output.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5009
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:5009/health || exit 1
ENTRYPOINT ["dotnet", "DataProcessing.Output.dll"]
