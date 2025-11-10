# RTL Technical Fields Fix - Progress Status

**Started:** October 27, 2025, 1:30 PM  
**Status:** Partially Complete (Critical P0 fields fixed)  
**Remaining:** Metrics components, DB migration, backend unreversal

---

## ✅ Completed Fixes

### 1. Global CSS Infrastructure ✅
**File:** `src/Frontend/src/App.css`
- Added `.ltr-field` class with !important directives
- Force LTR on technical inputs (direction, text-align, font)
- Code editor LTR enforcement
- Specialized classes (promql-field, formula-field, expression-field, pattern-field, cron-field, path-field, url-field)

### 2. Example Generator Fix ✅
**File:** `src/Frontend/src/utils/schemaExampleGenerator.ts`
- Added `unreverseRTLPattern()` function
- Detects reversed patterns (starting with $ or })
- Reverses string back to correct LTR form
- Proper date format generation based on pattern analysis
- YYYY-MM-DD for ^[0-9]{4}-[0-9]{2}-[0-9]{2}$
- DD-MM-YYYY for ^[0-9]{2}-[0-9]{2}-[0-9]{4}$

### 3. Data Source Connection Fields ✅
**File:** `src/Frontend/src/components/datasource/tabs/ConnectionTab.tsx`

**Fixed fields (7):**
- connectionHost → `className="ltr-field"`
- connectionPath (SFTP/FTP) → `className="ltr-field"`
- connectionUrl (HTTP) → `className="ltr-field"`
- connectionPath (Local) → `className="ltr-field"`
- kafkaBrokers → `className="ltr-field"`
- kafkaTopic → `className="ltr-field"`
- kafkaConsumerGroup → `className="ltr-field"`

### 4. Schedule Cron Expression ✅
**File:** `src/Frontend/src/components/datasource/tabs/ScheduleTab.tsx`

**Fixed fields (1):**
- cronExpression → `className="ltr-field"`

### 5. Cron Helper Dialog ✅
**File:** `src/Frontend/src/components/datasource/CronHelperDialog.tsx`

**Fixed fields (1):**
- Manual cron input → `className="ltr-field"`

---

## ⏳ Remaining Work

### 6. Metrics Configuration Components

**AlertRuleBuilder.tsx:**
- [ ] PromQL expression input field
- [ ] Custom expression textarea

**PromQLExpressionHelperDialog.tsx:**
- [ ] Expression textarea input
- [ ] Function insertion preserves LTR

**EnhancedLabelInput.tsx:**
- [ ] Label name inputs (Prometheus naming)
- [ ] Label value inputs (if technical)

**WizardStepField.tsx:**
- [ ] Field path input (dot notation like revenue.total.amount)

### 7. Schema Pattern Fields

**Challenge:** jsonjoy-builder is a third-party component
**Options:**
1. Wrap entire builder in LTR container
2. Add CSS targeting jsonjoy input fields
3. Post-process pattern values on save

**Recommended:** Add targeted CSS:
```css
.jsonjoy-builder input[type="text"],
.jsonjoy-builder textarea {
  direction: ltr !important;
  text-align: left !important;
}
```

### 8. Database Migration

**Create:** `scripts/fix-rtl-patterns.js`
```javascript
// Unreverse all existing patterns in MongoDB
// Fix schemas, data sources, metrics
```

**Estimate:** 1 hour

### 9. Backend Unreversal

**SchemaValidationService.cs:**
- Add pattern unreversal before validation
- Fix on save/create/update

**DataSourceService.cs:**
- Add cron unreversal before save

**Estimate:** 1-2 hours

### 10. Testing

- Test schema creation with pattern
- Test data source scheduling
- Test metrics with PromQL
- Verify DB values correct

**Estimate:** 1-2 hours

---

## 📊 Progress Summary

**Time Spent:** ~1 hour  
**P0 Critical Fields Fixed:** 9 of ~15 fields (60%)  
**Components Fixed:** 5 of ~10 components (50%)  
**Remaining Estimate:** 4-6 hours

**Ready for Production:** ❌ Not yet - need metrics + DB migration

**Usable for Development:** ✅ Yes - critical cron/connection fields fixed

---

## 🎯 Next Steps

### Option A: Continue Now (4-6 hours)
- Fix remaining metrics components (2 hours)
- Create DB migration script (1 hour)
- Add backend unreversal (1-2 hours)
- Test all workflows (1 hour)

### Option B: Staged Rollout
- Deploy current fixes (cron, connections) - Ready now
- Fix metrics components - Next session (2 hours)
- DB migration + backend - After metrics (2-3 hours)

### Option C: New Focused Task
- Mark current work complete
- Create new task: "Complete RTL Fix - Metrics + DB + Backend"
- Implement in focused 4-6 hour block

---

## ✅ What Works Now

With current fixes:
- ✅ Cron expressions display LTR
- ✅ File paths display LTR
- ✅ URLs display LTR
- ✅ Kafka connection strings display LTR
- ✅ Example generator creates correct format dates
- ✅ Pattern unreversal works in generator

**Impact:** Data sources can be configured with correct cron schedules!

---

## ⚠️ What Still Needs Fix

Without remaining fixes:
- ⚠️ Metrics PromQL expressions may still reverse
- ⚠️ Schema pattern fields in jsonjoy builder still RTL
- ⚠️ Database may contain reversed patterns (need cleanup)
- ⚠️ Backend doesn't unreverse on save (patterns saved reversed)

**Impact:** Metrics alerts won't work, some schemas invalid

---

**Status:** 60% of RTL fix complete  
**Recommendation:** Continue with Option A to complete all fixes

**Next Files to Modify:**
1. AlertRuleBuilder.tsx
2. PromQLExpressionHelperDialog.tsx  
3. EnhancedLabelInput.tsx
4. WizardStepField.tsx
5. DB migration script
6. SchemaValidationService.cs
7. DataSourceService.cs
