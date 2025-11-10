import pymongo

client = pymongo.MongoClient("mongodb://localhost:27017/")
db = client["ezplatform"]

print("\n=== Database Verification ===\n")
print(f"DataSources: {db['DataSource'].count_documents({})}")
print(f"Schemas: {db['Schema'].count_documents({})}")  
print(f"Metrics: {db['MetricConfiguration'].count_documents({})}")

print("\n=== Sample DataSource ===")
ds = db['DataSource'].find_one()
if ds:
    print(f"Name: {ds.get('Name')}")
    print(f"Category: {ds.get('Category')}")
    print(f"ID: {ds.get('_id')}")

print("\n=== Sample Schema ===")
schema = db['Schema'].find_one()
if schema:
    print(f"Name: {schema.get('DisplayName')}")
    print(f"DataSourceId: {schema.get('DataSourceId')}")

print("\n=== Sample Metric ===")
metric = db['MetricConfiguration'].find_one()
if metric:
    print(f"Name: {metric.get('DisplayName')}")
    print(f"Scope: {metric.get('Scope')}")

print("\n✅ Verification complete")
