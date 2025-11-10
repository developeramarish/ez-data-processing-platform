# Schema Embedding UI Integration - Proposal for Approval

## Date: 2025-10-20

## Overview

This document shows the proposed UI changes to integrate SchemaBuilder into DataSource forms. The SchemaBuilder component will be used **as-is** (no changes to the component itself), just moved to a new location.

---

## Current UI vs Proposed UI

### Current DataSource Create Form

**Structure:**
```
DataSourceFormEnhanced
├── Tab 1: מידע בסיסי (Basic Information)
├── Tab 2: הגדרות חיבור (Connection Settings)
├── Tab 3: הגדרות קובץ (File Settings)
├── Tab 4: תזמון (Schedule)
├── Tab 5: כללי אימות (Validation Rules)
└── Tab 6: התראות (Notifications)
```

**Validation Rules Tab (Current):**
- Shows informational alerts about linking to schemas
- Tells users to go to Schema Management to link
- Contains skipInvalidRecords switch
- Contains maxErrorsAllowed input

### Proposed DataSource Create Form

**Structure:**
```
DataSourceFormEnhanced
├── Tab 1: מידע בסיסי (Basic Information)
├── Tab 2: הגדרות חיבור (Connection Settings)
├── Tab 3: הגדרות קובץ (File Settings)
├── Tab 4: ⭐ הגדרת Schema (Schema Definition) ← NEW!
├── Tab 5: תזמון (Schedule)
├── Tab 6: כללי אימות (Validation Rules)
└── Tab 7: התראות (Notifications)
```

**New Schema Tab (Tab 4):**
- Contains the **entire SchemaBuilder component** exactly as-is
- Visual editor for adding fields
- JSON editor with tree/text modes
- Real-time preview
- Validation testing

**Updated Validation Tab (Tab 6):**
- Remove the alerts about linking schemas (no longer needed)
- Keep skipInvalidRecords and maxErrorsAllowed
- Simplified since schema is defined in Tab 4

---

## Detailed UI Changes

### 1. DataSourceFormEnhanced - New Schema Tab

**Location:** After "File Settings" tab, before "Schedule" tab

**Content:** Embedded SchemaBuilder component with these sections:

#### Section 1: Schema Basic Info (Top of tab)
```
┌─────────────────────────────────────────────────────────┐
│ שם Schema: [sales_data_v1        ]                      │
│ שם תצוגה:  [נתוני מכירות        ]                      │
│ תיאור:     [Schema לעסקאות מכירה...]                   │
└─────────────────────────────────────────────────────────┘
```

#### Section 2: Field Builder Tabs
```
┌─────────────────────────────────────────────────────────┐
│ Tabs:  [👁 עורך חזותי] [💻 עורך JSON] [✓ אימות]      │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  Visual Tab:                                              │
│  ┌─────────────────────────────────────────┐             │
│  │ [+ הוסף שדה חדש]                       │             │
│  ├─────────────────────────────────────────┤             │
│  │ ☑ transaction_id │ עסקה │ string │ חובה │ [ערוך] [מחק]│             │
│  │ ☑ amount        │ סכום │ number │ חובה │ [ערוך] [מחק]│             │
│  │   customer_name │ לקוח │ string │      │ [ערוך] [מחק]│             │
│  └─────────────────────────────────────────┘             │
│                                                           │
│  JSON Tab:                                                │
│  ┌─────────────────────────────────────────┐             │
│  │ [עץ (Tree)] [קוד (Text)]               │             │
│  │ {                                        │             │
│  │   "$schema": "...",                      │             │
│  │   "type": "object",                      │             │
│  │   "properties": {...}                    │             │
│  │ }                                        │             │
│  └─────────────────────────────────────────┘             │
└─────────────────────────────────────────────────────────┘
```

### 2. Tab Sequence Change

**Before:**
1. Basic Info → 2. Connection → 3. File → 4. Schedule → 5. Validation → 6. Notifications

**After:**
1. Basic Info → 2. Connection → 3. File → **4. Schema** → 5. Schedule → 6. Validation → 7. Notifications

### 3. Updated Validation Tab Content

**Remove these alerts:**
```diff
- Alert: "קישור מקור נתונים ל-Schema"
- Alert: "אתה יכול לקשר מקור נתונים זה ל-Schema קיים..."
- Alert: "קישור Schema" with link to /schema page
```

**Keep:**
```
✓ skipInvalidRecords switch
✓ maxErrorsAllowed input
```

**Add brief note:**
```
ℹ Schema מוגדר בלשונית "הגדרת Schema" למעלה
```

---

## Component Integration Details

