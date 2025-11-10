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

These are synchronized with frontend dropdowns.

### Complex Schema Features

Generated schemas use:
- ✅ Nested objects (3-4 levels deep)
- ✅ Array validation (minItems, maxItems)
- ✅ Pattern matching (regex for phone, email, IDs)
- ✅ Conditional schemas (if/then/else)
- ✅ Enum validations
- ✅ Format validations (date-time, email, uri)
- ✅ Range constraints (minimum, maximum)
- ✅ String length (minLength, maxLength)
- ✅ Required fields
- ✅ Additional properties control

### Output Example

```
═══════════════════════════════════════════════
    🎯 Demo Data Generator for EZ Platform    
═══════════════════════════════════════════════

Mode: FULL RESET
Seed: 42 (deterministic)

✓ Connected to MongoDB

[1/7] 🗑️  Resetting all database collections...
  ✓ Reset DataSources (deleted all, count: 0)
  ✓ Reset Schemas (deleted all, count: 0)
  ✓ Reset Metrics (deleted all, count: 0)
  ✓ Reset ValidationResults (deleted all, count: 0)
  ✓ Reset InvalidRecords (deleted all, count: 0)
  ✅ All collections reset successfully

[2/7] 📊 Generating 20 datasources...
  ✓ Created: מערכת מכירות מרכזית (מכירות)
  ✓ Created: נתוני לקוחות CRM (כספים)
  ... (18 more)
  ✅ Generated 20 datasources

[3/7] 📋 Generating complex JSON schemas...
  ✓ Schema for: מערכת מכירות מרכזית
  ... (19 more)
  ✅ Generated 20 complex schemas

[4/7] 📈 Generating 20 global metrics...
  ✓ ספירת רשומות כוללת
  ✓ ממוצע סכומים יומי
  ... (18 more)
  ✅ Generated 20 global metrics

[5/7] 📊 Generating datasource-specific metrics...
  ✓ 3 metrics for: מערכת מכירות מרכזית
  ... (19 more)
  ✅ Generated 65 datasource-specific metrics

[6/7] 🚨 Generating alerts for metrics...
  ✅ Generated 25 alerts

[7/7] 📊 Generation Summary:
  ✅ 20 DataSources
  ✅ 20 Schemas
  ✅ 85 Metrics (global + datasource-specific)
  ✅ 25 Metrics with alerts

═══════════════════════════════════════════════
  ✨ Demo data generation completed successfully!
═══════════════════════════════════════════════
```

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

**Show Help:**
```bash
dotnet run
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

### Output Example

```
═══════════════════════════════════════════════
    🚀 Service Orchestrator - START MODE
═══════════════════════════════════════════════

🛑 Stopping all running services...
  ✓ Stopped process on port 5001
  ✓ Stopped process on port 3000
✅ All services stopped

[1/7] Starting DataSourceManagement...
  → Started DataSourceManagement (port 5001)
  ⏳ Waiting for DataSourceManagement to be ready........... ✓ (3.2s)

[2/7] Starting MetricsConfiguration...
  → Started MetricsConfiguration (port 7002)
  ⏳ Waiting for MetricsConfiguration to be ready........ ✓ (2.8s)

... (5 more services)

[7/7] Starting Frontend...
  → Started Frontend (port 3000)
  ⏳ Waiting for Frontend to be ready................. ✓ (15.3s)

═══════════════════════════════════════════════
  ✅ All services started!
═══════════════════════════════════════════════

📊 Dashboard: http://localhost:3000
🔧 API Docs:  http://localhost:5001/swagger

Press Ctrl+C to exit (services will continue running)
```

---

## 📋 Complete Workflow

### Initial Setup (One Time)

1. **Ensure MongoDB is running:**
```bash
# Check if MongoDB is running
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
# In ServiceOrchestrator directory
dotnet run stop
```

---

## 🧪 For E2E/Integration Testing

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

### Add More Test Data Without Reset

```bash
cd tools/DemoDataGenerator
dotnet run -- --incremental
```

---

## 📁 Project Structure

```
tools/
├── DemoDataGenerator/
│   ├── DemoDataGenerator.csproj
│   ├── Program.cs
│   ├── Models/
│   │   └── HebrewCategories.cs (shared constants)
│   ├── Services/
│   │   └── DatabaseResetService.cs
│   └── Generators/
│       └── AllGenerators.cs (all data generators)
│
├── ServiceOrchestrator/
│   ├── ServiceOrchestrator.csproj
│   ├── Program.cs
│   └── Services/
│       └── OrchestratorServices.cs (process mgmt, health checks)
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

# Demo...
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

## 📚 Related Documentation

- **100-PERCENT-COMPLIANCE-IMPLEMENTATION.md** - MongoDB.Entities compliance
- **ENTITY-RELATIONSHIP-ANALYSIS-REPORT.md** - Entity relationship analysis
- **SERVICES-AUDIT-REPORT.md** - Service architecture audit
- **SCHEMA-PERSISTENCE-FIX-SUMMARY.md** - Technical fixes applied

---

## 🎉 Summary

These tools provide:
- ✅ Professional demo data generation
- ✅ Automated service lifecycle management
- ✅ Support for E2E/integration testing
- ✅ Deterministic, reproducible environments
- ✅ Hebrew-language support throughout

Perfect for demos, development, and automated testing!
