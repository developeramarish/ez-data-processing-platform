#!/usr/bin/env python3
"""
Complete CRUD Verification Test
Tests all CRUD operations for DataSources with proper cleanup
"""

import requests
import json
import time
from datetime import datetime

BASE_URL = "http://localhost:5001/api/v1/DataSource"

# Use timestamp to avoid name conflicts
timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
test_name = f"CRUD_TEST_{timestamp}"

print("="*80)
print("COMPLETE CRUD VERIFICATION TEST")
print("="*80)
print(f"Test DataSource Name: {test_name}")
print()

# Complete test datasource
test_ds = {
    "name": test_name,
    "supplierName": "Test Supplier",
    "connectionString": f"file:///test/data/{timestamp}",
    "category": "Testing",
    "description": "Automated CRUD test datasource",
    "isActive": True,
    "filePath": f"/test/data/{timestamp}",
    "filePattern": "*.json",
    "cronExpression": "0 */15 * * * *",
    "jsonSchema": {
        "$schema": "http://json-schema.org/draft-07/schema#",
        "type": "object",
        "required": ["id"],
        "properties": {
            "id": {"type": "string"},
            "data": {"type": "string"}
        }
    }
}

results = {
    "CREATE": False,
    "READ": False,
    "UPDATE": False,
    "DELETE": False,
    "issues": []
}

