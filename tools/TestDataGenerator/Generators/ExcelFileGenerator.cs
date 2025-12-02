// ExcelFileGenerator.cs - Generate Excel test files
// Date: December 2, 2025

using OfficeOpenXml;
using TestDataGenerator.Templates;

namespace TestDataGenerator.Generators;

public class ExcelFileGenerator
{
    public void Generate(TestFileConfig config)
    {
        // License is set globally in Program.cs
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

        // Create Excel file
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Transactions");

        // Write headers
        var headers = config.Template.GetHeaders();
        for (int col = 0; col < headers.Length; col++)
        {
            worksheet.Cells[1, col + 1].Value = headers[col];
            worksheet.Cells[1, col + 1].Style.Font.Bold = true;
        }

        // Write records
        for (int row = 0; row < records.Count; row++)
        {
            var record = records[row];
            for (int col = 0; col < headers.Length; col++)
            {
                var header = headers[col];
                if (record.ContainsKey(header))
                {
                    worksheet.Cells[row + 2, col + 1].Value = record[header];
                }
            }
        }

        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // Save to file
        var fileInfo = new FileInfo(config.FilePath);
        package.SaveAs(fileInfo);

        Console.WriteLine($"    ✅ Excel file: {config.FileName} ({config.RecordCount} records)");
    }
}
