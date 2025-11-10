# Option C Implementation Plan - Metrics Configuration + Dashboard
## 3-Week Implementation (UI-First, Always Runnable)

---

## Overview
Building Metrics Configuration and Dashboard features with Formula Builder, Business KPIs, Statistical Analysis, and Grafana integration.

**Key Principles:**
- ✅ UI-First: Build frontend components first
- ✅ Always Runnable: Frontend compiles after each change
- ✅ Real Data Only: All data persists to MongoDB
- ✅ Hebrew Localization: Complete Hebrew UI
- ✅ Incremental: Test each feature as built

---

## Week 1: Frontend UI Foundation (Days 1-5)

### Day 1: Metrics Configuration Page Setup
- [ ] Create Hebrew i18n entries for all metrics terms
- [ ] Create MetricsConfiguration page route in App.tsx
- [ ] Build basic MetricsConfigurationList page
- [ ] Create MetricConfigurationForm component shell
- [ ] Add navigation menu item (Hebrew)
- [ ] Test: Page renders, navigates correctly

### Day 2: Formula Builder UI - Part 1 (Templates)
- [ ] Create FormulaTemplateLibrary component
- [ ] Implement 8 common formula templates (Hebrew + English)
  - Daily Sum, Success Rate, Avg Processing Time, Error Count
  - Growth Trend, Moving Average, Percentile, Unique Count
- [ ] Template selection UI with Hebrew descriptions
- [ ] Test: Templates display, selection works

### Day 3: Formula Builder UI - Part 2 (Visual Builder)
- [ ] Create VisualFormulaBuilder component
- [ ] Function dropdown (SUM, AVG, COUNT, MIN, MAX, etc.)
- [ ] Field selector (from schema)
- [ ] Time Window selector (Hebrew labels)
- [ ] Group By selector
- [ ] Generated formula preview
- [ ] Test: Visual builder generates formula strings

### Day 4: Filter Condition Builder
- [ ] Create FilterConditionBuilder component
- [ ] Field/Operator/Value UI (Hebrew operators)
- [ ] AND/OR logic builder
- [ ] Common filter templates (Hebrew)
- [ ] Filter preview
- [ ] Test: Filters build correctly, display properly

### Day 5: Aggregation Helper + Form Integration
- [ ] Create AggregationHelper component
- [ ] Time window selector with Hebrew labels
- [ ] Grouping field selector
- [ ] Aggregation function picker (Hebrew descriptions)
- [ ] Integrate all helpers into MetricConfigurationForm
- [ ] Test: Complete form works, data structure correct

---

## Week 2: Backend Services + API Integration (Days 6-10)

### Day 6: Backend Project Setup
- [ ] Create MetricsConfigurationService project (.NET 9)
- [ ] Set up MongoDB.Entities
- [ ] Create MetricConfiguration entity
- [ ] Create FormulaTemplate entity
- [ ] Configure OpenTelemetry (OTel Collector pattern)
- [ ] Add health checks
- [ ] Test: Service starts, MongoDB connects

### Day 7: Metrics Configuration API - CRUD
- [ ] Create MetricController with CRUD endpoints
- [ ] Create IMetricConfigurationRepository
- [ ] Implement MetricConfigurationService
- [ ] Add validation for metric configurations
- [ ] Test: CRUD operations via Swagger/Postman

### Day 8: Formula Validation & Preview API
- [ ] Create IMetricFormulaService
- [ ] Implement formula syntax validation
- [ ] Implement formula preview (sample data execution)
- [ ] Add formula template repository
- [ ] Create endpoints:
  - POST /api/metrics/formula/validate
  - POST /api/metrics/formula/preview
  - GET /api/metrics/formula/templates
- [ ] Test: Validation works, preview returns data

### Day 9: Frontend-Backend Integration
- [ ] Create metrics-api-client.ts
- [ ] Integrate CRUD operations in MetricsConfigurationList
- [ ] Connect Formula Builder with validation API
- [ ] Connect preview feature with backend
- [ ] Add error handling and loading states
- [ ] Test: End-to-end CRUD works, formula validation works