try:
    # TEST 1: CREATE
    print("TEST 1: CREATE DataSource")
    print("-"*80)
    
    response = requests.post(BASE_URL, json=test_ds, timeout=10)
    
    if response.status_code not in [200, 201]:
        results["issues"].append(f"CREATE failed: {response.status_code} - {response.text[:200]}")
        print(f"✗ CREATE FAILED")
        print(f"  Status: {response.status_code}")
        print(f"  Response: {response.text[:300]}")
    else:
        result = response.json()
        
        # Get Data with capital D
        data = result.get('Data') or result.get('data', result)
        
        # Try all possible ID field names
        ds_id = (data.get('ID') or data.get('Id') or data.get('id') or 
                 data.get('_id') or data.get('_Id'))
        
        print(f"  Response IsSuccess: {result.get('IsSuccess')}")
        print(f"  Data keys: {data.keys() if isinstance(data, dict) else 'Not a dict'}")
        print(f"  Looking for ID in: ID, Id, id, _id, _Id")
        print(f"  Found ID: {ds_id}")
        
        # CRITICAL CHECKS
        print()
        print("  CRITICAL FIELD CHECK:")
        print(f"    CronExpression: {data.get('CronExpression')} (Expected: {test_ds['cronExpression']})")
        print(f"    JsonSchema: {data.get('JsonSchema')}")
        
        if ds_id:
            results["CREATE"] = True
            print(f"✓ CREATE SUCCESS")
            print(f"  ID: {ds_id}")
            
            # TEST 2: READ
            print()
            print("TEST 2: READ DataSource")
            print("-"*80)
            
            read_response = requests.get(f"{BASE_URL}/{ds_id}", timeout=5)
            
            if read_response.status_code != 200:
                results["issues"].append(f"READ failed: {read_response.status_code}")
                print(f"✗ READ FAILED")
            else:
                read_data = read_response.json().get('data', {})
                
                # Verify critical fields
                checks = {
                    "Name": read_data.get('name') == test_name,
                    "CronExpression": read_data.get('cronExpression') == test_ds['cronExpression'],
                    "FilePath": read_data.get('filePath') is not None,
                    "JsonSchema": len(read_data.get('jsonSchema', {})) > 0
                }
                
                if all(checks.values()):
                    results["READ"] = True
                    print(f"✓ READ SUCCESS - All fields present")
                    for field, value in checks.items():
                        print(f"    ✓ {field}")
                else:
                    failed = [k for k, v in checks.items() if not v]
                    results["issues"].append(f"READ incomplete: Missing {failed}")
                    print(f"✗ READ INCOMPLETE")
                    print(f"  Missing fields: {failed}")
                
                # TEST 3: UPDATE
                print()
                print("TEST 3: UPDATE CronExpression")
                print("-"*80)
                
                update_payload = {
                    "name": read_data.get('name'),
                    "supplierName": read_data.get('supplierName'),
                    "connectionString": read_data.get('filePath'),
                    "category": read_data.get('category'),
                    "description": read_data.get('description'),
                    "isActive": read_data.get('isActive'),
                    "filePath": read_data.get('filePath'),
                    "filePattern": read_data.get('filePattern'),
                    "cronExpression": "0 */30 * * * *",  # Change schedule
                    "jsonSchema": read_data.get('jsonSchema')
                }
                
                update_response = requests.put(f"{BASE_URL}/{ds_id}", json=update_payload, timeout=5)
                
                if update_response.status_code not in [200, 204]:
                    results["issues"].append(f"UPDATE failed: {update_response.status_code}")
                    print(f"✗ UPDATE FAILED")
                else:
                    # Verify update
                    time.sleep(0.5)
                    verify_response = requests.get(f"{BASE_URL}/{ds_id}", timeout=5)
                    verify_data = verify_response.json().get('data', {})
                    
                    if verify_data.get('cronExpression') == "0 */30 * * * *":
                        results["UPDATE"] = True
                        print(f"✓ UPDATE SUCCESS")
                        print(f"    CronExpression changed: {verify_data.get('cronExpression')}")
                    else:
                        results["issues"].append(f"UPDATE didn't persist: {verify_data.get('cronExpression')}")
                        print(f"✗ UPDATE DIDN'T PERSIST")
                
                # TEST 4: DELETE
                print()
                print("TEST 4: DELETE DataSource")
                print("-"*80)
                
                delete_response = requests.delete(f"{BASE_URL}/{ds_id}?deletedBy=AutoTest", timeout=5)
                
                if delete_response.status_code not in [200, 204]:
                    results["issues"].append(f"DELETE failed: {delete_response.status_code}")
                    print(f"✗ DELETE FAILED")
                else:
                    results["DELETE"] = True
                    print(f"✓ DELETE SUCCESS")
        else:
            results["issues"].append("No ID returned from CREATE")
            print(f"✗ No ID returned from CREATE")
    
    # Final Report
    print()
    print("="*80)
    print("FINAL RESULTS")
    print("="*80)
    
    total_tests = 4
    passed_tests = sum([results["CREATE"], results["READ"], results["UPDATE"], results["DELETE"]])
    
    print(f"Tests Passed: {passed_tests}/{total_tests}")
    print()
    print(f"  CREATE:  {'✓ PASS' if results['CREATE'] else '✗ FAIL'}")
    print(f"  READ:    {'✓ PASS' if results['READ'] else '✗ FAIL'}")
    print(f"  UPDATE:  {'✓ PASS' if results['UPDATE'] else '✗ FAIL'}")
    print(f"  DELETE:  {'✓ PASS' if results['DELETE'] else '✗ FAIL'}")
    print()
    
    if results["issues"]:
        print("ISSUES FOUND:")
        for issue in results["issues"]:
            print(f"  ✗ {issue}")
        print()
    
    if passed_tests == total_tests:
        print("="*80)
        print("✅ 100% SUCCESS - ALL CRUD OPERATIONS VERIFIED!")
        print("="*80)
        print()
        print("MongoDB: Working correctly")
        print("Backend: All endpoints functional")
        print("Data Persistence: Verified")
        print()
        print("You can now create production datasources with confidence!")
    else:
        print("="*80)
        print(f"⚠️  PARTIAL SUCCESS - {passed_tests}/{total_tests} tests passed")
        print("="*80)
        print()
        print("Review issues above and fix before proceeding")
    
    exit(0 if passed_tests == total_tests else 1)

except Exception as e:
    print()
    print(f"✗ CRITICAL ERROR: {e}")
    print()
    print("Possible causes:")
    print("  - DataSourceManagementService not running on port 5001")
    print("  - MongoDB not accessible")
    print("  - Network/connectivity issue")
    print()
    print("Check services are running:")
    print("  curl http://localhost:5001/health")
    exit(1)
