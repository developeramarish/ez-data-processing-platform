# Schema Embedding - Detailed Grid Structure Changes

## Date: 2025-10-20

## Overview

This document shows the **exact Ant Design grid structure** (Row/Col components) that will be used when embedding SchemaBuilder into DataSource forms.

---

## SchemaBuilder Grid Structure (As-Is - Will Be Copied)

### 1. Schema Basic Info Section

**Current structure in SchemaBuilder:**
```tsx
<Card style={{ marginBottom: 16 }}>
  <Row gutter={16}>
    <Col span={8}>
      <Form.Item label="שם Schema (אנגלית)">
        <Input value={schemaName} onChange={...} />
      </Form.Item>
    </Col>
    <Col span={8}>
      <Form.Item label="שם תצוגה (עברית)">
        <Input value={schemaDisplayName} onChange={...} />
      </Form.Item>
    </Col>
    <Col span={8}>
      <Form.Item label="גרסה">
        <Input defaultValue="v1.0" />
      </Form.Item>
    </Col>
  </Row>
  
  <Form.Item label="תיאור">
    <TextArea value={schemaDescription} onChange={...} rows={2} />
  </Form.Item>
</Card>
```

**Grid breakdown:**
- 1 Row with gutter=16
- 3 equal Cols (span=8 each, total=24)
- Full-width Form.Item below

### 2. Main Content Area

**Current structure in SchemaBuilder:**
```tsx
<Row gutter={16}>
  <Col span={isJsonPreviewVisible ? 14 : 24}>
    <Card>
      {/* Tabs with Visual/JSON/Validation editors */}
    </Card>
  </Col>
  
  {isJsonPreviewVisible && (
    <Col span={10}>
      <Card>
        {/* JSON Preview Panel */}
      </Card>
    </Col>
  )}
</Row>
```

**Grid breakdown:**
- 1 Row with gutter=16
- Dynamic Cols:
  - Main editor: span=14 OR span=24 (when preview hidden)
  - Preview panel: span=10 (conditional)
- Total always = 24 (14+10 or 24 alone)

### 3. Field Modal (Add/Edit Field)

**Current structure in SchemaBuilder:**
```tsx
<Modal width={800}>
  <Form>
    {/* Row 1: Field name fields */}
    <Row gutter={16}>
      <Col span={12}>
        <Form.Item name="name" label="שם שדה (אנגלית)">
          <Input />
        </Form.Item>
      </Col>
      <Col span={12}>
        <Form.Item name="displayName" label="שם תצוגה (עברית)">
          <Input />
        </Form.Item>
      </Col>
    </Row>

    {/* Row 2: Type and Required */}
    <Row gutter={16}>
      <Col span={16}>
        <Form.Item name="type" label="סוג השדה">
          <Select>...</Select>
        </Form.Item>
      </Col>
      <Col span={8}>
        <Form.Item name="required" label="שדה חובה">
          <Switch />
        </Form.Item>
      </Col>
    </Row>

    {/* String validation (conditional) */}
    {fieldType === 'string' && (
      <Collapse>
        <Panel>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="minLength">...</Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="maxLength">...</Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="format">...</Form.Item>
            </Col>
          </Row>
        </Panel>
      </Collapse>
    )}

    {/* Number validation (conditional) */}
    {(fieldType === 'number' || fieldType === 'integer') && (
      <Collapse>
        <Panel>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="minimum">...</Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="maximum">...</Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="multipleOf">...</Form.Item>
            </Col>
          </Row>
          
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="exclusiveMinimum">...</Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="exclusiveMaximum">...</Form.Item>
            </Col>
          </Row>
        </Panel>
      </Collapse>
    )}

    {/* Array validation (conditional) */}
    {fieldType === 'array' && (
      <Collapse>
        <Panel>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item name="minItems">...</Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="maxItems">...</Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item name="uniqueItems">...</Form.Item>
            </Col>
          </Row>
        </Panel>
      </Collapse>
    )}
  </Form>
</Modal>
```

**Grid breakdown:**
- Multiple Rows, each with gutter=16
- Various Col spans:
  - 2-column layouts: span=12 + span=12 (equal)
  - 3-column layouts: span=8 + span=8 + span=8 (equal)
  - Asymmetric: span=16 + span=8 (type selector + switch)

