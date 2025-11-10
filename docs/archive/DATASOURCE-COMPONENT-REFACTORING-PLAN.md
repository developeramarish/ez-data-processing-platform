# DataSource Component Refactoring Plan - Option A

## Current State
- **DataSourceFormEnhanced.tsx**: ~550 lines (already 50% reduced from 1,100)
- **DataSourceEditEnhanced.tsx**: ~650 lines (already 59% reduced from 1,600)  
- **DataSourceDetailsEnhanced.tsx**: ~440 lines
- **Total**: ~1,640 lines

## Target State (After Full Refactoring)
Break into smaller, focused components of ~50-150 lines each.

## Shared Utilities (✅ COMPLETED)
1. **types.ts** - All TypeScript interfaces
2. **helpers.ts** - Utility functions (humanizeCron, frequencyToCron, buildConnectionString, etc.)
3. **constants.ts** - Options and default values

## Form Tab Components (TODO)

### src/Frontend/src/components/datasource/tabs/BasicInfoTab.tsx (~80 lines)
**Props:**
```typescript
interface BasicInfoTabProps {
  form: FormInstance;
  t: (key: string) => string;
}
```

**Content:**
- Name, Supplier Name fields
- Category, Retention Days fields
- Description field
- IsActive switch

---

### src/Frontend/src/components/datasource/tabs/ConnectionTab.tsx (~120 lines)
**Props:**
```typescript
interface ConnectionTabProps {
  form: FormInstance;
  t: (key: string) => string;
  connectionType: string;
  testingConnection: boolean;
  connectionTestResult: 'success' | 'failed' | null;
  onTestConnection: () => void;
}
```

**Content:**
- Connection type selector
- Conditional fields based on type (SFTP/FTP/HTTP/Local)
- Test connection button with result display

---

### src/Frontend/src/components/datasource/tabs/FileSettingsTab.tsx (~100 lines)
**Props:**
```typescript
interface FileSettingsTabProps {
  form: FormInstance;
  t: (key: string) => string;
  fileType: string;
}
```

**Content:**
- File type selector
- CSV-specific fields (delimiter, hasHeaders)
- Excel-specific fields (sheetName, hasHeaders)
- Encoding selector

---

### src/Frontend/src/components/datasource/tabs/SchemaTab.tsx (~50 lines)
**Props:**
```typescript
interface SchemaTabProps {
  jsonSchema: JSONSchema;
  onChange: (schema: JSONSchema) => void;
}
```

**Content:**
- Alert message
- SchemaBuilderNew component wrapper

---

### src/Frontend/src/components/datasource/tabs/ScheduleTab.tsx (~100 lines)
**Props:**
```typescript
interface ScheduleTabProps {
  form: FormInstance;
  t: (key: string) => string;
  scheduleFrequency: string;
  cronExpression?: string;
  onOpenCronHelper: () => void;
}
```

**Content:**
- Schedule frequency selector
- Schedule enabled switch
- Custom cron expression input (conditional)
- Cron humanization display
- Examples alert

---

### src/Frontend/src/components/datasource/tabs/ValidationTab.tsx (~60 lines)
**Props:**
```typescript
interface ValidationTabProps {
  form: FormInstance;
  t: (key: string) => string;
}
```

**Content:**
- Skip invalid records switch
- Max errors allowed input

---

### src/Frontend/src/components/datasource/tabs/NotificationsTab.tsx (~60 lines)
**Props:**
```typescript
interface NotificationsTabProps {
  form: FormInstance;
  t: (key: string) => string;
}
```

**Content:**
- Notify on success switch
- Notify on failure switch
- Recipients input field

---

## Details Tab Components (TODO)

### src/Frontend/src/components/datasource/details/
- **BasicInfoDetailsTab.tsx** (~60 lines)
- **ConnectionDetailsTab.tsx** (~60 lines)
- **FileDetailsTab.tsx** (~60 lines)
- **SchemaDetailsTab.tsx** (~80 lines)
- **ScheduleDetailsTab.tsx** (~60 lines)
- **ValidationDetailsTab.tsx** (~60 lines)
- **NotificationsDetailsTab.tsx** (~60 lines)

