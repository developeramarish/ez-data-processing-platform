# Demo & Testing Infrastructure - Implementation Plan

## Overview
Creating two C# console applications for professional demo/testing infrastructure.

## Tool 1: DemoDataGenerator

**Purpose:** Generate deterministic, comprehensive demo data

**Location:** `tools/DemoDataGenerator/`

**Key Features:**
- Fully deterministic (seed=42)
- Complete database reset before generation
- 20 datasources with complex schemas
- 20 global metrics + 60 datasource-specific metrics
- Hebrew categories synchronized with frontend
- Comprehensive logging

**Files to Create:**
1. ✅ DemoDataGenerator.csproj
2. ✅ Models/HebrewCategories.cs
3. ✅ Services/DatabaseResetService.cs
4. Services/LoggingService.cs
5. Generators/DataSourceGenerator.cs
6. Generators/SchemaGenerator.cs (most complex)
7. Generators/GlobalMetricGenerator.cs
8. Generators/DatasourceMetricGenerator.cs
9. Generators/AlertGenerator.cs
10. Program.cs (CLI with --incremental option)

**Estimated Size:** ~1500 lines total

## Tool 2: ServiceOrchestrator

**Purpose:** Manage service lifecycle and dependencies

**Location:** `tools/ServiceOrchestrator/`

**Commands:**
- `start` - Start all services in order
- `stop` - Stop all services
- `restart` - Stop then start
- `status` - Check service health

**Files to Create:**
1. ServiceOrchestrator.csproj
2. Models/ServiceConfig.cs
3. Services/ProcessManager.cs
4. Services/HealthChecker.cs
5. Services/StartupSequencer.cs
6. Program.cs (command router)

**Estimated Size:** ~800 lines total

## Implementation Strategy

Given the scope (2300+ lines of code), I recommend:

**Option A:** Create complete applications file-by-file (current approach)
- Pro: Full visibility of each component
- Con: Many file operations, may hit context limits

**Option B:** Create as 2-3 large combined files
- Pro: Fewer operations
- Con: Less modularity

**Option C:** Create skeleton now, detailed implementation in next session
- Pro: Clear architecture established
- Con: Requires continuation

**My Recommendation:** Continue with Option A but combine some files where logical. For example:
- Combine all generators in one file initially
- Can split later if needed
- Focus on working code over perfect modularity

Shall I proceed with creating the remaining DemoDataGenerator files?
