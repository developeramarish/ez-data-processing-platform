# Option A - Phase 2 Implementation Progress Report

**Date:** October 19, 2025  
**Status:** In Progress - Foundation Components Complete  
**Next:** Multi-Step Wizard Implementation

## Overview

Phase 2 adds 5 major enhancements to the metrics configuration system:
1. ✅ Labels as simple comma-separated text with PromQL generation
2. ✅ Required field selection for all metrics (one metric → one field)
3. ⏳ Multi-step wizard (5 steps)
4. ✅ Hebrew→English metric name helper with 40+ term dictionary
5. ⏳ Alert rules system with PromQL expression builder

## Completed Work

### Backend Models (✅ Complete)

#### 1. AlertRule Model
**File:** `src/Services/MetricsConfigurationService/Models/AlertRule.cs`

```csharp
public class AlertRule
{
    public string? Id { get; set; }
    [Required]
    [RegularExpression(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    public string Name { get; set; }
    
    [Required]
    public string Description { get; set; }
    
    [Required]
    public string Expression { get; set; }
    
    public string? For { get; set; }
    public string? KeepFiringFor { get; set; }
    
    [Required]
    public string Severity { get; set; } = "warning";
    
    public Dictionary<string, string> Labels { get; set; }
    public Dictionary<string, string> Annotations { get; set; }
    public string? TemplateId { get; set; }
    public Dictionary<string, object>? TemplateParameters { get; set; }
    public bool IsEnabled { get; set; } = true;
}
```

**Supporting Models:**
- `AlertExpressionTemplate`: Template structure for UI helper
- `TemplateParameter`: Parameter definitions for templates

#### 2. MetricConfiguration Model Updates
**File:** `src/Services/MetricsConfigurationService/Models/MetricConfiguration.cs`

**Key Changes:**
```csharp
// REQUIRED: Every metric must relate to exactly one schema field
[Required]
public string FieldPath { get; set; } = string.Empty;

// Prometheus metric type
[Required]
public string PrometheusType { get; set; } = "gauge";

// Simple label names (user input)
public string? LabelNames { get; set; }

// Generated PromQL expression
public string? LabelsExpression { get; set; }

// Alert rules
public List<AlertRule>? AlertRules { get; set; }
```

### Frontend Components (✅ 3/5 Complete)

#### 1. AlertExpressionTemplates.tsx (✅ Complete)
**File:** `src/Frontend/src/components/metrics/AlertExpressionTemplates.tsx`

**Features:**
- Library of 8 pre-defined Prometheus alert templates
- Categories: rate, percentage, comparison, absence, change, aggregation, custom
- Helper functions for template selection and parameter filling

**Template Examples:**
```typescript
// High Rate Detection
{
  id: 'high_rate',
  template: 'rate({metric}[{duration}]) > {threshold}',
  example: 'rate(errors_total[5m]) > 10'
}

// Percentage Threshold
{
  id: 'percentage_threshold',
  template: '({numerator} / {denominator}) * 100 > {threshold}',
  example: '(failed_transactions / total_transactions) * 100 > 5'
}

// Absence Detection
{
  id: 'absence_detection',
  template: 'absent({metric}[{duration}])',
  example: 'absent(heartbeat_total[10m])'
}
```

**Functions:**
- `generateExpression()`: Fills template with user parameters
- `getTemplatesByCategory()`: Filter templates by category
- `getTemplateCategories()`: Get all available categories

#### 2. MetricNameHelper.tsx (✅ Complete)
**File:** `src/Frontend/src/components/metrics/MetricNameHelper.tsx`

**Features:**
- Hebrew-to-English dictionary with 40+ common terms
- Auto-suggests metric names based on:
  - Hebrew description entered by user
  - Selected field name
  - Prometheus type
  - Category
- Real-time validation of Prometheus naming rules
- Copy-to-clipboard functionality

**Dictionary Examples:**
```typescript
{
  'עסקה': 'transaction',
  'סכום': 'amount',
  'יומי': 'daily',
  'מונה': 'count',
  'סך': 'total',
  'שגיאה': 'error'
}
```

