// Update JSON datasource with Output configuration
db.DataProcessingDataSource.updateOne(
  { _id: ObjectId('6947f126c121d7dc9bffc8f6') },
  {
    $set: {
      Output: {
        Destinations: [
          {
            Id: 'e2e-json-folder-001',
            Name: 'E2E JSON Output Folder',
            Description: 'Output for JSON format E2E test',
            Type: 'folder',
            Enabled: true,
            OutputFormat: 'json',
            IncludeInvalidRecords: false,
            FolderConfig: {
              Path: '/mnt/external-test-data/output/E2E-003-JSON',
              FileNamePattern: '{filename}-output.json',
              CreateSubfolders: false,
              OverwriteExisting: true
            }
          }
        ],
        IncludeInvalidRecords: false,
        DefaultOutputFormat: 'json'
      }
    }
  }
);

// Update XML datasource with Output configuration
db.DataProcessingDataSource.updateOne(
  { _id: ObjectId('6947f013c121d7dc9bffc8f4') },
  {
    $set: {
      Output: {
        Destinations: [
          {
            Id: 'e2e-xml-folder-001',
            Name: 'E2E XML Output Folder',
            Description: 'Output for XML format E2E test',
            Type: 'folder',
            Enabled: true,
            OutputFormat: 'json',
            IncludeInvalidRecords: false,
            FolderConfig: {
              Path: '/mnt/external-test-data/output/E2E-003-XML',
              FileNamePattern: '{filename}-output.json',
              CreateSubfolders: false,
              OverwriteExisting: true
            }
          }
        ],
        IncludeInvalidRecords: false,
        DefaultOutputFormat: 'json'
      }
    }
  }
);

// Verify updates
print("JSON Datasource Output:");
printjson(db.DataProcessingDataSource.findOne({_id: ObjectId('6947f126c121d7dc9bffc8f6')}, {Output: 1}));

print("\nXML Datasource Output:");
printjson(db.DataProcessingDataSource.findOne({_id: ObjectId('6947f013c121d7dc9bffc8f4')}, {Output: 1}));
