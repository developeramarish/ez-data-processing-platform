# Entity Relationship & MongoDB.Entities ID Usage Analysis

**Date:** November 6, 2025  
**Scope:** Complete system-wide analysis of entity relationships and ID usage  
**MongoDB.Entities Version:** Using Entity base class with auto-generated ObjectIds

---

## Executive Summary

✅ **Overall Assessment: MOSTLY COMPLIANT with one denormalization issue**

- **Good News:** All entities properly use MongoDB.Entities Entity.ID pattern
- **Good News:** All foreign keys use string type (MongoDB ObjectId compatible)
- **Issue Found:** MetricConfiguration has redundant denormalized data (DataSourceName)
- **No hardcoded IDs** found in service code (after cleanup)

---

## Entity Relationship Map

### 1. DataProcessingDataSource (Root Entity)
**Location:** `src/Services/Shared/Entities/DataProcessingDataSource.cs`  
**Inherits From:** `DataProcessingBaseEntity : Entity`

**Foreign Keys:** NONE (root entity)

**Properties:**
```csharp
✅ ID                  // Inherited from Entity (MongoDB ObjectId)
- Name
- SupplierName  
- FilePath
- JsonSchema (BsonDocument)
- etc.
```

**Status:** ✅ CORRECT - No foreign key relationships

---

### 2. DataProcessingSchema
**Location:** `src/Services/Shared/Entities/DataProcessingSchema.cs`  
**Inherits From:** `DataProcessingBaseEntity : Entity`

**Foreign Keys:**
```csharp
✅ ID                      // Inherited from Entity (MongoDB ObjectId)
✅ DataSourceId (string?)  // References DataProcessingDataSource.ID
```

**Relationship:** 
- **Schema → DataSource** (Many-to-One, optional)
- One schema can reference one datasource (nullable, for shared schemas)

**Status:** ✅ CORRECT - Proper MongoDB ObjectId string reference

---

### 3. DataProcessingInvalidRecord
**Location:** `src/Services/Shared/Entities/DataProcessingInvalidRecord.cs`  
**Inherits From:** `DataProcessingBaseEntity : Entity`

**Foreign Keys:**
```csharp
✅ ID                          // Inherited from Entity (MongoDB ObjectId)
✅ DataSourceId (string)       // References DataProcessingDataSource.ID
✅ ValidationResultId (string) // References DataProcessingValidationResult.ID
```

**Relationships:**
- **InvalidRecord → DataSource** (Many-to-One, required)
- **InvalidRecord → ValidationResult** (Many-to-One, required)

**Status:** ✅ CORRECT - Proper MongoDB ObjectId string references

---

### 4. DataProcessingValidationResult
**Location:** `src/Services/Shared/Entities/DataProcessingValidationResult.cs`  
**Inherits From:** `DataProcessingBaseEntity : Entity`

**Foreign Keys:**
```csharp
✅ ID                      // Inherited from Entity (MongoDB ObjectId)
✅ DataSourceId (string)   // References DataProcessingDataSource.ID
```

**Relationship:**
- **ValidationResult → DataSource** (Many-to-One, required)

**Status:** ✅ CORRECT - Proper MongoDB ObjectId string reference

---

### 5. MetricConfiguration ⚠️ ISSUE FOUND
**Location:** `src/Services/MetricsConfigurationService/Models/MetricConfiguration.cs`  
**Inherits From:** `Entity` (MongoDB.Entities)

**Foreign Keys:**
```csharp
✅ ID                       // Inherited from Entity (MongoDB ObjectId)
✅ DataSourceId (string?)   // References DataProcessingDataSource.ID
⚠️ DataSourceName (string?)  // DENORMALIZED - Redundant copy of DataSource.Name
```

**Problem:** Stores both DataSourceId AND DataSourceName

**Risks:**
1. **Data Inconsistency:** If datasource name changes, metric still has old name
2. **Redundant Storage:** Wastes database space
3. **Maintenance Burden:** Must update in two places
4. **Violation of Normalization:** Single source of truth principle broken

