# Data Processing Platform - Development Environment Setup Script
# This script sets up the complete development environment for Windows

param(
    [switch]$SkipDocker,
    [switch]$SkipKubernetes,
    [switch]$SkipNode
)

Write-Host "🚀 Setting up Data Processing Platform Development Environment" -ForegroundColor Green

# Check prerequisites
Write-Host "`n📋 Checking Prerequisites..." -ForegroundColor Cyan

# Check .NET 9.0 SDK
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -like "9.*") {
        Write-Host "✅ .NET SDK $dotnetVersion found" -ForegroundColor Green
    } else {
        Write-Host "❌ .NET 9.0 SDK required. Current version: $dotnetVersion" -ForegroundColor Red
        Write-Host "Please download from: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

# Check Docker
if (-not $SkipDocker) {
    try {
        $dockerVersion = docker --version
        Write-Host "✅ Docker found: $dockerVersion" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Docker not found. Install Docker Desktop for development" -ForegroundColor Yellow
        Write-Host "Download from: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    }
}

# Check Node.js for frontend development
if (-not $SkipNode) {
    try {
        $nodeVersion = node --version
        Write-Host "✅ Node.js found: $nodeVersion" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Node.js not found. Required for frontend development" -ForegroundColor Yellow
        Write-Host "Download from: https://nodejs.org/" -ForegroundColor Yellow
    }
}

# Check Kubectl
if (-not $SkipKubernetes) {
    try {
        $kubectlVersion = kubectl version --client --short
        Write-Host "✅ kubectl found: $kubectlVersion" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  kubectl not found. Install for Kubernetes development" -ForegroundColor Yellow
        Write-Host "Install via: choco install kubernetes-cli" -ForegroundColor Yellow
    }
}

# Restore .NET packages
Write-Host "`n📦 Restoring .NET packages..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ .NET packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to restore .NET packages" -ForegroundColor Red
    exit 1
}

# Build solution
Write-Host "`n🔨 Building solution..." -ForegroundColor Cyan
dotnet build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Solution built successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Solution build failed" -ForegroundColor Red
    exit 1
}

# Create development configuration templates
Write-Host "`n⚙️  Creating development configuration templates..." -ForegroundColor Cyan

$devSettings = @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "Kafka": "localhost:9092",
    "Elasticsearch": "http://localhost:9200"
  },
  "CorrelationId": {
    "HeaderName": "X-Correlation-Id",
    "GenerateIfMissing": true
  },
  "Prometheus": {
    "Port": 9090,
    "Endpoint": "/metrics"
  },
  "OpenTelemetry": {
    "Jaeger": {
      "Endpoint": "http://localhost:14268/api/traces"
    }
  },
  "Environment": "Development"
}
'@

# Create appsettings.Development.json for each service
$services = @(
    "src/Services/DataSourceManagementService",
    "src/Services/SchedulingService", 
    "src/Services/FilesReceiverService",
    "src/Services/ValidationService",
    "src/Services/DataSourceChatService"
)

foreach ($service in $services) {
    $settingsPath = "$service/appsettings.Development.json"
    if (-not (Test-Path $settingsPath)) {
        $devSettings | Out-File -FilePath $settingsPath -Encoding utf8
        Write-Host "✅ Created $settingsPath" -ForegroundColor Green
    }
}

# Create docker-compose for development infrastructure
Write-Host "`n🐳 Creating development infrastructure setup..." -ForegroundColor Cyan

$dockerCompose = @'
version: '3.8'
services:
  mongodb:
    image: mongo:7.0
    container_name: dataprocessing-mongodb-dev
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: password
    volumes:
      - mongodb_data:/data/db
    networks:
      - dataprocessing-dev

  kafka:
    image: confluentinc/cp-kafka:7.5.0
    container_name: dataprocessing-kafka-dev
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
    depends_on:
      - zookeeper
    networks:
      - dataprocessing-dev

  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
    container_name: dataprocessing-zookeeper-dev
    ports:
      - "2181:2181"
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    networks:
      - dataprocessing-dev

  elasticsearch:
    image: elasticsearch:8.10.0
    container_name: dataprocessing-elasticsearch-dev
    ports:
      - "9200:9200"
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    volumes:
      - elasticsearch_data:/usr/share/elasticsearch/data
    networks:
      - dataprocessing-dev

  prometheus:
    image: prom/prometheus:v2.47.0
    container_name: dataprocessing-prometheus-dev
    ports:
      - "9090:9090"
    volumes:
      - ./deploy/monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
    networks:
      - dataprocessing-dev

  grafana:
    image: grafana/grafana:10.1.0
    container_name: dataprocessing-grafana-dev
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana_data:/var/lib/grafana
    networks:
      - dataprocessing-dev

