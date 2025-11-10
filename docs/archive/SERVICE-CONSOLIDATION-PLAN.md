# Service Consolidation Plan
## SchemaManagementService → DataSourceManagementService

## Current Status: ✅ ALL CRITICAL ISSUES FIXED & VERIFIED

All 5 critical issues have been successfully fixed and tested. The system is stable and ready for consolidation.

## Why Consolidate?
- Both services share the same MongoDB database
- 1-to-1 relationship between Schema and DataSource is confirmed
- Reduces infrastructure complexity (3 services → 2 services)
- Simplifies deployment and maintenance
- All schema operations logically belong with data source management

## Consolidation Steps

### Phase 1: Copy Schema Components ✅ STARTED
1. ✅ Created `DataSourceManagementService/Controllers/SchemaController.cs`
2. ⏳ Create `DataSourceManagementService/Models/Schema/` directory
3. ⏳ Copy all request models from SchemaManagementService
4. ⏳ Copy all response models from SchemaManagementService
5. ⏳ Copy service interfaces and implementations
6. ⏳ Update namespaces to DataProcessing.DataSourceManagement.Models.Schema

### Phase 2: Update Dependencies
1. Add Newtonsoft.Json.Schema package to DataSourceManagementService
2. Update Program.cs to register schema services
3. Update MongoDB seed data to create schemas in consolidated service

### Phase 3: Update Frontend
1. Update `src/Frontend/src/services/schema-api-client.ts`
   - Change all schema API calls from port 5050 to port 5001
   - Update base URL: `http://localhost:5050` → `http://localhost:5001`
2. Test all schema operations through updated frontend

### Phase 4: Verification Testing
1. Test schema CRUD operations
2. Test data source CRUD operations
3. Verify schema-datasource assignments still work
4. Test all edge cases again

### Phase 5: Cleanup
1. Stop SchemaManagementService
2. Archive SchemaManagementService folder
3. Remove SchemaManagementService from solution
4. Update documentation

## Files to Copy

### Models (SchemaManagementService → DataSourceManagementService/Models/Schema/)
- CreateSchemaRequest.cs
- UpdateSchemaRequest.cs
- DuplicateSchemaRequest.cs
- ValidateJsonSchemaRequest.cs
- TestRegexRequest.cs
- ValidationResult.cs
- DataValidationResult.cs
- RegexTestResult.cs
- SchemaTemplate.cs
- SchemaUsageStatistics.cs

### Services (SchemaManagementService → DataSourceManagementService/Services/Schema/)
- ISchemaService.cs
- SchemaService.cs
- ISchemaValidationService.cs
- SchemaValidationService.cs

### Dependencies
- Ensure DataProcessing.Shared.Entities.DataProcessingSchema is used (already exists)
- Add Newtonsoft.Json.Schema NuGet package

## Testing Checklist
- [ ] Schema list loads (GET /api/v1/schema)
- [ ] Schema details load (GET /api/v1/schema/{id})
- [ ] Schema creation works (POST /api/v1/schema)
- [ ] Schema update works (PUT /api/v1/schema/{id})
- [ ] Schema deletion works (DELETE /api/v1/schema/{id})
- [ ] Schema duplication works (POST /api/v1/schema/{id}/duplicate)
- [ ] Schema publish works (POST /api/v1/schema/{id}/publish)
- [ ] Data source list still works
- [ ] Data source CRUD still works
- [ ] Schema-DataSource assignments persist

## Rollback Plan
If consolidation fails:
1. Revert SchemaController.cs deletion from DataSourceManagementService
2. Restart SchemaManagementService on port 5050
3. Revert frontend API endpoint changes
4. System returns to original state

## Notes
- All 6 schemas currently in database will automatically be available through consolidated service
- MongoDB database is shared, so no data migration needed
- Frontend changes are minimal (just port number)
