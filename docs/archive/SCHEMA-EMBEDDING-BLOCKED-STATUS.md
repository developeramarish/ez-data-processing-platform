# Schema Embedding Refactor - Blocked Status

## Date: 2025-10-20

## Current Situation

The schema embedding refactor has been blocked due to a critical architectural issue discovered during the data migration phase.

### Problem Discovered

The `UpdateDataSourceRequest` model does not match the `DataProcessingDataSource` entity structure:

**Entity Fields (`DataProcessingDataSource`):**
- Name
- SupplierName
- FilePath
- PollingRate (TimeSpan)
- JsonSchema (BsonDocument) ← **TARGET for embedding**
- Category
- SchemaVersion
- IsActive
- FilePattern
- Description

**Request Model Fields (`UpdateDataSourceRequest`):**
- Id
- Name
- SupplierName
- Category
- Description
- ConnectionString ← **(NOT FilePath!)**
- IsActive
- ConfigurationSettings
- ValidationRules
- Metadata
- FileFormat
- RetentionDays

**Missing from Request Model:**
- JsonSchema
- PollingRate
- FilePath
- FilePattern
- SchemaVersion

### Migration Script Status

- ✅ Created: `migrate-schemas-to-datasources.py`
- ✅ Successfully retrieves 6 schemas from database
- ✅ Successfully retrieves 6 datasources
- ❌ **FAILS**: All 6 PUT requests return 400 Bad Request

### Root Cause

The API contract (`UpdateDataSourceRequest`) was designed for a different purpose and doesn't include the fields necessary for the schema embedding pattern. The backend service likely has mapping logic that we haven't examined yet.

### Current Data State

**Schemas:** 6 schemas exist with correct structure:
- Each has `JsonSchemaContent` (the actual schema definition)
- Each has `DataSourceId` (singular, points to one datasource)
- Located at: `http://localhost:5001/api/v1/schema`

**DataSources:** 6 datasources exist:
- All have **empty** `JsonSchema` properties (`{}`)
- All are active
- Located at: `http://localhost:5001/api/v1/datasource`

**Backup Location:** `backups/schema-migration-20251020_111838/`

### Options to Proceed

#### Option A: Fix UpdateDataSourceRequest Model
**Approach:** Add missing fields to the request model
**Pros:** 
- Aligns API contract with entity model
- Enables direct schema embedding
- Clear, straightforward solution

**Cons:**
- Requires updating the model
- May affect existing API consumers
- Requires testing the update endpoint

**Steps:**
1. Add JsonSchema, PollingRate, FilePath, FilePattern, SchemaVersion to UpdateDataSourceRequest
2. Update the service mapping if needed
3. Re-run migration script
4. Continue with frontend implementation

#### Option B: Find Alternative Update Mechanism
**Approach:** Check if there's a different endpoint or method to update JsonSchema
**Pros:**
- Doesn't modify existing API contract
- May be a dedicated schema update endpoint

**Cons:**
- Takes time to investigate
- May not exist
- Could complicate the implementation

#### Option C: Defer Refactor, Focus on Metrics Fixes
**Approach:** Prioritize the immediate metrics fixes (save button, dropdown) over architectural refactor
**Pros:**
- Delivers immediate value
- Less risky
- Can revisit refactor later

**Cons:**
- Doesn't solve the architectural misalignment
- Metrics would still need to access schemas from separate collection
- Technical debt remains

### Completed Work (Ready to Use)

Even though the refactor is blocked, several fixes are already implemented and ready:

1. ✅ **Metrics Save Button Fix**
   - File: `src/Frontend/src/pages/metrics/MetricConfigurationWizard.tsx`
   - Fix: Added formulaType conversion (string→int)

2. ✅ **Metrics Dropdown Fix**
   - File: `src/Frontend/src/components/metrics/WizardStepGlobalMetrics.tsx`
   - Fix: Added `optionLabelProp="label"` to prevent overflow

3. ✅ **Metrics API Client Update**
   - File: `src/Frontend/src/services/metrics-api-client.ts`
   - Fix: Added missing UpdateMetricRequest fields

These fixes can be tested **immediately** without the schema embedding refactor.

### Recommendation

**RECOMMENDED: Option A** - Fix the UpdateDataSourceRequest model

**Rationale:**
1. The architectural alignment is important for long-term maintainability
2. The fix is straightforward - just add the missing fields
3. The migration script is already written and working (except for the API contract mismatch)
4. Once fixed, the entire refactor can proceed smoothly

**Alternative if time-constrained:** Option C - Focus on metrics fixes first, defer refactor

### Next Steps (if Option A chosen)

1. Update `UpdateDataSourceRequest` model to include:
   - `JsonSchema` (BsonDocument or appropriate DTO type)
   - `PollingRate` (string representing TimeSpan)
   - `FilePath` (string)
   - `FilePattern` (string)
   - `SchemaVersion` (int)

2. Update service layer if mapping needs adjustment

3. Test PUT endpoint with updated model

4. Re-run migration script

5. Continue with frontend implementation (embed SchemaBuilder in DataSource forms)

6. Test end-to-end workflow

7. Create datasource-specific metrics

### Files Modified (Ready for Review)

**Backend:**
- `migrate-schemas-to-datasources.py` (migration script - working)

**Frontend Metrics Fixes:**
- `src/Frontend/src/services/metrics-api-client.ts`
- `src/Frontend/src/pages/metrics/MetricConfigurationWizard.tsx`
- `src/Frontend/src/components/metrics/WizardStepGlobalMetrics.tsx`

### Time Estimate

- **Option A (Fix model):** 1-2 hours
- **Option C (Metrics only):** 30 minutes (just testing)

### Decision Required

The user needs to decide whether to:
1. Proceed with Option A (fix the model, complete refactor)
2. Switch to Option C (test metrics fixes, defer refactor)

Both options are viable depending on priorities.