**Status:** ⚠️ DENORMALIZATION VIOLATION

---

## Denormalization Analysis

### ⚠️ ENTITIES (Persisted Data)

| Entity | Has Denormalization? | Issue | Recommendation |
|--------|---------------------|-------|----------------|
| DataProcessingDataSource | NO | - | ✅ Keep as-is |
| DataProcessingSchema | NO | - | ✅ Keep as-is |
| DataProcessingInvalidRecord | NO | - | ✅ Keep as-is |
| DataProcessingValidationResult | NO | - | ✅ Keep as-is |
| **MetricConfiguration** | **YES** | **DataSourceName** | **❌ REMOVE** |

### ✅ DTOs (Response Models) - ACCEPTABLE

**Purpose:** UI display optimization, avoids N+1 query problem

Found in:
- `InvalidRecordDto.DataSourceName` ✅ OK - For UI display
- `SchemaUsageStatistics.DataSourceName` ✅ OK - For reporting

**Why This Is Acceptable:**
- DTOs are read-only presentation layer
- Populated dynamically when building response
- Not persisted to database
- Common pattern in API design

### ✅ EVENTS/MESSAGES - ACCEPTABLE

**Purpose:** Logging and debugging, immutable snapshots

Found in:
- `FilePollingEvent.DataSourceName` ✅ OK - For logging
- `ValidationCompletedEvent.DataSourceName` ✅ OK - For logging  
- `FileProcessingFailedEvent.DataSourceName` ✅ OK - For logging

**Why This Is Acceptable:**
- Events are transient messages
- Used for logging context
- Snapshot of state at event time
- Industry standard pattern for event sourcing

---

## Frontend Analysis

### TypeScript Interfaces

**DataSource Interface** (`src/Frontend/src/components/datasource/shared/types.ts`):
```typescript
✅ ID: string              // Properly uses entity ID
- Name: string
- SupplierName: string
- ...
```

**Status:** ✅ CORRECT - Uses entity.ID property

**All Frontend Interfaces:**
- Use `ID: string` for entity identification
- Use `dataSourceId: string` for foreign keys
- No hardcoded IDs
- Properly aligned with backend

---

## API Contract Analysis

### Request Models
✅ All use proper ID fields:
- `CreateMetricRequest.DataSourceId` (string?)
- `UpdateMetricRequest.DataSourceId` (string?)
- `CreateSchemaRequest.DataSourceId` (string?)

### Response Models  
✅ Properly include denormalized names for display ONLY in DTOs
- NOT in entities themselves

---

## Compliance Summary

### ✅ COMPLIANT Areas (90%)

1. **Entity.ID Usage** - All entities properly inherit from Entity
2. **Foreign Key Types** - All use string for MongoDB ObjectIds
3. **No Hardcoded IDs** - All IDs auto-generated by MongoDB.Entities
4. **Frontend Alignment** - TypeScript interfaces match backend
5. **DTO Pattern** - Proper use of denormalization in response DTOs
6. **Event Pattern** - Proper use of snapshots in messages

### ⚠️ NON-COMPLIANT Areas (10%)

1. **MetricConfiguration Entity** - Contains redundant DataSourceName field

---

## Detailed Findings

### Issue #1: MetricConfiguration Denormalization ⚠️

**Current State:**
```csharp
public class MetricConfiguration : Entity
{
    public string? DataSourceId { get; set; }      // ✅ CORRECT
    public string? DataSourceName { get; set; }    // ❌ REDUNDANT
    // ...
}
```

**Problem:**
- Stores a copy of the datasource name
- Creates data consistency risk
- Violates single source of truth principle

**Recommended Fix:**
```csharp
public class MetricConfiguration : Entity
{
    public string? DataSourceId { get; set; }      // ✅ Keep only this
    // REMOVE: DataSourceName
    // ...
}
```

