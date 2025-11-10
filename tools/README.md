# EZ Platform Demo & Testing Tools

Professional C# tools for demo data generation and service orchestration.

## Overview

This directory contains two essential tools:

1. **DemoDataGenerator** - Generates comprehensive, deterministic demo data
2. **ServiceOrchestrator** - Manages service lifecycle and dependencies

---

## 🎯 DemoDataGenerator

### Purpose
Generate realistic, comprehensive demo data for the EZ Platform including datasources, schemas, metrics, and alerts.

### Key Features
- ✅ **Deterministic** - Same data every time (seed=42)
- ✅ **Complete Reset** - Cleans all database collections before generation
- ✅ **Comprehensive** - 20 datasources, 80+ metrics, complex schemas
- ✅ **Hebrew Content** - All descriptions and categories in Hebrew
- ✅ **Complex Schemas** - Uses all JSON Schema 2020-12 features
- ✅ **Incremental Mode** - Optional add-on generation without reset

### What It Generates

| Type | Count | Details |
|------|-------|---------|  
| DataSources | 20 | With varied polling rates, categories |
| JSON Schemas | 20 | 3-4 levels nested, all validations |
| Global Metrics | 20 | Mix of simple + complex PromQL |
| Datasource Metrics | 60+ | 2-4 per datasource |
| Alerts | ~25 | On 30% of metrics |

### Usage

**Full Reset + Generate:**
```bash
cd tools/DemoDataGenerator
dotnet run
```

**Incremental (Add More Data):**
```bash
dotnet run -- --incremental
```

### Categories (Synchronized)

All datasources use one of 10 predefined Hebrew categories:
- מכירות (Sales)
- כספים (Finance)
- משאבי אנוש (HR)
- מלאי (Inventory)
- שירות לקוחות (Customer Service)
- שיווק (Marketing)
- לוגיסטיקה (Logistics)
- תפעול (Operations)
- מחקר ופיתוח (R&D)
- רכש (Procurement)

---

## 🚀 ServiceOrchestrator

### Purpose
Manage the lifecycle of all EZ Platform services with proper dependency ordering and health checking.

### Commands

**Start All Services:**
```bash
cd tools/ServiceOrchestrator
dotnet run start
```

**Stop All Services:**
```bash
dotnet run stop
```

**Restart All Services:**
```bash
dotnet run restart
```

### What It Does

**Start Sequence:**
1. Stops any existing services on ports 5001-5006, 7002, 3000
2. Starts services in dependency order:
   - DataSourceManagementService (5001)
   - MetricsConfigurationService (7002)
   - ValidationService (5003)
   - SchedulingService (5004)
   - FilesReceiverService (5005)
   - InvalidRecordsService (5006)
   - Frontend (3000)
3. Waits for each service to become healthy
4. Provides access URLs

**Features:**
- ✅ Process detection and termination
- ✅ Health check waiting (max 30s per service)
- ✅ Visible terminal windows for debugging
- ✅ Graceful shutdown
- ✅ Error handling and logging

---

## 📋 Complete Workflow

### Initial Setup (One Time)

1. **Ensure MongoDB is running:**
```bash
mongosh
```

2. **Build the tools:**
```bash
cd tools/DemoDataGenerator
dotnet build

cd ../ServiceOrchestrator
dotnet build
```

### Demo/Testing Workflow

**Step 1: Generate Demo Data**
```bash
cd tools/DemoDataGenerator
dotnet run
```

**Step 2: Start All Services**
```bash
cd ../ServiceOrchestrator
dotnet run start
```

**Step 3: Access the Platform**
- Frontend: http://localhost:3000
- Swagger: http://localhost:5001/swagger

**Step 4: When Done**
```bash
cd tools/ServiceOrchestrator
dotnet run stop
```

---

## 🧘 For E2E/Integration Testing

### Reset and Regenerate Between Tests

```bash
# Clean slate
cd tools/DemoDataGenerator
dotnet run

# Start services
cd ../ServiceOrchestrator
dotnet run start

# Run your tests...

# Cleanup
dotnet run stop
```

---

## 📁 Project Structure

```
tools/
├── DemoDataGenerator/
│   ├── DemoDataGenerator.csproj
│   ├── Program.cs
│   ├── Models/
│   │   └── HebrewCategories.cs
│   ├── Services/
│   │   └── DatabaseResetService.cs
│   └── Generators/
│       └── AllGenerators.cs
│
├── ServiceOrchestrator/
│   ├── ServiceOrchestrator.csproj
│   ├── Program.cs
│   └── Services/
│       └── OrchestratorServices.cs
│
└── README.md (this file)
```

---

## 🔧 Technical Details

### DemoDataGenerator

**Dependencies:**
- MongoDB.Entities (23.0.0)
- Newtonsoft.Json (13.0.3)
- DataProcessing.Shared (project reference)

**Database Collections:**
- `DataProcessing.DataSource`
- `DataProcessing.Schema`
- `DataProcessing.MetricConfiguration`
- `DataProcessing.ValidationResult`
- `DataProcessing.InvalidRecord`

### ServiceOrchestrator

**Dependencies:**
- System.Diagnostics.Process

**Port Mapping:**
- 5001 - DataSourceManagementService
- 5003 - ValidationService
- 5004 - SchedulingService
- 5005 - FilesReceiverService
- 5006 - InvalidRecordsService
- 7002 - MetricsConfigurationService
- 3000 - Frontend (React)

---

## ⚠️ Important Notes

1. **MongoDB Required** - Both tools require MongoDB running on localhost:27017
2. **Deterministic Data** - Same seed (42) produces identical data every time
3. **Hebrew Support** - Ensure console supports UTF-8 encoding
4. **Service Dependencies** - ServiceOrchestrator starts services in correct order
5. **Health Checks** - Each service must respond to /health endpoint

---

## 🎓 Usage Scenarios

### Scenario 1: Daily Development
```bash
# Morning - start services
cd tools/ServiceOrchestrator && dotnet run start

# Develop...

# Evening - stop services
dotnet run stop
```

### Scenario 2: Demo Preparation
```bash
# Fresh demo data
cd tools/DemoDataGenerator && dotnet run

# Start for demo
cd ../ServiceOrchestrator && dotnet run start
```

### Scenario 3: Integration Testing
```bash
# Before tests
cd tools/DemoDataGenerator && dotnet run
cd ../ServiceOrchestrator && dotnet run start

# Run tests

# After tests
cd tools/ServiceOrchestrator && dotnet run stop
```

---

## 🎉 Summary

These tools provide:
- ✅ Professional demo data generation
- ✅ Automated service lifecycle management
- ✅ Support for E2E/integration testing
- ✅ Deterministic, reproducible environments
- ✅ Hebrew-language support throughout

Perfect for demos, development, and automated testing!

**Note:** Full implementation files available in local codebase at `c:/Users/UserC/source/repos/EZ/tools/`