---

## DataSource Form Grid Structure (Current)

### DataSourceFormEnhanced - Existing Tabs

**Tab: Basic Information**
```tsx
<Row gutter={16}>
  <Col xs={24} lg={12}>
    <Form.Item name="name">...</Form.Item>
  </Col>
  <Col xs={24} lg={12}>
    <Form.Item name="supplierName">...</Form.Item>
  </Col>
</Row>

<Row gutter={16}>
  <Col xs={24} lg={12}>
    <Form.Item name="category">...</Form.Item>
  </Col>
  <Col xs={24} lg={12}>
    <Form.Item name="retentionDays">...</Form.Item>
  </Col>
</Row>
```

**Current grid pattern:**
- Uses responsive spans: `xs={24} lg={12}`
- 2-column layout on large screens
- Stacks on mobile (xs=24)
- Consistent gutter=16

---

## Proposed Integration - Grid Structure

### Option A: Keep SchemaBuilder Grid As-Is (RECOMMENDED)

**New Schema Tab in DataSourceFormEnhanced:**
```tsx
<TabPane tab={<span><FileTextOutlined /> הגדרת Schema</span>} key="schema">
  <Alert message="..." type="info" style={{ marginBottom: 16 }} />
  
  {/* SchemaBuilder content pasted here - EXACT grid structure preserved */}
  
  {/* Section 1: Basic Info - UNCHANGED */}
  <Card style={{ marginBottom: 16 }}>
    <Row gutter={16}>
      <Col span={8}>
        <Form.Item label="שם Schema (אנגלית)">
          <Input />
        </Form.Item>
      </Col>
      <Col span={8}>
        <Form.Item label="שם תצוגה (עברית)">
          <Input />
        </Form.Item>
      </Col>
      <Col span={8}>
        <Form.Item label="גרסה">
          <Input defaultValue="v1.0" />
        </Form.Item>
      </Col>
    </Row>
    
    <Form.Item label="תיאור">
      <TextArea rows={2} />
    </Form.Item>
  </Card>

  {/* Section 2: Main Editor - UNCHANGED */}
  <Row gutter={16}>
    <Col span={isJsonPreviewVisible ? 14 : 24}>
      <Card>
        <Tabs>
          {/* Visual/JSON/Validation tabs */}
        </Tabs>
      </Card>
    </Col>
    
    {isJsonPreviewVisible && (
      <Col span={10}>
        <Card>{/* Preview */}</Card>
      </Col>
    )}
  </Row>
  
  {/* Section 3: Field Modal - UNCHANGED */}
  <Modal width={800}>
    <Form>
      {/* All Row/Col structures from SchemaBuilder preserved */}
    </Form>
  </Modal>
</TabPane>
```

**Grid consistency:**
- ✅ SchemaBuilder uses fixed spans (8, 12, 14, 10)
- ✅ DataSource forms use responsive spans (xs=24, lg=12)
- ✅ Both patterns work fine within their tabs
- ✅ No conflicts - each tab is independent

### Option B: Make SchemaBuilder Responsive (NOT RECOMMENDED)

Convert all SchemaBuilder Cols to responsive:
```tsx
<Row gutter={16}>
  <Col xs={24} lg={8}> {/* Instead of span={8} */}
    <Form.Item label="שם Schema">...</Form.Item>
  </Col>
  <Col xs={24} lg={8}>
    <Form.Item label="שם תצוגה">...</Form.Item>
  </Col>
  <Col xs={24} lg={8}>
    <Form.Item label="גרסה">...</Form.Item>
  </Col>
</Row>
```

**Why NOT recommended:**
- Would require changes to SchemaBuilder (violates "use as-is" requirement)
- SchemaBuilder is complex - changing grid could break layout
- Fixed spans work fine in tabs
- Not worth the risk

---

## Detailed Comparison

### Grid System Used

**SchemaBuilder (Standalone Page):**
```
Layout Pattern: Fixed Ant Design 24-column grid
Row gutter: 16px (consistent)
Col spans: 
  - 3-column: 8-8-8 (equals 24)
  - 2-column: 12-12 (equals 24)
  - Asymmetric: 16-8 (equals 24)
  - Dynamic: 14-10 OR 24 (preview toggle)
Responsive: NO
Mobile support: Fields stack naturally (Ant Design default)
```

