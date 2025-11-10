#!/usr/bin/env python3
"""Comprehensive CRUD test for DataSource API"""

import requests
import json
import time

BASE_URL = "http://localhost:5001/api/v1/datasource"

def test_create():
    """Test CREATE operation"""
    print("\n=== TEST 1: CREATE ===")
    
    # Use timestamp to ensure unique name
    import datetime
    timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    
    datasource = {
        "name": f"Test DataSource {timestamp}",
        "nameEn": f"Test DataSource EN {timestamp}",
        "description": "מקור נתונים לבדיקה",
        "descriptionEn": "Test data source",
        "type": "File",
        "connectionString": "C:\\Test\\Data",
        "supplierName": "Test Supplier",
        "category": "Test Category",
        "cronExpression": "0 0 * * *",
        "filePath": "C:\\Test\\Data",
        "filePattern": "*.csv",
        "isActive": True,
        "jsonSchema": {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "type": "object",
            "properties": {
                "id": {"type": "string"},
                "name": {"type": "string"}
            },
            "required": ["id"]
        }
    }
    
    try:
        response = requests.post(
            BASE_URL,
            json=datasource,
            headers={"Content-Type": "application/json"}
        )
        
        print(f"Status Code: {response.status_code}")
        
        if response.status_code in [200, 201]:
            result = response.json()
            # API uses PascalCase: Data, ID, Name, etc.
            data = result.get('Data', result)
            print("✓ CREATE successful")
            print(f"  Created datasource with ID: {data.get('ID', 'N/A')}")
            print(f"  Name: {data.get('Name', 'N/A')}")
            print(f"  CronExpression: {data.get('CronExpression', 'N/A')}")
            return data.get('ID')
        else:
            print("✗ CREATE failed")
            print(f"  Response: {response.text}")
            return None
            
    except Exception as e:
        print(f"✗ Error: {str(e)}")
        return None

