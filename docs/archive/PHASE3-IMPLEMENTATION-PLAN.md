# Phase 3 Implementation Plan - Form Integration

## Execution Strategy

Working on large files safely requires breaking down into small, verifiable steps.

### Step-by-Step Approach

#### Step 1: DataSourceFormEnhanced Updates

**File:** `src/Frontend/src/pages/datasources/DataSourceFormEnhanced.tsx` (~500 lines)

**Changes:**
1. Add imports (SchemaBuilder dependencies)
2. Add state variables for schema
3. Add new Schema tab (Tab 4) - copy SchemaBuilder JSX
4. Update Validation tab (remove schema linking alerts)
5. Update form submission (add JsonSchema to payload)

**Safe approach:**
- Make one change at a time
- Verify file saves successfully after each change
- Keep all existing functionality intact

#### Step 2: DataSourceEditEnhanced Updates

**File:** `src/Frontend/src/pages/datasources/DataSourceEditEnhanced.tsx` (~500 lines)

**Changes:**
1. Add imports (same as create form)
2. Add state variables for schema
3. Add new Schema tab (Tab 4) - copy SchemaBuilder JSX  
4. Add useEffect to load existing JsonSchema
5. Update form submission (include JsonSchema in payload)

#### Step 3: Navigation Update

**File:** `src/Frontend/src/App.tsx`

**Changes:**
1. Remove Schema Management menu item
2. Keep route (for direct access) but remove from sidebar

#### Step 4: Testing

- Test list table (already done)
- Test create datasource with schema
- Test edit datasource schema
- Test without schema

---

## Detailed Implementation

### DataSourceFormEnhanced Changes

#### Change 1: Imports
```tsx
// Add after existing imports
import { createJSONEditor } from 'vanilla-jsoneditor';
import 'vanilla-jsoneditor/themes/jse-theme-dark.css';
import RegexHelperDialog from '../../components/schema/RegexHelperDialog';
```

#### Change 2: State Variables
```tsx
// Add to existing state declarations
const [schemaName, setSchemaName] = useState<string>('');
const [schemaDisplayName, setSchemaDisplayName] = useState<string>('');
const [schemaDescription, setSchemaDescription] = useState<string>('');
const [schemaFields, setSchemaFields] = useState<any[]>([]);
const [isFieldModalVisible, setIsFieldModalVisible] = useState<boolean>(false);
const [editingField, setEditingField] = useState<any>(null);
const [schemaEditorTab, setSchemaEditorTab] = useState<string>('visual');
const [jsonSchemaString, setJsonSchemaString] = useState<string>('{}');
const [isJsonPreviewVisible, setIsJsonPreviewVisible] = useState<boolean>(false);
const [jsonEditorInstance, setJsonEditorInstance] = useState<any>(null);
const [isRegexHelperVisible, setIsRegexHelperVisible] = useState<boolean>(false);
const jsonEditorRef = useRef<HTMLDivElement>(null);
```

#### Change 3: New Schema Tab
Insert after File Settings tab, before Schedule tab:
```tsx
{/* Tab 4: Schema Definition - NEW */}
<TabPane 
  tab={<span><FileTextOutlined /> הגדרת Schema</span>} 
  key="schema"
>
  {/* Copy entire SchemaBuilder content here */}
  {/* Keep all Row/Col/Card structure exactly as-is */}
</TabPane>
```

#### Change 4: Update Validation Tab
Remove schema linking alerts, add note about Schema tab

#### Change 5: Form Submission
```tsx
const requestPayload = {
  // ... existing fields ...
  jsonSchema: JSON.parse(jsonSchemaString),
  schemaVersion: 1,
  filePath: values.connectionPath,
  filePattern: values.fileType === 'CSV' ? '*.csv' : 
               values.fileType === 'JSON' ? '*.json' : '*.*',
  pollingRate: '00:05:00',
};
```

---

## Safety Checks

Before each change:
- ✓ Check file compiles
- ✓ Check no syntax errors
- ✓ Verify imports resolve

After each change:
- ✓ File saves successfully
- ✓ No compilation errors
- ✓ TypeScript validates

---

## Rollback Strategy

If anything breaks:
- Each change is atomic
- Can revert individual changes
- Git history preserved
- Original SchemaBuilder untouched

---

## Estimated Timeline

- DataSourceFormEnhanced: 1.5 hours
- DataSourceEditEnhanced: 1.5 hours
- App.tsx navigation: 15 minutes
- Testing: 30 minutes
- **Total: ~3.5 hours**

---

## Starting Implementation Now

Will proceed step-by-step with careful verification.
