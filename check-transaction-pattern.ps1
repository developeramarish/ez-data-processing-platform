# Check what pattern is actually stored in DB for transactionId field
mongosh --quiet ezplatform --eval "
  db.schemas.find({ 
    'jsonSchema.properties.transactionId': { \$exists: true } 
  }, { 
    'name': 1, 
    'jsonSchema.properties.transactionId': 1 
  }).forEach(doc => { 
    print('Schema:', doc.name);
    print('Pattern:', doc.jsonSchema.properties.transactionId.pattern);
    print('---');
  })
"
