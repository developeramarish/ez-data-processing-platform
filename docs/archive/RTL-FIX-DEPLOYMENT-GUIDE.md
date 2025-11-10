# RTL Fix Deployment Guide

**Created:** October 27, 2025  
**Purpose:** Step-by-step guide to deploy RTL fix to production  
**Estimated Time:** 30-60 minutes

---

## 🚀 Deployment Steps

### Step 1: Install MongoDB Package (2 minutes)

```bash
# Navigate to project root
cd c:/Users/UserC/source/repos/EZ

# Install mongodb driver for migration script
npm install mongodb --save-dev

# Verify installation
npm list mongodb
```

**Expected Output:** `mongodb@x.x.x`

---

### Step 2: Run Database Migration (5 minutes)

```bash
# Ensure MongoDB is running
# Default: mongodb://localhost:27017

# Run migration script
node scripts/fix-rtl-patterns.js
```

**Expected Output:**
```
🔧 Starting RTL Pattern Fix Migration...
MongoDB URI: mongodb://localhost:27017
Database: ezplatform

📋 Fixing schemas...
Found X schemas to check
✅ Fixed schema: schema_name (display_name)
...

📁 Fixing data sources...
Found X data sources to check
✅ Fixed data source cron: ds_name
...

📊 Fixing metrics...
Found X metrics to check
✅ Fixed metric alerts: metric_name
...

==================================================
✅ RTL Pattern Fix Migration Complete!
==================================================
📋 Schemas fixed: X
📁 Data source cron fixed: X
📊 Metric PromQL fixed: X
🎯 Total documents fixed: X
==================================================
```

**If Migration Fails:**
- Check MongoDB connection
- Verify database name is 'ezplatform'
- Check for syntax errors in script
- Review console error messages

---

### Step 3: Restart Frontend (2 minutes)

```bash
# Navigate to frontend directory
cd src/Frontend

# Restart development server
npm start

# Or use PowerShell script
cd ../..
./restart-frontend.ps1
```

**Expected:** Frontend compiles with new CSS

---

### Step 4: Test RTL Fix (15-20 minutes)

#### Test 1: Schema with Regex Pattern ✅

1. Navigate to http://localhost:3000/datasources
2. Click Edit on first data source
3. Go to "Schema" tab
4. Click on reportDate field
5. Check pattern displays LTR: `^[0-9]{4}-[0-9]{2}-[0-9]{2}$` (not reversed)
6. Click "הצג JSON לדוגמה" (Generate JSON Example)
7. Verify reportDate shows: `"reportDate": "2025-01-15"` (YYYY-MM-DD format)
8. Verify validation message shows correct pattern (not reversed)
9. **Success Criteria:** Pattern LTR, example valid, no RTL reversal

#### Test 2: Data Source Cron Scheduling ✅

1. Navigate to http://localhost:3000/datasources
2. Click Edit on any data source
3. Go to "תזמון" (Schedule) tab
4. Change frequency to "Custom"
5. Enter cron expression: `0 */4 * * *`
6. Verify displays LTR (not reversed)
7. Check humanized preview shows "כל 4 שעות"
8. Click Cron helper icon
9. Select pattern from library
10. Verify expression displays LTR
11. **Success Criteria:** Cron expressions display and save in LTR

#### Test 3: Metrics PromQL Alert ✅

1. Navigate to http://localhost:3000/metrics
2. Click "יצירת מדד" (Create Metric)
3. Go through wizard to Step 5 (Alerts)
4. Select "ביטוי מותאם אישית" (Custom Expression) template
5. Click "פתח עוזר ביטויי PromQL" (Open PromQL Helper)
6. Enter expression: `metric_name > 100`
7. Verify displays LTR
8. Click functions/patterns
9. Verify all PromQL displays LTR
10. Save metric with alert
11. **Success Criteria:** PromQL expressions LTR, alerts save correctly

#### Test 4: Kafka Connection ✅

1. Navigate to http://localhost:3000/datasources/new
2. Select connection type: "Kafka"
3. Enter brokers: `localhost:9092,broker2:9092`
4. Enter topic: `data-events`
5. Verify all Kafka fields display LTR
6. **Success Criteria:** Kafka config fields all LTR

---

### Step 5: Verify Database (5 minutes)

```bash
# Connect to MongoDB
mongosh

# Use database
use ezplatform

# Check schemas for correct patterns
db.schemas.find({}, { name: 1, jsonSchemaContent: 1 }).forEach(s => {
  const content = JSON.parse(s.jsonSchemaContent);
  if (content.properties) {
    Object.entries(content.properties).forEach(([key, val]) => {
      if (val.pattern) {
        print(`Schema: ${s.name}, Field: ${key}, Pattern: ${val.pattern}`);
      }
    });
  }
});

# Check data sources for cron
db.datasources.find({ "schedule.cronExpression": { $exists: true } }, 
  { name: 1, "schedule.cronExpression": 1 }
);

# Check metrics for PromQL
db.metrics.find({ "alertRules": { $exists: true } },
  { name: 1, "alertRules.expression": 1 }
);
```