**DataSourceFormEnhanced (Current):**
```
Layout Pattern: Responsive Ant Design grid
Row gutter: 16px (consistent)
Col spans: 
  - xs={24} lg={12} (full width mobile, half width desktop)
Responsive: YES
Mobile support: Explicit (xs breakpoint)
```

### Side-by-Side Grid Comparison

**Schema Basic Info Row:**
```
SchemaBuilder (as-is):
<Row gutter={16}>
  <Col span={8}>...</Col>     // 33.3% width (8/24)
  <Col span={8}>...</Col>     // 33.3% width (8/24)
  <Col span={8}>...</Col>     // 33.3% width (8/24)
</Row>

DataSource Basic Info Row (existing):
<Row gutter={16}>
  <Col xs={24} lg={12}>...</Col>  // 50% on desktop, 100% on mobile
  <Col xs={24} lg={12}>...</Col>  // 50% on desktop, 100% on mobile
</Row>
```

**Different but both valid!** Each tab can have its own grid pattern.

---

## Proposed Embedded Structure

### Complete Schema Tab Structure

```tsx
{/* NEW TAB 4 - Schema Definition */}
<TabPane 
  tab={<span><FileTextOutlined /> הגדרת Schema</span>} 
  key="schema"
>
  <Alert
    message="הגדרת JSON Schema למקור נתונים"
    description="Schema יאמת את הקבצים שיתקבלו ממקור זה"
    type="info"
    showIcon
    style={{ marginBottom: 16 }}
  />

  {/* SCHEMA BASIC INFO - Keep SchemaBuilder grid (span=8) */}
  <Card style={{ marginBottom: 16 }}>
    <Row gutter={16}>
      <Col span={8}>                          {/* ← Fixed span, not responsive */}
        <Form.Item label="שם Schema (אנגלית)">
          <Input 
            value={schemaName}
            onChange={(e) => setSchemaName(e.target.value)}
            placeholder="sales_data_v1"
          />
        </Form.Item>
      </Col>
      <Col span={8}>                          {/* ← Fixed span, not responsive */}
        <Form.Item label="שם תצוגה (עברית)">
          <Input 
            value={schemaDisplayName}
            onChange={(e) => setSchemaDisplayName(e.target.value)}
            placeholder="נתוני מכירות"
          />
        </Form.Item>
      </Col>
      <Col span={8}>                          {/* ← Fixed span, not responsive */}
        <Form.Item label="גרסה Schema">
          <Input defaultValue="v1.0" disabled />
        </Form.Item>
      </Col>
    </Row>
    
    <Form.Item label="תיאור Schema">
      <TextArea
        value={schemaDescription}
        onChange={(e) => setSchemaDescription(e.target.value)}
        rows={2}
        placeholder="תאר את מטרת Schema זה"
      />
    </Form.Item>
  </Card>

  {/* MAIN EDITOR AREA - Keep SchemaBuilder grid (dynamic spans) */}
  <Row gutter={16}>
    <Col span={isJsonPreviewVisible ? 14 : 24}>  {/* ← Dynamic based on preview */}
      <Card>
        <Tabs activeKey={schemaEditorTab} onChange={setSchemaEditorTab}>
          
          {/* Visual Editor Tab */}
          <Tabs.TabPane tab={<span><EyeOutlined /> עורך חזותי</span>} key="visual">
            <Button type="dashed" icon={<PlusOutlined />} onClick={handleAddField}>
              הוסף שדה חדש
            </Button>
            
            <List dataSource={schemaFields} renderItem={(field) => (
              <List.Item actions={[
                <Button type="link" icon={<EditOutlined />}>ערוך</Button>,
                <Button type="link" danger icon={<DeleteOutlined />}>מחק</Button>
              ]}>
                {/* Field display */}
              </List.Item>
            )} />
          </Tabs.TabPane>

          {/* JSON Editor Tab */}
          <Tabs.TabPane tab={<span><CodeOutlined /> עורך JSON</span>} key="json">
            {/* vanilla-jsoneditor component */}
            <div ref={jsonEditorRef} style={{ height: '500px' }} />
          </Tabs.TabPane>

          {/* Validation Tab */}
          <Tabs.TabPane tab={<span><CheckCircleOutlined /> אימות</span>} key="validation">
            {/* Validation UI */}
          </Tabs.TabPane>
        </Tabs>
      </Card>
    </Col>
    
    {isJsonPreviewVisible && (
      <Col span={10}>                         {/* ← Fixed span for preview */}
        <Card title="JSON Schema Preview">
          <div ref={previewEditorRef} />
        </Card>
      </Col>
    )}
  </Row>

  {/* FIELD MODAL - Keep SchemaBuilder grid */}
  <Modal width={800} visible={isFieldModalVisible}>
    <Form onFinish={handleSaveField}>
      
      {/* Row 1: Field names (2 equal columns) */}
      <Row gutter={16}>
        <Col span={12}>                       {/* ← 50% */}
          <Form.Item name="name" label="שם שדה (אנגלית)">
            <Input />
          </Form.Item>
        </Col>
        <Col span={12}>                       {/* ← 50% */}
          <Form.Item name="displayName" label="שם תצוגה (עברית)">
            <Input />
          </Form.Item>
        </Col>
      </Row>

      {/* Row 2: Type and Required (asymmetric) */}
      <Row gutter={16}>
        <Col span={16}>                       {/* ← 66.7% */}
          <Form.Item name="type" label="סוג השדה">
            <Select>...</Select>
          </Form.Item>
        </Col>
        <Col span={8}>                        {/* ← 33.3% */}
          <Form.Item name="required" label="שדה חובה">
            <Switch />
          </Form.Item>
        </Col>
      </Row>

      {/* Validation rows (conditional on field type) */}
      {fieldType === 'string' && (
        <Row gutter={16}>
          <Col span={8}>                      {/* ← 3 equal columns */}
            <Form.Item name="minLength">...</Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="maxLength">...</Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item name="format">...</Form.Item>
          </Col>
        </Row>
      )}
    </Form>
  </Modal>
</TabPane>
```