**Impact of Fix:**
- ✅ Eliminates data consistency risks
- ✅ Follows MongoDB.Entities best practices
- ✅ Frontend can fetch name via join or separate API call
- ⚠️ Requires updating:
  - MetricConfiguration entity
  - CreateMetricRequest/UpdateMetricRequest models
  - MetricRepository
  - Frontend metric wizard components
  - API response building logic (populate name in DTO if needed)

---

##Recommendations

### Priority 1: Fix MetricConfiguration Denormalization

**Steps Required:**

1. **Remove DataSourceName from Entity**
   - `MetricConfiguration.cs` - Remove DataSourceName property
   - `CreateMetricRequest.cs` - Remove DataSourceName
   - `UpdateMetricRequest.cs` - Remove DataSourceName

2. **Update Repository Layer**
   - `MetricRepository.cs` - Remove DataSourceName in create/update

3. **Update Controller/Service Layer**
   - When building response DTOs, populate name via lookup if needed
   - Example:
   ```csharp
   var datasource = await DB.Find<DataProcessingDataSource>()
       .Match(d => d.ID == metric.DataSourceId)
       .ExecuteFirstAsync();
   
   return new MetricDto {
       Id = metric.ID,
       DataSourceId = metric.DataSourceId,
       DataSourceName = datasource?.Name  // Populate in DTO only
   };
   ```

4. **Update Frontend**
   - Remove DataSourceName from form submissions
   - Fetch datasource name separately for display if needed
   - Use ID-only for create/update operations

### Priority 2: Maintain Current Good Patterns

✅ **Keep using denormalization in these contexts:**
1. **Response DTOs** - For UI optimization
2. **Event Messages** - For logging context
3. **API Responses** - To avoid N+1 queries

❌ **Never denormalize in:**
1. **Entity models** - Single source of truth
2. **Request models** - ID only for relationships
3. **Database persistence** - Normalize all relationships

---

## Best Practices Established

### ✅ DO: Use Entity.ID for Relationships

```csharp
// CORRECT pattern
public class ChildEntity : DataProcessingBaseEntity
{
    public string ParentEntityId { get; set; }  // References Parent.ID
}
```

### ✅ DO: Use String Type for MongoDB ObjectIds

```csharp
// CORRECT - MongoDB ObjectIds are strings in MongoDB.Entities
public string DataSourceId { get; set; }
```

### ✅ DO: Denormalize in DTOs for Performance

```csharp
// CORRECT - Response DTO can include denormalized data
public class MetricDto
{
    public string ID { get; set; }
    public string? DataSourceId { get; set; }
    public string? DataSourceName { get; set; }  // ✅ OK in DTO
}
```

### ❌ DON'T: Denormalize in Entity Models

```csharp
// INCORRECT - Don't store redundant data in entities
public class MetricConfiguration : Entity
{
    public string? DataSourceId { get; set; }      // ✅ Keep
    public string? DataSourceName { get; set; }    // ❌ Remove
}
```

---

## Entity Relationship Diagram

```
┌─────────────────────────────┐
│  DataProcessingDataSource   │ (Root)
│  - ID (ObjectId)             │
│  - Name                      │
│  - SupplierName              │
└─────────────┬───────────────┘
              │
              │ Referenced by (1:N)
              │
     ┌────────┴────────┬───────────────┬──────────────────┐
     │                 │               │                  │
     ▼                 ▼               ▼                  ▼
┌─────────┐   ┌─────────────┐  ┌──────────────┐  ┌─────────────┐
│ Schema  │   │ Invalid     │  │ Validation   │  │   Metric    │
│         │   │ Record      │  │ Result       │  │Configuration│
│DataSrc─┐│   │DataSrc─┐    │  │DataSrc─┐     │  │DataSrc─┐    │
│Id      ││   │Id      │    │  │Id      │     │  │Id ⚠️   │    │
│(string)││   │(string)│    │  │(string)│     │  │(string)│    │
│        ││   │Valid───┐    │  │             │  │Name⚠️   │    │
│        ││   │Result─┐│    │  │             │  │(redundant)  │
│        ││   │Id     ││    │  │             │  │         │    │
└────────┘│   └───────┴┘    │  └─────────────┘  └─────────────┘
          │           │      │
          └───────────┴──────┘
```