### Day 10: Statistical Analysis API
- [ ] Create IStatisticalAnalysisService
- [ ] Implement threshold suggestions (mean, median, stddev, percentiles)
- [ ] Implement trend analysis
- [ ] Create endpoints:
  - POST /api/metrics/thresholds/suggest
  - GET /api/metrics/{id}/trend
- [ ] Test: Statistical endpoints return valid data

---

## Week 3: Dashboard + Prometheus Integration (Days 11-15)

### Day 11: Metrics Collector Service
- [ ] Create MetricsCollectorService project
- [ ] Create IMetricExecutionService
- [ ] Implement MongoDB aggregation formula executor
- [ ] Register user-configured metrics with OpenTelemetry
- [ ] Push metrics to OTel Collector → Business Prometheus
- [ ] Test: Metrics appear in Business Prometheus

### Day 12: Grafana API Client
- [ ] Create GrafanaApiClient.ts
- [ ] Implement query methods:
  - queryBusinessMetric()
  - querySystemMetric()
  - queryLogs()
- [ ] Add authentication handling
- [ ] Test: Can query Grafana successfully

### Day 13: Dashboard UI - Part 1 (Layout & Widgets)
- [ ] Create MetricsDashboard page
- [ ] Create DashboardGrid component (drag & drop)
- [ ] Create widget types:
  - NumberWidget (single value display)
  - LineChartWidget (time series)
  - BarChartWidget (categorical)
  - PieChartWidget (distribution)
- [ ] Hebrew labels for all widgets
- [ ] Test: Dashboard displays, widgets render

### Day 14: Dashboard UI - Part 2 (Live Data Integration)
- [ ] Connect widgets to Grafana API
- [ ] Implement real-time data refresh
- [ ] Add date range selector (Hebrew)
- [ ] Add metric selector per widget
- [ ] Implement widget configuration panel
- [ ] Test: Live data displays in widgets

### Day 15: Polish, Testing & Documentation
- [ ] Add loading states and error handling
- [ ] Implement dashboard save/load
- [ ] Add export functionality (CSV, PNG)
- [ ] Complete Hebrew translations
- [ ] Create user guide (Hebrew + English)
- [ ] End-to-end testing
- [ ] Performance optimization
- [ ] Test: Full workflow works smoothly

---

## Technical Architecture

### Frontend Stack
```
React 18 + TypeScript
Ant Design 5.x (RTL Hebrew)
React Query (API state)
Recharts (visualizations)
i18next (Hebrew localization)
```

### Backend Stack
```
.NET 9 ASP.NET Core
MongoDB.Entities
OpenTelemetry SDK
MassTransit (optional event bus)
```

### Monitoring Stack
```
OpenTelemetry Collector (4317)
Business Prometheus (9091)
System Prometheus (9090)
Grafana (3000) - Query Layer
```

### Data Flow
```
User Configures Metric (UI)
    ↓
Saved to MongoDB (MetricConfiguration)
    ↓
MetricsCollectorService reads config
    ↓
Executes formula on MongoDB data
    ↓
Pushes via OpenTelemetry SDK
    ↓
OTel Collector routes to Business Prometheus
    ↓
Grafana queries Business Prometheus
    ↓
Dashboard displays via Grafana API
```

---

## File Structure

### Frontend Files
```
src/Frontend/src/
├── pages/
│   └── metrics/
│       ├── MetricsConfigurationList.tsx
│       ├── MetricConfigurationForm.tsx
│       └── MetricsDashboard.tsx
├── components/
│   └── metrics/
│       ├── FormulaTemplateLibrary.tsx
│       ├── VisualFormulaBuilder.tsx
│       ├── FilterConditionBuilder.tsx
│       ├── AggregationHelper.tsx
│       ├── FormulaExplainer.tsx
│       ├── FormulaPreviewPanel.tsx
│       └── dashboard/
│           ├── DashboardGrid.tsx
│           ├── NumberWidget.tsx
│           ├── LineChartWidget.tsx
│           ├── BarChartWidget.tsx
│           └── PieChartWidget.tsx
├── services/
│   ├── metrics-api-client.ts
│   └── grafana-api-client.ts
└── i18n/
    └── locales/
        └── he.json (updated)
```