### SchemaBuilder Component Usage

**Current location:**
- `src/Frontend/src/pages/schema/SchemaBuilder.tsx` (standalone page)

**New usage:**
- Import and use **as-is** in DataSourceFormEnhanced
- **No changes** to SchemaBuilder component itself
- Only changes to the **parent form** to:
  1. Import the component
  2. Add it to a new tab
  3. Capture its output (JSON schema string)
  4. Pass it to the API when creating datasource

### Code Structure (Conceptual)

```tsx
// DataSourceFormEnhanced.tsx

import SchemaBuilder from '../schema/SchemaBuilder';  // ← Import

const DataSourceFormEnhanced: React.FC = () => {
  const [schemaJson, setSchemaJson] = useState<string>('{}');

  return (
    <Tabs>
      {/* ... existing tabs ... */}
      
      {/* NEW TAB */}
      <TabPane tab={<span><FileTextOutlined /> הגדרת Schema</span>} key="schema">
        <SchemaBuilder 
          onChange={(json) => setSchemaJson(json)}  // ← Capture output
          embedded={true}                            // ← Flag for embedded mode
        />
      </TabPane>
      
      {/* ... rest of tabs ... */}
    </Tabs>
  );
};
```

**Note:** May need to add an `embedded` prop and `onChange` prop to SchemaBuilder, but keep all UI/logic intact.

---

## Form Submission Changes

### Current Request Body

```json
{
  "name": "מקור נתונים חדש",
  "supplierName": "ספק ABC",
  "category": "financial",
  "connectionString": "/data/files",
  "configurationSettings": "{...}"
}
```

### Proposed Request Body

```json
{
  "name": "מקור נתונים חדש",
  "supplierName": "ספק ABC",
  "category": "financial",
  "connectionString": "/data/files",
  "filePath": "/data/files",           // ← NEW
  "pollingRate": "00:05:00",           // ← NEW
  "filePattern": "*.json",             // ← NEW
  "jsonSchema": {                      // ← NEW - From SchemaBuilder
    "$schema": "https://json-schema.org/draft/2020-12/schema",
    "type": "object",
    "properties": {
      "transactionId": {...},
      "amount": {...}
    },
    "required": ["transactionId", "amount"]
  },
  "schemaVersion": 1,                  // ← NEW
  "configurationSettings": "{...}"
}
```

---

## Navigation Changes

### Current Navigation (App.tsx)

```tsx
<Menu.Item key="datasources" icon={<DatabaseOutlined />}>
  <Link to="/datasources">מקורות נתונים</Link>
</Menu.Item>
<Menu.Item key="schema" icon={<FileTextOutlined />}>  ← REMOVE THIS
  <Link to="/schema">ניהול Schema</Link>
</Menu.Item>
<Menu.Item key="metrics" icon={<LineChartOutlined />}>
  <Link to="/metrics">הגדרות מדדים</Link>
</Menu.Item>
```

### Proposed Navigation

```tsx
<Menu.Item key="datasources" icon={<DatabaseOutlined />}>
  <Link to="/datasources">מקורות נתונים</Link>
</Menu.Item>
<!-- Schema Management menu item REMOVED -->
<Menu.Item key="metrics" icon={<LineChartOutlined />}>
  <Link to="/metrics">הגדרות מדדים</Link>
</Menu.Item>
```

---

## User Workflow Changes

### Before (2-Step Process)

1. **Create DataSource** → Fill basic info, connection, etc.
2. **Navigate to Schema Management** → Create/link schema to datasource

### After (1-Step Process)

1. **Create DataSource** → Fill basic info, connection, **define schema in same form**, done!

---

## Edit Form Changes

### DataSourceEditEnhanced

**Same integration as create form:**
- Add Schema tab with SchemaBuilder
- Load existing JsonSchema from datasource into SchemaBuilder
- Allow editing schema inline
- Save updated schema back to datasource

**Tab sequence:**
1. Basic Info → 2. Connection → 3. File → **4. Schema (editable)** → 5. Schedule → 6. Validation → 7. Notifications

---

## Visual Mockup

### Create DataSource - Schema Tab

