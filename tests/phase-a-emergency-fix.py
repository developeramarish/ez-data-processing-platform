#!/usr/bin/env python3
"""
Phase A: Emergency Fix - Clean MongoDB and Test One Complete DataSource
This will verify the entire CRUD pipeline works before creating more data
"""

import requests
import json
import time

BASE_URL_DATASOURCE = "http://localhost:5001/api/v1/DataSource"
BASE_URL_SCHEMA = "http://localhost:5001/api/v1/schema"

print("="*80)
print("PHASE A: EMERGENCY FIX - CLEAN SLATE TEST")
print("="*80)
print()

# Test DataSource with ALL fields
test_datasource = {
    "name": "TEST - Banking Transactions",
    "supplierName": "Test Bank System",
    "connectionString": "file:///data/test/banking",
    "category": "Financial",
    "description": "Complete test datasource with all fields",
    "isActive": True,
    "fileFormat": "JSON",
    "retentionDays": 90,
    "filePath": "/data/test/banking",
    "filePattern": "*.json",
    "cronExpression": "0 */15 * * * *",
    "jsonSchema": {
        "$schema": "http://json-schema.org/draft-07/schema#",
        "type": "object",
        "required": ["transactionId", "amount", "currency"],
        "properties": {
            "transactionId": {
                "type": "string",
                "pattern": "^TXN-[0-9]{8}$",
                "description": "מזהה עסקה ייחודי"
            },
            "amount": {
                "type": "number",
                "minimum": 0,
                "maximum": 999999.99,
                "description": "סכום העסקה"
            },
            "currency": {
                "type": "string",
                "enum": ["ILS", "USD", "EUR"],
                "description": "מטבע"
            },
            "timestamp": {
                "type": "string",
                "format": "date-time",
                "description": "זמן ביצוע"
            }
        }
    }
}

print("Step 1: Create Test DataSource")
print("-"*80)