**Validation Rules:**
- Must match: `^[a-z][a-z0-9_]*$`
- Minimum 3 characters
- Maximum 100 characters
- Must start with lowercase letter

**Functions:**
- `suggestMetricName()`: Generates suggestion from inputs
- `validatePrometheusName()`: Validates name format
- `translateHebrew()`: Translates Hebrew words to English

#### 3. SimpleLabelInput.tsx (✅ Complete)
**File:** `src/Frontend/src/components/metrics/SimpleLabelInput.tsx`

**Features:**
- Accepts comma-separated label names: `"status, region, customer_type"`
- Auto-generates PromQL expression: `{status="$status", region="$region", customer_type="$customer_type"}`
- Validates label names against Prometheus rules
- Checks if labels match schema fields
- Real-time preview of generated PromQL

**Validation:**
- Label names must match: `^[a-zA-Z_][a-zA-Z0-9_]*$`
- Warns if labels don't exist in schema
- Highlights invalid labels in red

**Functions:**
- `generatePromQLExpression()`: Creates PromQL from label names
- `validateLabelName()`: Validates single label name
- `parseLabels()`: Parses and validates all labels

**Example Usage:**
```typescript
Input: "status, region"
Output PromQL: {status="$status", region="$region"}

Validation:
- ✅ Valid labels shown in green tags
- ❌ Invalid labels shown in red tags
- ⚠️ Non-schema labels shown in orange tags
```

## Remaining Work

### Multi-Step Wizard (⏳ Not Started)

#### Required Components:
1. **MetricConfigurationWizard.tsx** - Main wizard container with Steps component
2. **WizardStepDataSource.tsx** - Step 1: Global/Specific tabs (reuse existing)
3. **WizardStepField.tsx** - Step 2: REQUIRED field selection + helper text
4. **WizardStepDetails.tsx** - Step 3: Name, description, category with MetricNameHelper
5. **WizardStepLabels.tsx** - Step 4: SimpleLabelInput integration
6. **WizardStepAlerts.tsx** - Step 5: AlertRuleBuilder (optional)

#### Wizard Flow:
```
Step 1: Data Source Selection
  ├─ Global metrics (applies to all data sources)
  └─ Specific metrics (one-to-one with data source)

Step 2: Field Selection (REQUIRED)
  ├─ Select exactly one schema field
  ├─ Show field usage explanation per type
  └─ Validation: Cannot proceed without field

Step 3: Metric Details
  ├─ MetricNameHelper shows suggestion
  ├─ Enter display name (Hebrew)
  ├─ Enter description
  ├─ Select category
  └─ Select Prometheus type

Step 4: Labels Configuration
  ├─ SimpleLabelInput for comma-separated names
  ├─ Real-time PromQL preview
  └─ Validation against schema fields

Step 5: Alert Rules (Optional)
  ├─ AlertRuleBuilder with templates
  ├─ Add multiple alert rules
  ├─ Preview generated YAML
  └─ Can skip this step
```

### AlertRuleBuilder Component (⏳ Not Started)

**File to Create:** `src/Frontend/src/components/metrics/AlertRuleBuilder.tsx`

**Requirements:**
- Select from ALERT_EXPRESSION_TEMPLATES
- Dynamic parameter inputs based on template
- Metric selector for multi-metric expressions
- Real-time PromQL preview
- Add/Edit/Delete alert rules
- Severity selector (critical/warning/info)
- Duration inputs for `for` and `keep_firing_for`

**UI Structure:**
```tsx
<Card>
  <Select // Template chooser
    options={ALERT_EXPRESSION_TEMPLATES}
  />
  
  <Form> // Dynamic parameters based on template
    {template.parameters.map(param => 
      param.type === 'metric' ? <MetricSelector /> :
      param.type === 'duration' ? <DurationInput /> :
      param.type === 'number' ? <NumberInput /> :
      <TextInput />
    )}
  </Form>
  
  <Card> // PromQL Preview
    <pre>{generatedExpression}</pre>
  </Card>
  
  <Select // Severity
    options={['critical', 'warning', 'info']}
  />
  
  <List> // Added alerts
    {alertRules.map(rule => <AlertRuleCard />)}
  </List>
</Card>
```

