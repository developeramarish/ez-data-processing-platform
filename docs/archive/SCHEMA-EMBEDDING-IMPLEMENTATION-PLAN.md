# Schema Embedding Implementation Plan

## Current Situation

**Good News:** Schema is ALREADY embedded in DataSource entity!
```csharp
public class DataProcessingDataSource : DataProcessingBaseEntity
{
    // ... properties ...
    public BsonDocument JsonSchema { get; set; } = new(); // ← Schema is here!
    public int SchemaVersion { get; set; } = 1;
    // ... more properties ...
}
```

**The Issue:** UI treats schema as a separate entity with its own management page, when conceptually it should be part of datasource management.

---

## Implementation Plan

### Scope Clarification
**What needs to change:**
1. ✅ Backend already has embedded schema (JsonSchema property)
2. 🔄 Frontend UI organization (move schema editing to datasource form)
3. 🔄 Navigation (remove standalone Schema Management)
4. ✅ Keep existing schema editor component as-is (no changes to editing logic)

### Phase 1: Quick Win - Enable Metrics (15 minutes)
Since schema IS already in DataSource entity, just need to:
1. Update Python script to use JsonSchema property
2. Create datasource-specific metrics
3. Test metrics work

### Phase 2: UI Refactor (Later - 4-6 hours)
1. Embed SchemaBuilder component in DataSource form
2. Remove Schema Management page from navigation
3. Update workflows
4. Test thoroughly

---

## Immediate Action: Fix Metrics Script

The `JsonSchema` property exists but appears empty in API responses. Let me check if schemas are actually stored or if there's a serialization issue.

**Quick Test:**
```bash
curl http://localhost:5001/api/v1/datasource/ds005
```

Shows: `"JsonSchema": {}`

This means schemas are likely:
- Not populated (empty BsonDocuments), OR
- Not being returned due to serialization, OR  
- Managed separately in the current architecture

Since you mentioned UI shows schemas ARE assigned, they must be in the Schema collection, not in DataSource.JsonSchema.

---

## Revised Understanding

**Current Architecture:**
- DataSource entity has `JsonSchema` property (intended for embedded schema)
- BUT schemas are actually stored in separate Schema collection
- UI manages schemas separately
- Schema assignment stored in Schema.DataSourceIds[]

**This explains the confusion!**

---

## Recommendation: Two-Phase Approach

### Phase 1: Immediate (Option 1 - Query Schemas)
**Time:** 15 minutes
**Goal:** Get metrics working NOW

Update metrics script to:
1. Fetch all schemas
2. Build datasourceId → schemaId map
3. For each datasource, lookup its schema
4. Create metrics from schema fields

**Benefit:** Unblocks metrics work immediately

### Phase 2: Architecture Refactor (Later)
**Time:** 6-8 hours  
**Goal:** Proper architecture

Migrate schemas from separate collection into DataSource.JsonSchema:
1. Migration script to copy Schema.SchemaDefinition → DataSource.JsonSchema
2. Update UI to edit DataSource.JsonSchema inline
3. Remove Schema entity and collection
4. Remove Schema Management pages
5. Comprehensive testing

**Benefit:** Proper architecture, simpler system

---

## My Recommendation

**Do Phase 1 NOW** (query schemas approach)
- Gets you unblocked in 15 minutes
- Creates the datasource-specific metrics you need
- No backend changes required
- Works with current architecture

**Plan Phase 2 LATER** (embedded schema refactor)
- Proper architecture change
- Requires careful migration
- Should be done when you have time to test thoroughly
- Significant improvement but not urgent

---

## Implementation for Phase 1

Update `create-datasource-specific-metrics.py`:

```python
def fetch_all_schemas():
    response = requests.get(f"{SCHEMA_API_URL}")
    result = response.json()
    data = result.get('Data', {})
    return data.get('Items', [])

def build_datasource_schema_map(schemas):
    ds_to_schema = {}
    for schema in schemas:
        for ds_id in schema.get('DataSourceIds', []):
            ds_to_schema[ds_id] = schema['ID']
    return ds_to_schema

def main():
    schemas = fetch_all_schemas()
    ds_schema_map = build_datasource_schema_map(schemas)
    datasources = fetch_datasources()
    
    for ds in datasources:
        schema_id = ds_schema_map.get(ds['ID'])
        if schema_id:
            schema = fetch_schema(schema_id)
            # Create metrics from schema...
```

**This will work immediately with zero backend changes.**

---

## Your Decision

**For Metrics (NOW):**
- [ ] Implement Phase 1 (query schemas) - 15 min ← **Recommended**
- [ ] Wait for full refactor

**For Architecture (LATER):**
- [ ] Schedule Phase 2 refactor for next iteration
- [ ] Skip refactor, keep current architecture

What would you like me to do?