```
┌────────────────────────────────────────────────────────────────────┐
│ יצירת מקור נתונים                                        [חזור] [שמור] │
├────────────────────────────────────────────────────────────────────┤
│ Tabs: [מידע בסיסי] [הגדרות חיבור] [הגדרות קובץ] 【הגדרת Schema】 │
│       [תזמון] [כללי אימות] [התראות]                               │
├────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ℹ הגדר את JSON Schema עבור מקור נתונים זה                      │
│    Schema יאמת את הקבצים שיתקבלו ממקור זה                         │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────┐       │
│  │ שם Schema:    [user_transactions                  ]      │       │
│  │ שם תצוגה:     [עסקאות משתמשים                   ]      │       │
│  │ תיאור:        [Schema לעסקאות משתמשים מהמערכת...     ]  │       │
│  └──────────────────────────────────────────────────────────┘       │
│                                                                      │
│  Tabs: 【👁 עורך חזותי】 [💻 עורך JSON] [✓ אימות]           │
│  ┌──────────────────────────────────────────────────────────┐       │
│  │ [+ הוסף שדה חדש]                                        │       │
│  ├──────────────────────────────────────────────────────────┤       │
│  │ ☑ transaction_id  מזהה עסקה   string  ⚠ חובה         │       │
│  │     Min: 10 | Max: 10 | Pattern: ^TRX[0-9]{7}$          │       │
│  │                                         [ערוך] [מחק]     │       │
│  ├──────────────────────────────────────────────────────────┤       │
│  │ ☑ amount         סכום          number  ⚠ חובה         │       │
│  │     Min: 0 | Max: 999999                                 │       │
│  │                                         [ערוך] [מחק]     │       │
│  ├──────────────────────────────────────────────────────────┤       │
│  │   customer_name  שם לקוח       string                   │       │
│  │     Min: 2 | Max: 100                                    │       │
│  │                                         [ערוך] [מחק]     │       │
│  └──────────────────────────────────────────────────────────┘       │
│                                                                      │
│  ✓ 3 שדות | 2 חובה | 2 עם אימות                                 │
│                                                                      │
└────────────────────────────────────────────────────────────────────┘
```

---

## Proposed Code Changes

### 1. Create Shared Schema Component

**File:** `src/Frontend/src/components/datasource/EmbeddedSchemaBuilder.tsx` (NEW)

**Purpose:** Wrapper around SchemaBuilder that works in embedded mode

**Content:** Import and re-export SchemaBuilder with props for embedding
```tsx
import React from 'react';
import SchemaBuilder from '../../pages/schema/SchemaBuilder';

interface EmbeddedSchemaBuilderProps {
  value?: string;           // JSON schema as string
  onChange?: (json: string) => void;  // Callback when schema changes
  disabled?: boolean;
}

const EmbeddedSchemaBuilder: React.FC<EmbeddedSchemaBuilderProps> = ({
  value,
  onChange,
  disabled
}) => {
  return (
    <div style={{ padding: '0' }}>
      <SchemaBuilder 
        initialSchema={value}
        onSchemaChange={onChange}
        embedded={true}
        showHeader={false}      // Hide page header
        showBackButton={false}  // Hide back button
        showSaveButton={false}  // Hide save button (form handles save)
      />
    </div>
  );
};

export default EmbeddedSchemaBuilder;
```

**Note:** The SchemaBuilder component itself is **NOT changed**. Only the wrapper provides the interface.

### 2. Update DataSourceFormEnhanced

**File:** `src/Frontend/src/pages/datasources/DataSourceFormEnhanced.tsx`

**Changes:**

#### Import Addition:
```tsx
import EmbeddedSchemaBuilder from '../../components/datasource/EmbeddedSchemaBuilder';
```

#### State Addition:
```tsx
const [jsonSchema, setJsonSchema] = useState<string>('{}');
```

#### New Tab (after File Settings tab):
```tsx
<TabPane 
  tab={<span><FileTextOutlined /> הגדרת Schema</span>} 
  key="schema"
>
  <Alert
    message="הגדרת JSON Schema למקור נתונים"
    description="Schema יאמת את הקבצים שיתקבלו ממקור נתונים זה. השדות שתגדיר כאן ישמשו גם ליצירת מדדים."
    type="info"
    showIcon
    style={{ marginBottom: 16 }}
  />

  <EmbeddedSchemaBuilder
    value={jsonSchema}
    onChange={setJsonSchema}
  />
</TabPane>
```

#### Update Validation Tab:
```tsx
<TabPane 
  tab={<span><SafetyOutlined /> כללי אימות</span>} 
  key="validation"
>
  <Alert
    message="כללי אימות נתונים"
    description="Schema מוגדר בלשונית 'הגדרת Schema'. כאן תוכל להגדיר כיצד לטפל ברשומות שגויות."
    type="info"
    showIcon
    style={{ marginBottom: 16 }}
  />

  {/* Remove schema linking alerts */}
  
  <Row gutter={16}>
    {/* ... keep existing validation fields ... */}
  </Row>
</TabPane>
```