volumes:
  mongodb_data:
  elasticsearch_data:
  grafana_data:

networks:
  dataprocessing-dev:
    driver: bridge
'@

$dockerComposePath = "deploy/docker/docker-compose.dev.yml"
$dockerCompose | Out-File -FilePath $dockerComposePath -Encoding utf8
Write-Host "✅ Created $dockerComposePath" -ForegroundColor Green

# Create README with next steps
Write-Host "`n📄 Creating development guide..." -ForegroundColor Cyan

$readme = @'
# Data Processing Platform - Development Environment

## Quick Start

The development environment has been set up successfully!

### Prerequisites Verified
* .NET 9.0 SDK
* Docker (optional but recommended)
* Node.js (for frontend development)
* kubectl (for Kubernetes development)

### Project Structure
```
src/Services/
├── Shared/                          # Common utilities and base classes
├── DataSourceManagementService/     # REST API for data source CRUD
├── SchedulingService/              # Quartz.NET job scheduling
├── FilesReceiverService/           # File ingestion and processing
├── ValidationService/              # Schema validation engine
└── DataSourceChatService/          # AI-powered chat interface
```

### Development Infrastructure

Start the development infrastructure:
```bash
# Start all development services
docker-compose -f deploy/docker/docker-compose.dev.yml up -d

# Check services are running
docker-compose -f deploy/docker/docker-compose.dev.yml ps
```

Services will be available at:
* MongoDB: localhost:27017 (admin/password)
* Kafka: localhost:9092
* Elasticsearch: http://localhost:9200
* Prometheus: http://localhost:9090
* Grafana: http://localhost:3000 (admin/admin)

### Build and Run Services

Build all services:
```bash
dotnet build
```

Run individual services:
```bash
# Data Source Management Service
cd src/Services/DataSourceManagementService
dotnet run

# Scheduling Service  
cd src/Services/SchedulingService
dotnet run

# Files Receiver Service
cd src/Services/FilesReceiverService
dotnet run

# Validation Service
cd src/Services/ValidationService
dotnet run

# Chat Service
cd src/Services/DataSourceChatService
dotnet run
```

### Next Steps

1. Review Implementation Guidelines: Check shrimp-rules.md for development standards
2. Check Task Progress: See project-progress.md for current status
3. Follow Implementation Plan: Use detailed-implementation-plan.md for task sequence

### Development Tasks Ready to Start

Based on the implementation plan, these foundation tasks can start immediately:

1. MongoDB Integration & Base Entities (Task 2)
2. MassTransit & Kafka Infrastructure (Task 3)  
3. Monitoring & Observability Infrastructure (Task 4)

These can be developed in parallel by different team members.

### Hebrew UI Development

The frontend will be developed with Hebrew RTL support. Reference web_ui_mockups_hebrew.html for UI patterns.

### Support & Documentation

* Development Guidelines: shrimp-rules.md
* Progress Tracking: project-progress.md
* Implementation Plan: detailed-implementation-plan.md
* Requirements: data_processing_prd.md

Happy coding!
'@

$readme | Out-File -FilePath "README-DEV.md" -Encoding utf8
Write-Host "✅ Created README-DEV.md" -ForegroundColor Green

Write-Host "`n🎉 Development environment setup complete!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Review README-DEV.md for development guidelines" -ForegroundColor White
Write-Host "2. Start development infrastructure: docker-compose -f deploy/docker/docker-compose.dev.yml up -d" -ForegroundColor White
Write-Host "3. Begin foundation tasks as outlined in detailed-implementation-plan.md" -ForegroundColor White
Write-Host "`n✨ Happy coding!" -ForegroundColor Green
