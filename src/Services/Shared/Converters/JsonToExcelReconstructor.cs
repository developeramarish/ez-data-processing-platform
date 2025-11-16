using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Text.Json;

namespace DataProcessing.Shared.Converters;

/// <summary>
/// Reconstructs Excel format (.xlsx) from JSON
/// </summary>
public class JsonToExcelReconstructor : IFormatReconstructor
{
    private readonly ILogger<JsonToExcelReconstructor> _logger;

    public string TargetFormat => "excel";

    public JsonToExcelReconstructor(ILogger<JsonToExcelReconstructor> logger)
    {
        _logger = logger;
    }

    public Task<Stream> ReconstructFromJsonAsync(
        string jsonContent,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var records = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);
            if (records == null || records.Count == 0)
            {
                return Task.FromResult<Stream>(new MemoryStream());
            }

            var sheetName = metadata?.ContainsKey("SheetName") == true
                ? metadata["SheetName"].ToString() ?? "Sheet1"
                : "Sheet1";

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // Write headers
            var headers = records[0].Keys.ToList();
            for (int col = 0; col < headers.Count; col++)
            {
                worksheet.Cells[1, col + 1].Value = headers[col];
            }

            // Write data
            for (int row = 0; row < records.Count; row++)
            {
                var record = records[row];
                for (int col = 0; col < headers.Count; col++)
                {
                    var header = headers[col];
                    if (record.ContainsKey(header))
                    {
                        var value = record[header];
                        worksheet.Cells[row + 2, col + 1].Value = value?.ToString() ?? "";
                    }
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var memoryStream = new MemoryStream();
            package.SaveAs(memoryStream);
            memoryStream.Position = 0;

            return Task.FromResult<Stream>(memoryStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconstructing Excel from JSON");
            throw;
        }
    }

    public Task<bool> CanReconstructAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        try
        {
            JsonDocument.Parse(jsonContent);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