---

## Refactored Main Containers

### DataSourceFormEnhanced.tsx (~150 lines after refactoring)
```typescript
import { BasicInfoTab } from '../../components/datasource/tabs/BasicInfoTab';
import { ConnectionTab } from '../../components/datasource/tabs/ConnectionTab';
// ... etc

// Only orchestration logic:
- State management
- Form submission
- API calls
- Tab rendering with imported components
```

### DataSourceEditEnhanced.tsx (~180 lines after refactoring)
```typescript
// Same as create form + data fetching/loading logic
```

### DataSourceDetailsEnhanced.tsx (~150 lines after refactoring)
```typescript
// Header + Statistics + Tab orchestration with imported detail components
```

---

## Benefits After Refactoring

**File Size Reduction:**
- Form containers: ~150-180 lines (vs current 550-650)
- Each tab component: ~50-120 lines (easy to understand)
- Total new files: ~14 components + 3 utilities = 17 files
- Average file size: ~80 lines

**Maintainability:**
✅ Single responsibility per component  
✅ Easy to test individual tabs  
✅ Clear separation of concerns  
✅ Reusable components between create/edit  

**Developer Experience:**
✅ Faster IDE loading  
✅ Easier to find specific functionality  
✅ Reduced cognitive load  
✅ Better code organization

---

## Implementation Steps

### Phase 1: Shared Utilities (✅ DONE)
- [x] Create types.ts
- [x] Create helpers.ts
- [x] Create constants.ts

### Phase 2: Form Tab Components (TODO)
- [ ] Create BasicInfoTab.tsx
- [ ] Create ConnectionTab.tsx
- [ ] Create FileSettingsTab.tsx
- [ ] Create SchemaTab.tsx
- [ ] Create ScheduleTab.tsx
- [ ] Create ValidationTab.tsx
- [ ] Create NotificationsTab.tsx

### Phase 3: Refactor Create Form (TODO)
- [ ] Update DataSourceFormEnhanced.tsx to use tab components
- [ ] Test create workflow

### Phase 4: Refactor Edit Form (TODO)
- [ ] Update DataSourceEditEnhanced.tsx to use tab components
- [ ] Test edit workflow

### Phase 5: Details Tab Components (TODO)
- [ ] Create BasicInfoDetailsTab.tsx
- [ ] Create ConnectionDetailsTab.tsx
- [ ] Create FileDetailsTab.tsx
- [ ] Create SchemaDetailsTab.tsx
- [ ] Create ScheduleDetailsTab.tsx
- [ ] Create ValidationDetailsTab.tsx
- [ ] Create NotificationsDetailsTab.tsx

### Phase 6: Refactor Details (TODO)
- [ ] Update DataSourceDetailsEnhanced.tsx to use detail tab components
- [ ] Test details view

### Phase 7: Final Testing (TODO)
- [ ] Compile all files
- [ ] Test create datasource workflow
- [ ] Test edit datasource workflow
- [ ] Test details view
- [ ] Verify all tabs work correctly
- [ ] Verify schema embedding works
- [ ] Verify configuration merge works

---

## Estimated Time
- **Phase 2-4 (Forms)**: 1-1.5 hours
- **Phase 5-6 (Details)**: 30-45 minutes
- **Phase 7 (Testing)**: 30 minutes
- **Total**: ~2-3 hours

---

## Recommendation

**Current Implementation Status:**
- ✅ Schema embedding working with SchemaBuilderNew
- ✅ Files reduced by 47% (from 3,090 → 1,640 lines)
- ✅ Configuration merge bug fixed
- ✅ Schema display in details view added
- ✅ All TypeScript errors resolved

**Suggested Approach:**
1. **Test current implementation first** - Verify create/edit/view workflows work correctly
2. **If working well** - Proceed with Option A refactoring as separate task
3. **If issues found** - Fix bugs before refactoring

This ensures we don't refactor broken code and can validate that component extraction doesn't break functionality.

**Would you like to:**
- A) Test current implementation, then refactor (RECOMMENDED)
- B) Proceed with refactoring immediately
