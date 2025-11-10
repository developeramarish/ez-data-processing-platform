#!/usr/bin/env python3
"""
Check actual regex patterns stored in MongoDB via DataSource API
"""
import requests
import json

print("🔍 Checking schema patterns in database...")
print()

# Get datasources from API
try:
    response = requests.get('http://localhost:5002/api/v1/datasource')
    response.raise_for_status()
    data = response.json()
    
    if 'items' in data and len(data['items']) > 0:
        first_ds = data['items'][0]
        print(f"📋 DataSource: {first_ds.get('name', 'N/A')}")
        print(f"   Category: {first_ds.get('category', 'N/A')}")
        print()
        
        # Check if jsonSchema exists
        if 'jsonSchema' in first_ds and first_ds['jsonSchema']:
            schema = first_ds['jsonSchema']
            
            # Function to find patterns recursively
            def find_patterns(obj, path="root"):
                patterns = []
                if isinstance(obj, dict):
                    for key, value in obj.items():
                        if key == 'pattern' and isinstance(value, str):
                            patterns.append((path, value))
                        elif isinstance(value, (dict, list)):
                            patterns.extend(find_patterns(value, f"{path}.{key}"))
                elif isinstance(obj, list):
                    for i, item in enumerate(obj):
                        patterns.extend(find_patterns(item, f"{path}[{i}]"))
                return patterns
            
            patterns = find_patterns(schema)
            
            if patterns:
                print(f"🔍 Found {len(patterns)} pattern(s):\n")
                for path, pattern in patterns:
                    print(f"  Location: {path}")
                    print(f"  Pattern:  '{pattern}'")
                    
                    # Check if pattern looks reversed
                    reversed_indicators = [
                        pattern.startswith('${'),  # Should start with ^{
                        pattern.endswith('^'),     # Should end with $
                        '}{' in pattern,           # Reversed quantifiers
                        pattern.startswith(']'),   # Starts with closing bracket
                        pattern.startswith('}')    # Starts with closing brace
                    ]
                    
                    if any(reversed_indicators):
                        print(f"  ⚠️  WARNING: Pattern appears REVERSED!")
                        # Try reversing it to see what it should be
                        reversed_pattern = pattern[::-1]
                        print(f"  ✓  Correct form: '{reversed_pattern}'")
                    else:
                        print(f"  ✅ Pattern looks correct (LTR)")
                    print()
            else:
                print("ℹ️  No pattern properties found in schema")
        else:
            print("ℹ️  No jsonSchema property in datasource")
    else:
        print("❌ No datasources found")
        
except requests.exceptions.ConnectionError:
    print("❌ Error: Cannot connect to API at http://localhost:5002")
    print("   Make sure DataSourceManagement service is running")
except Exception as e:
    print(f"❌ Error: {str(e)}")

print("\n✅ Check complete!")
