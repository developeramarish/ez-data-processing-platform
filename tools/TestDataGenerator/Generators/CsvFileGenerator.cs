// CsvFileGenerator.cs - Generate CSV test files
// Date: December 2, 2025

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TestDataGenerator.Templates;

namespace TestDataGenerator.Generators;

public class CsvFileGenerator
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

        // Shuffle records so invalid ones are mixed in
        var shuffled = records.OrderBy(x => Guid.NewGuid()).ToList();

        // Write CSV file
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using var writer = new StreamWriter(config.FilePath);
        using var csv = new CsvWriter(writer, csvConfig);

        // Write headers
        var headers = config.Template.GetHeaders();
        foreach (var header in headers)
        {
            csv.WriteField(header);
        }
        csv.NextRecord();

        // Write records
        foreach (var record in shuffled)
        {
            foreach (var header in headers)
            {
                if (record.ContainsKey(header))
                {
                    csv.WriteField(record[header]);
                }
                else
                {
                    csv.WriteField(""); // Missing field
                }
            }
            csv.NextRecord();
        }

        Console.WriteLine($"    ✅ CSV file: {config.FileName} ({config.RecordCount} records)");
    }
}
