# Cron Seconds Support Implementation Plan

## Overview
Add seconds resolution support to cron expressions throughout the system.

## Current State
- Backend uses Quartz.NET `SimpleSchedule` with `TimeSpan PollingRate`
- Frontend shows 5-field cron expressions (minute minimum)
- Minimum interval: 1 minute

## Target State
- Backend uses Quartz.NET `CronSchedule` with 6-field expressions
- Support seconds resolution: `second minute hour day month dayOfWeek`
- Example: `*/30 * * * * *` = every 30 seconds

## Implementation Steps

### Phase 1: Backend Changes

#### 1.1 Update DataSource Entity
**File:** `src/Services/Shared/Entities/DataProcessingDataSource.cs`
- Add new property: `public string? CronExpression { get; set; }`
- Keep `PollingRate` for backward compatibility
- Add helper property to get cron or convert TimeSpan

#### 1.2 Update SchedulingManager
**File:** `src/Services/SchedulingService/Services/SchedulingManager.cs`
- Replace `WithSimpleSchedule` with `WithCronSchedule`
- Use 6-field Quartz cron format
- Add validation for 6-field expressions
- Update min interval check (allow seconds)

#### 1.3 Update DataSourceService
**File:** `src/Services/DataSourceManagementService/Services/DataSourceService.cs`
- Handle CronExpression in create/update
- Maintain backward compatibility with PollingRate

### Phase 2: Frontend Changes

#### 2.1 Update CronHelperDialog
**File:** `src/Frontend/src/components/datasource/CronHelperDialog.tsx`
- Add seconds field to visual builder
- Add 6-field patterns (every 5/10/15/30/45 seconds)
- Update pattern library
- Update input validation

#### 2.2 Update Constants
**File:** `src/Frontend/src/components/datasource/shared/constants.ts`
- Add seconds-based frequency options

#### 2.3 Update Helper Functions
**File:** `src/Frontend/src/components/datasource/shared/helpers.ts`
- ✅ Already using cronstrue (supports 6 fields)
- Update frequencyToCron for seconds options

### Phase 3: Data Migration

#### 3.1 Create Migration Script
**File:** `scripts/migrate-timespan-to-cron.js`
- Convert existing PollingRate TimeSpan to CronExpression
- Mapping:
  - `00:05:00` → `0 */5 * * * *`
  - `00:02:00` → `0 */2 * * * *`
  - `00:10:00` → `0 */10 * * * *`

## Backward Compatibility Strategy

**Hybrid Approach:**
1. Add `CronExpression` field (nullable)
2. Keep `PollingRate` field
3. Scheduling logic: Use `CronExpression` if present, else fallback to `PollingRate`
4. Gradually migrate existing data sources

## Testing Plan

1. Test 6-field cron with seconds
2. Test backward compatibility with TimeSpan
3. Test cronstrue Hebrew output for 6 fields
4. Verify minimum interval validation

## Rollout Plan

1. Deploy backend changes (supports both formats)
2. Run data migration script
3. Deploy frontend changes
4. Monitor for issues