#### Update Form Submission:
```tsx
const requestPayload = {
  // ... existing fields ...
  jsonSchema: JSON.parse(jsonSchema),  // ← Add this
  schemaVersion: 1,                    // ← Add this
  filePath: values.connectionPath,     // ← Add this
  filePattern: values.fileType === 'CSV' ? '*.csv' : 
               values.fileType === 'JSON' ? '*.json' : '*.*',  // ← Add this
  pollingRate: '00:05:00',             // ← Add this (or from form if added)
};
```

### 3. Update DataSourceEditEnhanced

**File:** `src/Frontend/src/pages/datasources/DataSourceEditEnhanced.tsx`

**Similar changes:**
- Import EmbeddedSchemaBuilder
- Add jsonSchema state
- Add Schema tab (same as create form)
- Load existing JsonSchema from datasource
- Save updated JsonSchema on submit

**Load existing schema:**
```tsx
useEffect(() => {
  if (dataSource && dataSource.JsonSchema) {
    setJsonSchema(JSON.stringify(dataSource.JsonSchema, null, 2));
  }
}, [dataSource]);
```

### 4. Update App.tsx Navigation

**File:** `src/Frontend/src/App.tsx`

**Remove Schema Management menu item:**
```tsx
// REMOVE THIS:
<Menu.Item key="schema" icon={<FileTextOutlined />}>
  <Link to="/schema">{t('menu.schemaManagement')}</Link>
</Menu.Item>

// REMOVE THIS ROUTE TOO:
<Route path="/schema/*" element={<SchemaManagementEnhanced />} />
```

**Keep schema pages** for now (in case any direct links exist), but remove from navigation.

---

## User Experience Flow

### Creating a New DataSource

**Step-by-step:**

1. Navigate to DataSources → "יצירת מקור נתונים"
2. Fill **Tab 1** (מידע בסיסי): Name, Supplier, Category
3. Fill **Tab 2** (הגדרות חיבור): Connection type, path, credentials
4. Fill **Tab 3** (הגדרות קובץ): File type, encoding, delimiters
5. **NEW! Tab 4** (הגדרת Schema):
   - Add fields visually OR
   - Edit JSON directly OR
   - Import existing schema JSON
6. Fill **Tab 5** (תזמון): Schedule settings
7. Fill **Tab 6** (כללי אימות): Validation rules
8. Fill **Tab 7** (התראות): Notification settings
9. Click **"שמור"** (Save)
10. Done! DataSource created with embedded schema

### Editing an Existing DataSource

**Step-by-step:**

1. Click "ערוך" on a datasource
2. See all tabs including **Schema tab**
3. Schema tab shows **existing schema** (loaded from JsonSchema property)
4. Edit schema using visual or JSON editor
5. Save - schema updates in place

---

## Benefits of This Approach

1. **Single place** to manage datasource and its schema
2. **No navigation** between pages
3. **Immediate validation** - see schema as you build datasource
4. **Consistent UX** - all datasource config in one form
5. **Simplified workflow** - from 2 steps to 1 step

---

## Risks & Mitigations

### Risk 1: SchemaBuilder is too complex for embedding
**Mitigation:** We've reviewed it - it's self-contained and works as component

### Risk 2: Form becomes too long with extra tab
**Mitigation:** Tabs keep it organized; users can skip schema if not needed

### Risk 3: Users might miss the schema tab
**Mitigation:** Add alert in Validation tab pointing to Schema tab

---

## Testing Plan

After implementation:

1. **Create datasource with schema**
   - Fill all tabs including Schema
   - Verify save includes JsonSchema
   - Check datasource in database has schema

2. **Edit datasource schema**
   - Open edit form
   - See existing schema loaded
   - Modify schema
   - Save and verify changes persist

3. **Create datasource without schema**
   - Skip schema tab
   - Verify datasource saves with empty JsonSchema

4. **Navigate to metrics**
   - Verify datasource dropdown shows datasources with schemas
   - Create metric using schema fields

---

## Approval Required

**Please confirm:**

✅ Add Schema tab to DataSource create/edit forms (Tab 4)
✅ Use SchemaBuilder component as-is (no changes to component)
✅ Remove Schema Management from navigation
✅ Update form submission to include JsonSchema
✅ Update Validation tab to remove schema linking alerts

**Once approved, I will:**
1. Create EmbeddedSchemaBuilder wrapper component
2. Update DataSourceFormEnhanced with new Schema tab
3. Update DataSourceEditEnhanced with new Schema tab
4. Remove Schema Management from App.tsx navigation
5. Test the complete workflow
6. Then proceed with enhanced metrics (labels + alerts)

**Do you approve these changes?**