### Backend API Updates (⏳ Not Started)

**Files to Update:**
- `src/Services/MetricsConfigurationService/Controllers/MetricController.cs`
- `src/Services/MetricsConfigurationService/Repositories/MetricRepository.cs`

**Required Changes:**
1. Add validation for required `FieldPath`
2. Add validation for `PrometheusType`
3. Handle `LabelNames` and `LabelsExpression` fields
4. Handle `AlertRules` list
5. Update existing metrics to include new required fields

### Frontend Routing Update (⏳ Not Started)

**File to Update:** `src/Frontend/src/App.tsx`

**Change:**
```tsx
// Replace current route
<Route path="/metrics/new" element={<MetricConfigurationFormSimplified />} />

// With wizard route
<Route path="/metrics/new" element={<MetricConfigurationWizard />} />

// Keep edit route as-is or update to wizard
<Route path="/metrics/edit/:id" element={<MetricConfigurationWizard />} />
```

## Technical Patterns Established

### 1. Label Generation Pattern
```typescript
// Input
const labelNames = "status, region, customer_type";

// Process
const labels = labelNames.split(',').map(l => l.trim());
const promqlParts = labels.map(label => `${label}="$${label}"`);
const expression = `{${promqlParts.join(', ')}}`;

// Output
// {status="$status", region="$region", customer_type="$customer_type"}
```

### 2. Metric Name Translation Pattern
```typescript
// Input
const hebrewDesc = "סך עסקאות יומי";
const fieldName = "transaction_amount";
const prometheusType = "counter";

// Process
const translatedWords = translateHebrew(hebrewDesc);
// ['total', 'transactions', 'daily']

const parts = [fieldName, ...translatedWords];
if (prometheusType === 'counter') parts.push('total');

const suggestion = parts.join('_')
  .replace(/_+/g, '_')
  .replace(/[^a-z0-9_]/g, '_')
  .replace(/^[^a-z]/g, '')
  .replace(/_$/g, '');

// Output
// "transaction_amount_total_transactions_daily_total"
```

### 3. Alert Expression Generation Pattern
```typescript
// Input
const template = {
  template: 'rate({metric}[{duration}]) > {threshold}',
  parameters: [
    { name: 'metric', type: 'metric' },
    { name: 'duration', type: 'duration', defaultValue: '5m' },
    { name: 'threshold', type: 'number' }
  ]
};

const userParams = {
  metric: 'errors_total',
  duration: '5m',
  threshold: '10'
};

// Process
let expression = template.template;
Object.keys(userParams).forEach(key => {
  expression = expression.replace(
    new RegExp(`\\{${key}\\}`, 'g'),
    userParams[key]
  );
});

// Output
// "rate(errors_total[5m]) > 10"
```

## Key Decisions Made

### 1. Field Selection is REQUIRED
- Every metric MUST relate to exactly one schema field
- This ensures metrics have clear data source mapping
- UI will prevent proceeding without field selection

### 2. Two-Field Label System
- `LabelNames`: User-friendly comma-separated input
- `LabelsExpression`: Generated PromQL for backend use
- Both stored for flexibility and debugging

### 3. Alert Rules as Embedded List
- Alert rules stored directly in `MetricConfiguration` document
- Simplifies querying and management
- Each metric can have 0-many alert rules

### 4. Template-Based Alert Creation
- Pre-defined templates for common patterns
- Custom template for advanced users
- Parameters validated based on type
- Real-time PromQL preview

### 5. Prometheus Type is REQUIRED
- Forces users to understand metric semantics
- Affects metric name suggestions (_total, _seconds suffixes)
- Determines valid PromQL operations

## Testing Requirements

### Unit Tests Needed
1. `translateHebrew()` - Dictionary translations
2. `suggestMetricName()` - Name generation logic
3. `validatePrometheusName()` - Name validation
4. `generatePromQLExpression()` - Label expression generation
5. `validateLabelName()` - Label validation
6. `generateExpression()` - Alert template filling