def test_read(datasource_id):
    """Test READ operation"""
    print("\n=== TEST 2: READ ===")
    
    if not datasource_id:
        print("✗ Cannot test READ - no datasource ID")
        return False
        
    try:
        response = requests.get(f"{BASE_URL}/{datasource_id}")
        
        print(f"Status Code: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            # API uses PascalCase
            data = result.get('Data', result)
            print("✓ READ successful")
            print(f"  ID: {data.get('ID', 'N/A')}")
            print(f"  Name: {data.get('Name', 'N/A')}")
            print(f"  CronExpression: {data.get('CronExpression', 'N/A')}")
            print(f"  JsonSchema: {'Present' if data.get('JsonSchema') else 'Missing'}")
            return True
        else:
            print("✗ READ failed")
            print(f"  Response: {response.text}")
            return False
            
    except Exception as e:
        print(f"✗ Error: {str(e)}")
        return False

def test_update(datasource_id):
    """Test UPDATE operation"""
    print("\n=== TEST 3: UPDATE ===")
    
    if not datasource_id:
        print("✗ Cannot test UPDATE - no datasource ID")
        return False
        
    # First get current data
    try:
        get_response = requests.get(f"{BASE_URL}/{datasource_id}")
        if get_response.status_code != 200:
            print("✗ Failed to get current data")
            return False
            
        current_data = get_response.json().get('Data', {})
        
        # Update the datasource
        update_data = {
            "id": datasource_id,
            "name": "Updated Test DataSource",
            "nameEn": "Updated Test DataSource EN",
            "description": current_data.get('Description'),
            "descriptionEn": current_data.get('DescriptionEn'),
            "type": current_data.get('Type'),
            "connectionString": current_data.get('ConnectionString'),
            "supplierName": current_data.get('SupplierName'),
            "category": current_data.get('Category'),
            "cronExpression": "0 12 * * *",  # Changed
            "filePath": current_data.get('FilePath'),
            "filePattern": current_data.get('FilePattern'),
            "isActive": current_data.get('IsActive'),
            "jsonSchema": current_data.get('JsonSchema')
        }
        
        response = requests.put(
            f"{BASE_URL}/{datasource_id}",
            json=update_data,
            headers={"Content-Type": "application/json"}
        )
        
        print(f"Status Code: {response.status_code}")
        
        if response.status_code == 200:
            print("✓ UPDATE successful")
            print(f"  Updated name to: 'Updated Test DataSource'")
            print(f"  Updated cronExpression to: '0 12 * * *'")
            return True
        else:
            print("✗ UPDATE failed")
            print(f"  Response: {response.text}")
            return False
            
    except Exception as e:
        print(f"✗ Error: {str(e)}")
        return False

def test_delete(datasource_id):
    """Test DELETE operation"""
    print("\n=== TEST 4: DELETE ===")
    
    if not datasource_id:
        print("✗ Cannot test DELETE - no datasource ID")
        return False
        
    try:
        response = requests.delete(
            f"{BASE_URL}/{datasource_id}?deletedBy=TestScript"
        )
        
        print(f"Status Code: {response.status_code}")
        
        if response.status_code == 200:
            print("✓ DELETE successful")
            return True
        else:
            print("✗ DELETE failed")
            print(f"  Response: {response.text}")
            return False
            
    except Exception as e:
        print(f"✗ Error: {str(e)}")
        return False

def verify_deleted(datasource_id):
    """Verify datasource was deleted"""
    print("\n=== TEST 5: VERIFY DELETION ===")
    
    if not datasource_id:
        print("✗ Cannot verify - no datasource ID")
        return False
        
    try:
        response = requests.get(f"{BASE_URL}/{datasource_id}")
        
        print(f"Status Code: {response.status_code}")
        
        if response.status_code == 404:
            print("✓ Datasource successfully deleted (404 Not Found)")
            return True
        else:
            print("✗ Datasource still exists!")
            print(f"  Response: {response.text}")
            return False
            
    except Exception as e:
        print(f"✗ Error: {str(e)}")
        return False

def verify_database_empty():
    """Verify database is empty"""
    print("\n=== TEST 6: VERIFY DATABASE EMPTY ===")
    
    try:
        response = requests.get(BASE_URL)
        
        if response.status_code == 200:
            result = response.json()
            
            # Check if it's a paginated response (API uses PascalCase)
            if 'Data' in result:
                data = result['Data']
                if isinstance(data, dict) and 'items' in data:
                    count = len(data['items'])
                elif isinstance(data, list):
                    count = len(data)
                else:
                    count = 1 if data else 0
            else:
                count = len(result) if isinstance(result, list) else (1 if result else 0)
            
            print(f"Total datasources: {count}")
            
            if count == 0:
                print("✓ Database is empty")
                return True
            else:
                print(f"✗ Database still has {count} datasources")
                return False
        else:
            print(f"✗ Failed to verify (Status: {response.status_code})")
            return False
            
    except Exception as e:
        print(f"✗ Error: {str(e)}")
        return False

def main():
    print("=" * 70)
    print("COMPREHENSIVE CRUD TEST")
    print("=" * 70)
    
    # Run tests
    datasource_id = test_create()
    time.sleep(1)  # Brief pause
    
    read_success = test_read(datasource_id) if datasource_id else False
    time.sleep(1)
    
    update_success = test_update(datasource_id) if datasource_id else False
    time.sleep(1)
    
    delete_success = test_delete(datasource_id) if datasource_id else False
    time.sleep(1)
    
    verify_success = verify_deleted(datasource_id) if datasource_id else False
    time.sleep(1)
    
    # Wait a bit longer for the database to sync after deletion
    time.sleep(2)
    
    empty_success = verify_database_empty()
    
    # Summary
    print("\n" + "=" * 70)
    print("TEST SUMMARY")
    print("=" * 70)
    
    tests_passed = sum([
        datasource_id is not None,
        read_success,
        update_success,
        delete_success,
        verify_success,
        empty_success
    ])
    
    print(f"Tests Passed: {tests_passed}/6")
    print(f"  1. CREATE:           {'✓' if datasource_id else '✗'}")
    print(f"  2. READ:             {'✓' if read_success else '✗'}")
    print(f"  3. UPDATE:           {'✓' if update_success else '✗'}")
    print(f"  4. DELETE:           {'✓' if delete_success else '✗'}")
    print(f"  5. VERIFY DELETION:  {'✓' if verify_success else '✗'}")
    print(f"  6. DATABASE EMPTY:   {'✓' if empty_success else '✗'}")
    
    print("=" * 70)
    
    if tests_passed == 6:
        print("✓ ALL TESTS PASSED - System is working correctly!")
    else:
        print(f"✗ {6 - tests_passed} TEST(S) FAILED")
    
    print("=" * 70)

if __name__ == "__main__":
    main()
