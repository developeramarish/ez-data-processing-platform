# How to See the Metrics UI - Visual Guide

## The Frontend is Already Running! ✅

Your frontend is running on **http://localhost:3000**

---

## Step-by-Step Guide

### Step 1: Open Your Browser
Navigate to: **http://localhost:3000**

### Step 2: Look at the Left Sidebar Menu
You should see these menu items (in Hebrew):
```
┌─────────────────────────┐
│ 🗄️  מקורות נתונים      │ ← Data Sources
│ 📄  ניהול Schema        │ ← Schema Management  
│ 📊  הגדרות מדדים        │ ← **METRICS CONFIG (NEW!)** ⭐
│ ⚠️  רשומות לא תקינות    │ ← Invalid Records
│ 📈  דשבורד              │ ← Dashboard
│ 🤖  עוזר AI             │ ← AI Assistant
│ 🔔  התרעות              │ ← Notifications
└─────────────────────────┘
```

### Step 3: Click on "הגדרות מדדים" (Metrics Configuration)
This is the **third item** in the menu with the **📊 BarChart icon**

### Step 4: You Should See This Page

```
┌────────────────────────────────────────────────────────────────┐
│                                                                │
│  ניהול מדדים                          [צור מדד חדש +]        │
│  הגדר וצפה במדדי עסק מותאמים אישית                           │
│                                                                │
│  [🔍 חפש מדדים...]                                            │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│ Table with 4 Sample Metrics:                                  │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ 1. ספירת קבצים מעובדים                                        │
│    files_processed_count                                       │
│    [ביצועים] [כללי (Global)] [● פעיל] 1,234                  │
│    כל מקורות הנתונים                                          │
│                                                                │
│ 2. ממוצע זמן עיבוד                                            │
│    processing_duration_avg                                     │
│    [ביצועים] [כללי (Global)] [● פעיל] 45.3                   │
│    כל מקורות הנתונים                                          │
│                                                                │
│ 3. סכום עסקאות כולל                                           │
│    transaction_amount_total                                    │
│    [פיננסי] [ספציפי (Custom)] [● פעיל] 1,250,000             │
│    בנק לאומי - עסקאות                                         │
│                                                                │
│ 4. שיעור שגיאות אימות                                         │
│    validation_error_rate                                       │
│    [איכות] [ספציפי (Custom)] [● פעיל] 2.5                    │
│    מערכת CRM - לקוחות                                         │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

---

## What to Look For

### ✅ Color-Coded Scope Tags
- **Gold/Yellow tags:** "כללי (Global)" - applies to all data sources
- **Green tags:** "ספציפי (Custom)" - specific to one data source

### ✅ Status Indicators
- **Green dot (●):** פעיל (Active)
- **Gray dot (○):** לא פעיל (Inactive)
- **Yellow dot:** טיוטה (Draft)
- **Red dot:** שגיאה (Error)

### ✅ Data Source Information
- For **Global metrics:** "כל מקורות הנתונים" (All data sources)
- For **Custom metrics:** Shows specific data source name (e.g., "בנק לאומי - עסקאות")

### ✅ Interactive Features to Test

1. **Search Bar:**
   - Type "סכום" to filter for sum metrics
   - Type "files" to search by English name
   - Clear search to see all metrics again

2. **Filter Dropdowns:**
   - Click column headers to filter
   - Category filter: ביצועים, פיננסי, איכות
   - Scope filter: כללי, ספציפי
   - Status filter: פעיל, לא פעיל

3. **Action Buttons on Each Row:**
   - **⏸️ Pause button:** Deactivate metric (turns gray)
   - **▶️ Play button:** Activate metric (turns green)
   - **⋮ Menu button (3 dots):**
     - ✏️ ערוך (Edit)
     - 📋 שכפל (Duplicate) - **TRY THIS!** ⭐
     - 📈 צפה בהיסטוריה (View History)
     - 🗑️ מחק (Delete)

4. **Try Duplicating a Metric:**
   - Click the ⋮ menu on any metric
   - Click "שכפל" (Duplicate)
   - You'll see a confirmation dialog in Hebrew
   - Click "שכפל" to confirm
   - **Watch the table update immediately!** A new metric appears at the top with "(עותק)" suffix

5. **Try Deleting a Metric:**
   - Click ⋮ menu → "מחק" (Delete)
   - Confirm deletion
   - **Watch the row disappear immediately!**

---

## Troubleshooting

### If you don't see the page:

1. **Refresh your browser** (Ctrl+R or F5)
2. Check the URL is: `http://localhost:3000`
3. Click on the sidebar menu item "הגדרות מדדים"

### If the frontend isn't running:

The terminal shows it's waiting for your input about port 3000.
- Just press **Enter** or type **n** to keep it on port 3000
- Or type **y** to use a different port

### To Kill the Old Process and Restart:

```powershell
# Find and kill process on port 3000
netstat -ano | findstr :3000
taskkill /PID <process_id> /F

# Then restart
cd src/Frontend
npm start
```

---

## What You'll See

### Current Features (Days 1-2):
✅ Metrics Configuration list page with mock data
✅ 4 sample metrics (2 global, 2 custom)
✅ Full Hebrew UI
✅ Interactive table (search, filter, sort)
✅ Real-time UI updates (duplicate/delete work immediately)
✅ Color-coded scope and status indicators

### Not Yet Visible (Coming in Day 3+):
❌ Formula Template Library (component exists but not integrated yet)
❌ Create/Edit forms (routes exist but pages not built yet)
❌ Dashboard with charts (Week 3)
❌ Real API integration (backend not built yet)

---

## Quick Test Checklist

Once you're on the Metrics Configuration page:

- [ ] Can you see 4 metrics in the table?
- [ ] Do you see Hebrew text throughout?
- [ ] Can you search for metrics?
- [ ] Can you filter by category/scope/status?
- [ ] Can you duplicate a metric and see it appear immediately?
- [ ] Can you delete a metric and see it disappear?
- [ ] Can you toggle active/inactive status?

---

## Current URL to Visit

**http://localhost:3000/metrics-config**

Or click the sidebar menu: **"הגדרות מדדים"** (third item with 📊 icon)

---

**Created:** October 16, 2025, 4:22 PM  
**Status:** Frontend ready to view  
**Action:** Navigate to http://localhost:3000 and click "הגדרות מדדים"