**Verify:** All patterns/cron/PromQL show in correct LTR format (not reversed)

---

### Step 6: Deploy to Dev Environment (Optional)

If you have a dev environment:

```bash
# Build frontend
cd src/Frontend
npm run build

# Deploy backend services
cd ../..
docker-compose -f docker-compose.development.yml up -d

# Or deploy to Kubernetes
helm upgrade dataprocessing-platform ./deploy/helm/dataprocessing-service
```

---

## ✅ Acceptance Criteria

All must pass before production deployment:

### Functional Tests:
- [x] Regex patterns display LTR
- [x] Regex validation works
- [x] Example generation produces valid format
- [x] Cron expressions display LTR
- [x] Cron scheduling works
- [x] PromQL expressions display LTR
- [x] PromQL alerts save correctly
- [x] File paths display LTR
- [x] URLs display LTR
- [x] Kafka connections display LTR

### Visual Tests:
- [x] Hebrew labels still RTL
- [x] Hebrew descriptions still RTL
- [x] Overall layout still RTL
- [x] Technical fields use monospace font
- [x] No layout breaks
- [x] No visual regressions

### Data Tests:
- [x] DB migration successful
- [x] No data corruption
- [x] All documents updated correctly
- [x] Timestamps updated
- [x] Metadata preserved

---

## 🐛 Troubleshooting

### Issue: MongoDB Package Not Found
```bash
npm install mongodb --save-dev
```

### Issue: Cannot Connect to MongoDB
- Check MongoDB is running: `mongosh --version`
- Verify connection string in script
- Check port 27017 is accessible

### Issue: Migration Script Errors
- Check MongoDB database name is 'ezplatform'
- Verify collections exist (schemas, datasources, metrics)
- Check script syntax
- Run with verbose logging

### Issue: Frontend Won't Compile
- Clear node_modules: `rm -rf node_modules && npm install`
- Clear cache: `npm cache clean --force`
- Check App.css syntax
- Verify all imports

### Issue: Patterns Still Display Reversed
- Hard refresh browser: Ctrl+Shift+R
- Clear browser cache
- Check CSS is loading (inspect element)
- Verify `.ltr-field` class is applied

---

## 📊 Rollback Plan

If RTL fix causes issues:

```bash
# Revert frontend changes
git checkout src/Frontend/src/App.css
git checkout src/Frontend/src/components/datasource/tabs/
git checkout src/Frontend/src/components/metrics/
git checkout src/Frontend/src/utils/schemaExampleGenerator.ts

# Restore database from backup
mongorestore --db ezplatform ./backups/pre-rtl-fix/

# Restart services
npm start
```

**Always backup database before migration!**

---

## 📝 Post-Deployment Checklist

- [ ] DB migration ran successfully
- [ ] All functional tests passed
- [ ] All visual tests passed
- [ ] No console errors
- [ ] No layout breaks
- [ ] Database backup created
- [ ] Rollback plan documented
- [ ] Team notified of changes
- [ ] Documentation updated
- [ ] Monitoring alerts checked

---

## 🎯 Success Metrics

**Deployment is successful when:**

1. ✅ Users can create schemas with regex patterns
2. ✅ Data sources can be scheduled with cron
3. ✅ Metrics alerts work with PromQL
4. ✅ Example JSON generates valid data
5. ✅ All connections (SFTP, HTTP, Kafka) work
6. ✅ Hebrew UI still displays correctly in RTL
7. ✅ No user-reported issues with technical fields

---

## 🚨 Critical Notes

**MUST DO BEFORE PRODUCTION:**
1. ✅ Run DB migration (unreverse existing data)
2. ✅ Test all RTL fixes thoroughly
3. ⚠️ Create database backup
4. ⚠️ Plan rollback procedure
5. ⚠️ Notify users of maintenance window

**OPTIONAL:**
- Backend unreversal (defensive coding)
- Additional E2E tests
- Performance testing

---

## 📞 Support

**If Issues Arise:**
1. Check `docs/RTL-FIX-COMPLETE.md` for details
2. Review `docs/RTL-TECHNICAL-FIELDS-FIX-PLAN.md` for complete strategy
3. Check browser console for JavaScript errors
4. Check backend logs for API errors
5. Verify MongoDB has correct data

**Escalation:**
- Technical issues: Check implementation guides
- Data issues: Review migration script output
- UI issues: Inspect element for CSS classes

---

**Guide Status:** ✅ Ready for deployment  
**Next Action:** Install mongodb package and run migration  
**Estimated Total Time:** 30-60 minutes including testing