---

## Comparison with DataSource Form Tabs

### DataSource Form Grid (Unchanged Tabs)

**Example: Basic Info Tab (will remain unchanged)**
```tsx
<TabPane tab="מידע בסיסי" key="basic">
  <Row gutter={16}>
    <Col xs={24} lg={12}>              {/* ← Responsive */}
      <Form.Item name="name">...</Form.Item>
    </Col>
    <Col xs={24} lg={12}>              {/* ← Responsive */}
      <Form.Item name="supplierName">...</Form.Item>
    </Col>
  </Row>
  {/* ... more rows ... */}
</TabPane>
```

**Example: Connection Tab (will remain unchanged)**
```tsx
<TabPane tab="הגדרות חיבור" key="connection">
  <Row gutter={16}>
    <Col xs={24} lg={16}>              {/* ← Responsive, asymmetric */}
      <Form.Item name="connectionHost">...</Form.Item>
    </Col>
    <Col xs={24} lg={8}>               {/* ← Responsive, asymmetric */}
      <Form.Item name="connectionPort">...</Form.Item>
    </Col>
  </Row>
  {/* ... more rows ... */}
</TabPane>
```

### New Schema Tab (Using SchemaBuilder Grid)

**Schema Tab - Will use DIFFERENT grid pattern:**
```tsx
<TabPane tab="הגדרת Schema" key="schema">
  {/* Using SchemaBuilder's grid pattern - Fixed spans */}
  <Row gutter={16}>
    <Col span={8}>                     {/* ← Fixed, not responsive */}
      <Form.Item>...</Form.Item>
    </Col>
    <Col span={8}>
      <Form.Item>...</Form.Item>
    </Col>
    <Col span={8}>
      <Form.Item>...</Form.Item>
    </Col>
  </Row>
  
  <Row gutter={16}>
    <Col span={14}>                    {/* ← Dynamic but fixed (not responsive) */}
      {/* Editor */}
    </Col>
    <Col span={10}>                    {/* ← Fixed (conditional) */}
      {/* Preview */}
    </Col>
  </Row>
</TabPane>
```

---

## Grid Pattern Summary

