# UI Fixes Test Plan - Schema Management

## Critical Fixes Implemented

### ✅ Issue 1: Status Persistence Fixed
**Schema Management:**
- Fixed: Fetch current schema data before updating to preserve jsonSchemaContent
- Fixed: Added refetch() after update to show latest UI state
- Fixed: Proper error handling with UI reversion

**Data Source Management:**
- Fixed: Complete payload with all required fields (ConnectionString, etc.)
- Fixed: Fetch-before-update pattern
- Fixed: Added missing TypeScript interface properties

### ✅ Issue 2: Data Source Field Styling  
- Simplified to single-line display
- Removed second descriptive line
- Clean, right-justified appearance within control borders
- Maintains search functionality

### ✅ Issue 3: Monaco Editor Alternative
- Replaced Monaco Editor with reliable TextArea component
- Monospace font family for code editing
- Format and validation buttons working
- Real-time preview in side panel still uses Monaco (read-only)

### ✅ Issue 5: Schema Highlighting
- Navigation state passing with highlightDataSourceId
- Smooth scroll animation to highlighted row
- Visual fade-in/fade-out animation
- 3-second highlight duration

## Testing Checklist

### Schema Management Tests
- [ ] Load 6 schemas successfully
- [ ] Status dropdown changes persist to backend
- [ ] Data source assignment works without HTTP errors
- [ ] Data source unassignment (clear) works
- [ ] Data source reassignment shows confirmation dialog
- [ ] Field count displays correctly (8, 12, 5, 6 fields)
- [ ] Search functionality works
- [ ] Status filter works
- [ ] Create new schema navigates to builder
- [ ] Edit schema loads correctly
- [ ] Delete schema works (only when usageCount = 0)
- [ ] View schema modal shows all details
- [ ] Duplicate schema creates copy

### Data Source Management Tests
- [ ] Load 6 data sources successfully
- [ ] Status dropdown changes persist to backend  
- [ ] Schema links navigate to schema page with highlighting
- [ ] Highlighted row animates correctly
- [ ] Create new data source works
- [ ] Edit data source works
- [ ] Delete data source works
- [ ] Category tags display correctly

### Schema Builder Tests
- [ ] Visual editor: Add field works
- [ ] Visual editor: Edit field works
- [ ] Visual editor: Delete field works
- [ ] JSON editor: TextArea shows JSON content
- [ ] JSON editor: Format button works
- [ ] JSON editor: Validate button works
- [ ] JSON editor: Copy button works
- [ ] JSON preview pane displays real-time updates
- [ ] Load existing schema shows JSON content
- [ ] Save button persists changes

### Edge Cases
- [ ] Reassign data source from one schema to another
- [ ] Unassign data source then reassign
- [ ] Change status of schema with data source assigned
- [ ] Delete schema with usageCount > 0 (should fail)
- [ ] Invalid JSON in editor shows error
- [ ] Empty schema fields display correctly
- [ ] Large JSON schemas (100+ fields) perform well

## Known Issues to Monitor
1. `destroyOnClose` deprecation warning (minor, use destroyOnHidden in future)
2. React Router future flags warnings (minor, for v7 upgrade)
3. Missing manifest.json (404 - cosmetic only)

## Next Steps After Testing
1. If all tests pass: Proceed to service consolidation
2. If tests fail: Document failures and fix before consolidation
