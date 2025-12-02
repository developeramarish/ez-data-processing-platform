// XmlFileGenerator.cs - Generate XML test files
// Date: December 2, 2025

using System.Xml;
using System.Xml.Linq;
using TestDataGenerator.Templates;

namespace TestDataGenerator.Generators;

public class XmlFileGenerator
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

        // Create XML document
        var root = new XElement("Records");

        foreach (var record in records)
        {
            var recordElement = new XElement("Record");

            foreach (var kvp in record)
            {
                recordElement.Add(new XElement(kvp.Key, kvp.Value?.ToString() ?? ""));
            }

            root.Add(recordElement);
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);

        // Write to file
        using var writer = XmlWriter.Create(config.FilePath, new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  "
        });

        doc.Save(writer);

        Console.WriteLine($"    ✅ XML file: {config.FileName} ({config.RecordCount} records)");
    }
}