### Integration Tests Needed
1. Create metric with required field
2. Create metric with labels and PromQL
3. Create metric with alert rules
4. Update existing metric with new fields
5. Validate field selection prevents submission
6. Validate label names against schema

### E2E Tests Needed
1. Complete wizard flow (all 5 steps)
2. Skip alert rules (optional step)
3. Edit existing metric in wizard
4. Field validation prevents next step
5. Label PromQL generation displayed correctly
6. Alert rule preview shows correct syntax

## Next Immediate Steps

1. ✅ Create AlertRule model
2. ✅ Update MetricConfiguration model
3. ✅ Create AlertExpressionTemplates library
4. ✅ Create MetricNameHelper component
5. ✅ Create SimpleLabelInput component
6. ⏳ Create AlertRuleBuilder component
7. ⏳ Create MetricConfigurationWizard component
8. ⏳ Create wizard step components (5 files)
9. ⏳ Update App.tsx routing
10. ⏳ Update backend validation
11. ⏳ Test complete wizard flow
12. ⏳ Document new features

## Files Created

### Backend (3 files)
1. `src/Services/MetricsConfigurationService/Models/AlertRule.cs` - New file
2. `src/Services/MetricsConfigurationService/Models/MetricConfiguration.cs` - Updated

### Frontend (3 files)
1. `src/Frontend/src/components/metrics/AlertExpressionTemplates.tsx` - New file
2. `src/Frontend/src/components/metrics/MetricNameHelper.tsx` - New file
3. `src/Frontend/src/components/metrics/SimpleLabelInput.tsx` - New file

### Documentation (1 file)
1. `docs/OPTION-A-PHASE-2-PROGRESS.md` - This file

## Files Still to Create

### Frontend Components (7 files)
1. `src/Frontend/src/components/metrics/AlertRuleBuilder.tsx`
2. `src/Frontend/src/pages/metrics/MetricConfigurationWizard.tsx`
3. `src/Frontend/src/components/metrics/WizardStepDataSource.tsx`
4. `src/Frontend/src/components/metrics/WizardStepField.tsx`
5. `src/Frontend/src/components/metrics/WizardStepDetails.tsx`
6. `src/Frontend/src/components/metrics/WizardStepLabels.tsx`
7. `src/Frontend/src/components/metrics/WizardStepAlerts.tsx`

## Success Criteria

Phase 2 will be considered complete when:

✅ Backend Models
- [x] AlertRule model created with validation
- [x] MetricConfiguration updated with new fields
- [ ] API validation updated for required fields

✅ Frontend Components
- [x] AlertExpressionTemplates library with 8 templates
- [x] MetricNameHelper with 40+ term dictionary
- [x] SimpleLabelInput with PromQL generation
- [ ] AlertRuleBuilder with template selector
- [ ] Multi-step wizard with 5 steps

✅ User Experience
- [ ] User can complete wizard start to finish
- [ ] Field selection is required (cannot skip)
- [ ] Labels generate PromQL automatically
- [ ] Metric name suggestions from Hebrew
- [ ] Alert rules can be added (optional)
- [ ] PromQL previews show everywhere

✅ Testing
- [ ] All wizard steps validated
- [ ] PromQL generation verified
- [ ] Alert rules saved correctly
- [ ] Edit mode loads wizard correctly
- [ ] Browser testing complete

## Estimated Completion

- **Foundation (Backend + 3 Components):** ✅ Complete (3-4 hours)
- **AlertRuleBuilder Component:** ⏳ Estimated 2-3 hours
- **Multi-Step Wizard:** ⏳ Estimated 3-4 hours
- **Testing & Refinement:** ⏳ Estimated 2 hours
- **Total Remaining:** ~7-9 hours of development

## Notes

- All Prometheus naming conventions validated against official docs
- Alert templates based on real-world Prometheus patterns
- Hebrew dictionary can be expanded as needed
- Wizard preserves data between steps
- Alert rules optional but metric configuration required
