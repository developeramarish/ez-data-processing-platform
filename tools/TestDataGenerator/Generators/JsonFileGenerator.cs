// JsonFileGenerator.cs - Generate JSON test files
// Date: December 2, 2025

using Newtonsoft.Json;
using TestDataGenerator.Templates;

namespace TestDataGenerator.Generators;

public class JsonFileGenerator
{
    public void Generate(TestFileConfig config)
    {
        var records = new List<Dictionary<string, object>>();

        // Generate valid records
        for (int i = 0; i < config.ValidRecords; i++)
        {
            records.Add(config.Template.GenerateValidRecord(i));
        }

        // Generate invalid records
        var errorTypes = Enum.GetValues<ErrorType>();
        for (int i = 0; i < config.InvalidRecords; i++)
        {
            var errorType = errorTypes[i % errorTypes.Length];
            records.Add(config.Template.GenerateInvalidRecord(config.ValidRecords + i, errorType));
        }

        // Create JSON structure
        var jsonData = new
        {
            metadata = new
            {
                schema = config.Template.GetSchemaName(),
                generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                recordCount = records.Count,
                validRecords = config.ValidRecords,
                invalidRecords = config.InvalidRecords
            },
            records = records
        };

        // Write to file with formatting
        var json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
        File.WriteAllText(config.FilePath, json);

        Console.WriteLine($"    ✅ JSON file: {config.FileName} ({config.RecordCount} records)");
    }
}