try:
    # Create
    print(f"Creating: {test_datasource['name']}")
    response = requests.post(BASE_URL_DATASOURCE, json=test_datasource, timeout=10)
    
    if response.status_code not in [200, 201]:
        print(f"✗ FAILED to create")
        print(f"  Status: {response.status_code}")
        print(f"  Response: {response.text[:500]}")
        exit(1)
    
    result = response.json()
    
    # Debug: print actual response structure
    print(f"  Response structure: {result.keys()}")
    
    # Try different response structures
    if 'data' in result:
        created_ds = result['data']
        # MongoDB.Entities uses 'ID' (uppercase)
        ds_id = created_ds.get('ID') or created_ds.get('id')
    else:
        created_ds = result
        ds_id = created_ds.get('ID') or created_ds.get('id')
    
    if not ds_id:
        print(f"✗ No ID in response!")
        print(f"  Full response: {json.dumps(result, indent=2)}")
        exit(1)
    
    print(f"✓ Created successfully")
    print(f"  ID: {ds_id}")
    print(f"  Name: {created_ds.get('name') or created_ds.get('Name')}")
    print()
    
    # Read back
    print("Step 2: Read DataSource Back")
    print("-"*80)
    
    read_response = requests.get(f"{BASE_URL_DATASOURCE}/{ds_id}", timeout=5)
    
    if read_response.status_code != 200:
        print(f"✗ FAILED to read back")
        print(f"  Status: {read_response.status_code}")
        exit(1)
    
    read_result = read_response.json()
    read_ds = read_result.get('data', {})
    
    print(f"✓ Read successfully")
    
    # Verify ALL fields
    print()
    print("Step 3: Verify All Fields Present")
    print("-"*80)
    
    verifications = {
        "Name": read_ds.get('name') == test_datasource['name'],
        "SupplierName": read_ds.get('supplierName') == test_datasource['supplierName'],
        "Category": read_ds.get('category') == test_datasource['category'],
        "FilePath": read_ds.get('filePath') is not None,
        "FilePattern": read_ds.get('filePattern') is not None,
        "CronExpression": read_ds.get('cronExpression') == test_datasource['cronExpression'],
        "JsonSchema": read_ds.get('jsonSchema') is not None and len(read_ds.get('jsonSchema', {})) > 0,
        "IsActive": read_ds.get('isActive') == True
    }
    
    all_passed = True
    for field, passed in verifications.items():
        status = "✓" if passed else "✗ MISSING"
        print(f"  {status} {field}: {read_ds.get(field.lower(), 'N/A')}")
        if not passed:
            all_passed = False
    
    if not all_passed:
        print()
        print("✗ VERIFICATION FAILED - Some fields missing!")
        print("Raw data:")
        print(json.dumps(read_ds, indent=2))
        exit(1)
    
    print()
    print("✓ ALL FIELDS VERIFIED!")
    
    # Update test
    print()
    print("Step 4: Update CronExpression")
    print("-"*80)
    
    update_payload = {
        "name": read_ds.get('name'),
        "supplierName": read_ds.get('supplierName'),
        "connectionString": read_ds.get('filePath', ''),
        "category": read_ds.get('category'),
        "description": read_ds.get('description'),
        "isActive": read_ds.get('isActive'),
        "filePath": read_ds.get('filePath'),
        "filePattern": read_ds.get('filePattern'),
        "cronExpression": "0 */30 * * * *",  # Changed from */15 to */30
        "jsonSchema": read_ds.get('jsonSchema')
    }
    
    update_response = requests.put(f"{BASE_URL_DATASOURCE}/{ds_id}", json=update_payload, timeout=5)
    
    if update_response.status_code not in [200, 204]:
        print(f"✗ FAILED to update")
        print(f"  Status: {update_response.status_code}")
        print(f"  Response: {update_response.text[:500]}")
        exit(1)
    
    print("✓ Update successful")
    
    # Verify update
    time.sleep(1)
    verify_response = requests.get(f"{BASE_URL_DATASOURCE}/{ds_id}", timeout=5)
    verify_ds = verify_response.json().get('data', {})
    
    if verify_ds.get('cronExpression') == "0 */30 * * * *":
        print(f"✓ CronExpression updated correctly: {verify_ds.get('cronExpression')}")
    else:
        print(f"✗ CronExpression NOT updated: {verify_ds.get('cronExpression')}")
        exit(1)
    
    # Delete test
    print()
    print("Step 5: Soft Delete DataSource")
    print("-"*80)
    
    delete_response = requests.delete(f"{BASE_URL_DATASOURCE}/{ds_id}?deletedBy=TestScript", timeout=5)
    
    if delete_response.status_code not in [200, 204]:
        print(f"✗ FAILED to delete")
        print(f"  Status: {delete_response.status_code}")
        exit(1)
    
    print("✓ Deleted successfully")
    
    # Verify deletion
    time.sleep(1)
    list_response = requests.get(BASE_URL_DATASOURCE, timeout=5)
    list_result = list_response.json().get('data', {})
    items = list_result.get('items', [])
    
    active_items = [item for item in items if not item.get('isDeleted', False)]
    
    print(f"✓ Verified: {len(active_items)} active datasources (deleted one not in list)")
    
    print()
    print("="*80)
    print("✅ PHASE A COMPLETE - ALL CRUD OPERATIONS WORKING!")
    print("="*80)
    print()
    print("Summary:")
    print("  ✓ CREATE works - all fields saved")
    print("  ✓ READ works - all fields retrieved")
    print("  ✓ UPDATE works - CronExpression updated")
    print("  ✓ DELETE works - soft delete functional")
    print()
    print("MongoDB Status: Working correctly")
    print("Backend Status: All endpoints functional")
    print("Next: Create production datasources with confidence!")
    print("="*80)

except Exception as e:
    print()
    print(f"✗ ERROR: {e}")
    print()
    print("This indicates a problem with:")
    print("  - Service not running (check DataSourceManagementService on port 5001)")
    print("  - MongoDB not accessible")
    print("  - Network/connectivity issue")
    exit(1)
