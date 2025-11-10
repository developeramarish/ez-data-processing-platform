using DataProcessing.FilesReceiver.Infrastructure;
using DataProcessing.FilesReceiver.Models;
using ExcelDataReader;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using System.Text.Json;

namespace DataProcessing.FilesReceiver.Infrastructure;

/// <summary>
/// File reader for Excel format files (.xlsx, .xls)
/// </summary>
public class ExcelFileReader : IFileReader
{
    private readonly ILogger<ExcelFileReader> _logger;

    public ExcelFileReader(ILogger<ExcelFileReader> logger)
    {
        _logger = logger;
        
        // Required for ExcelDataReader
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public bool CanRead(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".xlsx" || 
               extension == ".xls" || 
               contentType?.Contains("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") == true ||
               contentType?.Contains("application/vnd.ms-excel") == true;
    }

    public Task<ProcessedFileInfo> ReadFileAsync(string filePath, string correlationId)
    {
        _logger.LogInformation("Reading Excel file: {FilePath}, CorrelationId: {CorrelationId}", filePath, correlationId);

        return Task.Run(() =>
        {
            try
        {
            var fileName = Path.GetFileName(filePath);
            var records = new List<Dictionary<string, object>>();

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // Convert to DataSet
            var result = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true // Use first row as column headers
                }
            });

            if (result.Tables.Count > 0)
            {
                var table = result.Tables[0]; // Read first worksheet
                var columnNames = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();

                foreach (DataRow row in table.Rows)
                {
                    var record = new Dictionary<string, object>();
                    
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        var columnName = columnNames[i];
                        var value = row[i];
                        
                        // Convert DBNull to empty string, keep other values
                        record[columnName] = value == DBNull.Value ? string.Empty : value?.ToString() ?? string.Empty;
                    }
                    
                    records.Add(record);
                }
            }

            // Convert to JSON bytes
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var jsonData = JsonSerializer.Serialize(records, jsonOptions);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            _logger.LogInformation("Successfully processed Excel file: {FileName}, Records: {RecordCount}", 
                fileName, records.Count);

            return new ProcessedFileInfo
            {
                FileName = fileName,
                FileContent = jsonBytes,
                ContentType = "application/json",
                FileSize = jsonBytes.Length,
                RecordCount = records.Count,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
                _logger.LogError(ex, "Failed to read Excel file: {FilePath}", filePath);
                throw;
            }
        });
    }
}
