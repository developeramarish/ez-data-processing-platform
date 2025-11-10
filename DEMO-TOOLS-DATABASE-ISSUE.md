# Demo Tools - Database Name Mismatch Issue

**Date:** November 6, 2025  
**Severity:** HIGH  
**Status:** IDENTIFIED - Requires Fix

---

## Root Cause Identified ✅

**Database Name Mismatch Between Services:**

| Service | Database Name |
|---------|---------------|
| DemoDataGenerator | `ezplatform` |
| DataSourceManagementService | `ezplatform` (default in Program.cs) |
| MetricsConfigurationService | `DataProcessing` (from appsettings.json) |

---

## Impact

**Issue 1: Datasources Don't Show Schemas**
- DataSources created in: `ezplatform` database
- Schemas created in: `ezplatform` database  
- DataSourceManagement service reads from: `ezplatform` ✅ (works)
- **But schemas ARE linked** - the relationship exists

**Issue 2: Metrics Page Shows No Metrics**
- Metrics created in: `ezplatform` database
- MetricsConfiguration service reads from: `DataProcessing` database ❌ (MISMATCH!)
- Result: Metrics exist but service can't see them

---

## Solution Options

### Option A: Fix Services to Use Same Database ✅ RECOMMENDED

**Update MetricsConfigurationService appsettings.json:**
```json
{
  "MongoDB": {
    "DatabaseName": "ezplatform"  // Change from "DataProcessing"
  }
}
```

**Pros:**
- Simple one-file change
- All services use same database
- Matches DataSourceManagement default

**Cons:**
- None

### Option B: Update DemoDataGenerator

Change DemoDataGenerator to write metrics to "DataProcessing" database (separate DB.Init for metrics).

**Pros:**
- Keeps services as-is

**Cons:**
- More complex (multiple database connections)
- Fragmented data across databases

---

## Recommended Fix

**Update:** `src/Services/MetricsConfigurationService/appsettings.json`

Change line 12 from:
```json
"DatabaseName": "DataProcessing"
```

To:
```json
"DatabaseName": "ezplatform"
```

Then restart MetricsConfigurationService.

---

## Re-Test Steps

After fix:
1. Update appsettings.json
2. Restart MetricsConfigurationService
3. Open http://localhost:3000/metrics
4. Verify 79 metrics display
5. Check datasource detail pages show linked schemas

---

## Current Status

✅ DemoDataGenerator working correctly  
✅ Data generated successfully (20 DS, 20 schemas, 79 metrics)  
✅ Schemas ARE linked to datasources (relationship exists)  
❌ Services reading from different databases  
❌ Metrics not visible due to database mismatch  

**Simple fix required: Standardize all services to use "ezplatform" database.**