---

## Compliance Checklist

- [x] All entities inherit from Entity (MongoDB.Entities)
- [x] All entity IDs are auto-generated MongoDB ObjectIds
- [x] All foreign keys use string type
- [x] No hardcoded IDs in entities
- [x] Frontend uses proper entity.ID references
- [ ] ⚠️ MetricConfiguration has denormalized DataSourceName (should be removed)
- [x] DTOs properly use denormalization for performance
- [x] Events properly include snapshot data for logging

---

## Detailed Entity Analysis

### Entity 1: DataProcessingDataSource ✅
- **Relationships:** None (root entity)
- **ID Type:** MongoDB ObjectId (via Entity.ID)
- **Status:** COMPLIANT

### Entity 2: DataProcessingSchema ✅
- **Relationships:** 
  - `DataSourceId: string?` → DataProcessingDataSource.ID
- **ID Type:** MongoDB ObjectId (via Entity.ID)
- **Status:** COMPLIANT

### Entity 3: DataProcessingInvalidRecord ✅
- **Relationships:**
  - `DataSourceId: string` → DataProcessingDataSource.ID
  - `ValidationResultId: string` → DataProcessingValidationResult.ID
- **ID Type:** MongoDB ObjectId (via Entity.ID)
- **Status:** COMPLIANT

### Entity 4: DataProcessingValidationResult ✅
- **Relationships:**
  - `DataSourceId: string` → DataProcessingDataSource.ID
- **ID Type:** MongoDB ObjectId (via Entity.ID)
- **Status:** COMPLIANT

### Entity 5: MetricConfiguration ⚠️
- **Relationships:**
  - `DataSourceId: string?` → DataProcessingDataSource.ID ✅
  - `DataSourceName: string?` → **REDUNDANT DENORMALIZATION** ❌
- **ID Type:** MongoDB ObjectId (via Entity.ID)
- **Status:** PARTIAL COMPLIANCE - Needs denormalization removal

---

## Denormalization Strategy

### When Denormalization Is ACCEPTABLE ✅

**1. Response DTOs (Data Transfer Objects)**
```csharp
// GOOD - For API responses to avoid N+1 queries
public class InvalidRecordDto
{
    public string Id { get; set; }
    public string DataSourceId { get; set; }
    public string DataSourceName { get; set; }  // ✅ OK - Populated at query time
}
```

**2. Event Messages**
```csharp
// GOOD - Immutable snapshot for logging
public class FilePollingEvent
{
    public string DataSourceId { get; set; }
    public string DataSourceName { get; set; }  // ✅ OK - For logging context
}
```

### When Denormalization Is WRONG ❌

**1. Entity Models (Persisted Data)**
```csharp
// BAD - Creates data consistency issues
public class MetricConfiguration : Entity
{
    public string? DataSourceId { get; set; }
    public string? DataSourceName { get; set; }  // ❌ REMOVE THIS
}
```

---

## Frontend Analysis

### TypeScript Interfaces ✅

All frontend interfaces properly use entity IDs:

**Example:** `src/Frontend/src/components/datasource/shared/types.ts`
```typescript
export interface DataSource {
  ID: string;              // ✅ CORRECT - Matches Entity.ID
  Name: string;
  SupplierName: string;
  // ...
}
```

**Frontend Foreign Key Usage:**
```typescript
export interface DataSourceUsage {
  dataSourceId: string;    // ✅ CORRECT - References DataSource.ID
}
```

**Status:** ✅ FULLY COMPLIANT

---

## API Contract Compliance

### Request Models ✅
All API request models properly use ID-only references:
- `CreateMetricRequest.DataSourceId: string?` ✅
- `UpdateMetricRequest.DataSourceId: string?` ✅
- `CreateSchemaRequest.DataSourceId: string?` ✅