| Component | Grid Type | Spans Used | Responsive | Mobile Behavior |
|-----------|-----------|------------|------------|-----------------|
| **SchemaBuilder (as-is)** | Fixed | 8, 12, 14, 10 | NO | Ant Design auto-stack |
| **DataSource Basic Tab** | Responsive | xs=24, lg=12 | YES | Explicit stack |
| **DataSource Connection Tab** | Responsive | xs=24, lg=8/16 | YES | Explicit stack |
| **NEW Schema Tab** | Fixed (from SchemaBuilder) | 8, 12, 14, 10 | NO | Ant Design auto-stack |

**Key insight:** Each tab can have its own grid pattern - they don't need to match!

---

## Mobile Responsiveness Comparison

### On Desktop (>992px)

**DataSource Basic Tab:**
```
┌─────────────────┬─────────────────┐
│   Name (50%)    │  Supplier (50%) │
└─────────────────┴─────────────────┘
```

**New Schema Tab:**
```
┌────────┬────────┬────────┐
│ Name   │ Display│ Version│  (3 × 33.3%)
│ (33%)  │ (33%)  │ (33%)  │
└────────┴────────┴────────┘

┌──────────────┬────────┐
│   Editor     │Preview │  (58% + 42%)
│   (14/24)    │(10/24) │
└──────────────┴────────┘
```

### On Mobile (<768px)

**DataSource Basic Tab (with xs=24):**
```
┌─────────────────┐
│   Name (100%)   │
├─────────────────┤
│ Supplier (100%) │
└─────────────────┘
```

**New Schema Tab (SchemaBuilder with span only):**
```
┌─────────────────┐
│   Name (33%)    │  ← Ant Design will auto-stack these
├─────────────────┤     when screen < breakpoint
│ Display (33%)   │
├─────────────────┤
│ Version (33%)   │
└─────────────────┘

┌─────────────────┐
│  Editor (100%)  │  ← Preview hidden or stacked
└─────────────────┘
```

**Both work!** Ant Design's grid system handles both patterns gracefully.

---

## Final Grid Structure Recommendation

### ✅ Use SchemaBuilder Grid As-Is

**Rationale:**
1. **No changes needed** - copy SchemaBuilder content exactly
2. **Grid mismatch is OK** - each tab is independent
3. **Mobile works** - Ant Design auto-handles stacking
4. **Less risky** - no modifications to working component
5. **Faster implementation** - just copy & paste

### ⚠️ Alternative: Harmonize Grid Patterns

If you prefer **consistent** grid across ALL tabs:

**Option:** Make SchemaBuilder responsive (add xs/lg to all Cols)
**Effort:** Moderate (need to update ~10 Row/Col blocks)
**Risk:** Medium (could break SchemaBuilder layout)
**Benefit:** Consistent mobile experience across tabs

---

## What I'll Do (Pending Your Approval)

### Approach 1: Copy As-Is (Recommended)

```tsx
// Simply copy SchemaBuilder's JSX into new tab
<TabPane key="schema">
  {/* Paste SchemaBuilder content here */}
  {/* Keep all Row gutter={16} and Col span={X} exactly as-is */}
</TabPane>
```

**Result:**
- Tab 1-3, 5-7: Use responsive grid (xs/lg)
- Tab 4 (Schema): Use fixed grid (span only)
- Both patterns coexist peacefully

### Approach 2: Make Responsive (If Requested)

```tsx
// Update SchemaBuilder grid to match form pattern
<TabPane key="schema">
  <Row gutter={16}>
    <Col xs={24} lg={8}>...</Col>  {/* Add xs/lg to all */}
    <Col xs={24} lg={8}>...</Col>
    <Col xs={24} lg={8}>...</Col>
  </Row>
</TabPane>
```

**Result:**
- All tabs use consistent responsive grid
- More work, slightly higher risk

---

## My Recommendation

**Use Approach 1 (Copy As-Is)** because:

1. ✅ Zero changes to SchemaBuilder component
2. ✅ Faster implementation
3. ✅ Lower risk
4. ✅ Grid mismatch doesn't affect UX
5. ✅ Mobile still works fine

**Unless you specifically want consistent responsive grid across all tabs, in which case I can do Approach 2.**

---

## Which Approach Do You Prefer?

**Option A:** Copy SchemaBuilder as-is (mixed grid patterns across tabs) - RECOMMENDED
**Option B:** Make SchemaBuilder responsive (consistent grid patterns across tabs)

Please confirm your preference so I can proceed with implementation.