### Backend Files
```
src/Services/
├── MetricsConfigurationService/
│   ├── Controllers/
│   │   └── MetricController.cs
│   ├── Services/
│   │   ├── IMetricConfigurationService.cs
│   │   ├── MetricConfigurationService.cs
│   │   ├── IMetricFormulaService.cs
│   │   ├── MetricFormulaService.cs
│   │   ├── IStatisticalAnalysisService.cs
│   │   └── StatisticalAnalysisService.cs
│   ├── Repositories/
│   │   ├── IMetricConfigurationRepository.cs
│   │   └── MetricConfigurationRepository.cs
│   ├── Models/
│   │   ├── Entities/
│   │   │   ├── MetricConfiguration.cs
│   │   │   └── FormulaTemplate.cs
│   │   ├── Requests/
│   │   │   ├── CreateMetricRequest.cs
│   │   │   ├── UpdateMetricRequest.cs
│   │   │   ├── ValidateFormulaRequest.cs
│   │   │   └── PreviewFormulaRequest.cs
│   │   └── Responses/
│   │       ├── MetricConfigurationResponse.cs
│   │       ├── FormulaValidationResult.cs
│   │       └── FormulaPreviewResult.cs
│   └── Program.cs
└── MetricsCollectorService/
    ├── Services/
    │   ├── IMetricExecutionService.cs
    │   └── MetricExecutionService.cs
    ├── Jobs/
    │   └── MetricCollectionJob.cs
    └── Program.cs
```

---

## Success Criteria

### Week 1 Complete
- ✅ Metrics Configuration page fully functional (UI only)
- ✅ Formula Builder works (templates + visual builder)
- ✅ Filter Builder works
- ✅ All Hebrew localization in place
- ✅ Frontend compiles and runs

### Week 2 Complete
- ✅ Backend API fully functional
- ✅ Formula validation works
- ✅ Formula preview works with sample data
- ✅ Statistical analysis endpoints work
- ✅ Frontend-backend integration complete
- ✅ Can create/read/update/delete metrics

### Week 3 Complete
- ✅ Metrics pushed to Business Prometheus
- ✅ Dashboard displays live data from Grafana
- ✅ All widget types work
- ✅ End-to-end flow complete
- ✅ Documentation complete
- ✅ Ready for production use

---

## Dependencies

### Existing Infrastructure (Already Working)
- ✅ MongoDB running
- ✅ DataSourceManagementService (port 5001)
- ✅ SchemaManagementService (schemas available)
- ✅ Frontend (port 3000)
- ✅ OpenTelemetry Collector pattern documented
- ✅ Dual Prometheus architecture documented

### To Be Set Up
- [ ] Business Prometheus instance (port 9091)
- [ ] Grafana with datasources configured
- [ ] MetricsConfigurationService (new)
- [ ] MetricsCollectorService (new)

---

## Risk Mitigation

### Technical Risks
1. **Formula complexity:** Start with simple formulas, expand gradually
2. **Performance:** Use MongoDB indexes, implement caching
3. **Prometheus integration:** Use existing OTel Collector pattern

### Timeline Risks
1. **Scope creep:** Stick to 3-week plan, defer advanced features
2. **Dependencies:** Can work on UI without backend initially
3. **Testing:** Build tests as we go, not at the end

---

## Next Steps

**Immediate Action (Day 1):**
1. Update he.json with all metrics-related Hebrew translations
2. Create MetricsConfigurationList page (shell)
3. Add route in App.tsx
4. Test: Navigate to new page

**Status:** Ready to begin implementation
**Start Date:** Today
**Expected Completion:** 3 weeks from today

---

**Document Created:** October 16, 2025
**Status:** Implementation Plan Ready
**Next:** Begin Day 1 - Metrics Configuration Page Setup