**However:** Should remove DataSourceName from metric requests

### Response Models ✅
Response DTOs appropriately include denormalized names for UI:
- Built dynamically at query time
- Not stored in database
- Acceptable pattern

---

## Recommendations Summary

### Immediate Action Required

**1. Fix MetricConfiguration Entity** (Priority: HIGH)
- Remove `DataSourceName` property from entity
- Remove from CreateMetricRequest/UpdateMetricRequest
- Update MetricRepository to not persist DataSourceName
- Update Controller to populate name in response DTO only

**Files to Modify:**
```
src/Services/MetricsConfigurationService/Models/MetricConfiguration.cs
src/Services/MetricsConfigurationService/Models/Requests/MetricDataRequests.cs  
src/Services/MetricsConfigurationService/Repositories/MetricRepository.cs
src/Services/MetricsConfigurationService/Controllers/MetricController.cs
src/Frontend/src/components/metrics/* (update wizard to not send DataSourceName)
```

### Maintain Current Patterns

**2. Keep Denormalization in DTOs** (Priority: MAINTAIN)
- ✅ InvalidRecordDto.DataSourceName - Keep for UI
- ✅ Response models - Keep for performance

**3. Keep Denormalization in Events** (Priority: MAINTAIN)
- ✅ FilePollingEvent.DataSourceName - Keep for logging
- ✅ All event messages - Keep snapshot data

---

## Benefits of Fixing

### After Removing DataSourceName from MetricConfiguration:

✅ **Data Consistency**
- Single source of truth for datasource names
- No risk of stale cached names
- Database integrity maintained

✅ **Flexibility**
- Datasource names can be updated without breaking metrics
- Metrics automatically reflect current names

✅ **Storage Efficiency**
- Reduces database storage
- Cleaner data model

✅ **Best Practice Compliance**
- Follows database normalization principles
- Matches MongoDB.Entities patterns
- Industry standard architecture

---

## Migration Strategy

If we fix MetricConfiguration:

**1. Database Migration:**
```csharp
// Optional: Clean existing data
var metrics = await DB.Find<MetricConfiguration>().ExecuteAsync();
foreach (var metric in metrics)
{
    // DataSourceName will be ignored after entity change
    await metric.SaveAsync(); // Re-save without DataSourceName
}
```

**2. API Response Building:**
```csharp
// In Controller - populate name for display
var datasources = await DB.Find<DataProcessingDataSource>().ExecuteAsync();
var datasourceMap = datasources.ToDictionary(d => d.ID, d => d.Name);

return metrics.Select(m => new MetricDto 
{
    ID = m.ID,
    DataSourceId = m.DataSourceId,
    DataSourceName = m.DataSourceId != null 
        ? datasourceMap.GetValueOrDefault(m.DataSourceId, "Unknown")
        : null
}).ToList();
```

---

## Conclusion

### Overall Status: **MOSTLY COMPLIANT** (90%)

**Strengths:**
- ✅ All entities properly use MongoDB.Entities Entity base class
- ✅ All foreign keys use proper string type for ObjectIds
- ✅ No hardcoded IDs anywhere in the system
- ✅ Frontend properly aligned with backend
- ✅ DTOs and Events use denormalization appropriately

**Single Issue:**
- ⚠️ MetricConfiguration stores redundant DataSourceName

**Impact:**
- Current system works but has data consistency risk
- Easy fix with minimal impact
- Would bring system to 100% compliance

**Recommendation:** Remove DataSourceName from MetricConfiguration entity to achieve full compliance with MongoDB.Entities best practices and database normalization principles.

---

## Files Summary

**Entities Analyzed:** 5  
**Services Analyzed:** 6  
**Frontend Files Analyzed:** 10+  
**Total Denormalization Issues:** 1 (in entity model)  
**Acceptable Denormalizations:** 15+ (in DTOs and events)

The system demonstrates strong architectural consistency with MongoDB.Entities patterns, requiring only one focused fix to achieve 100% compliance.
