using CsvHelper;
using CsvHelper.Configuration;
using DataProcessing.FilesReceiver.Infrastructure;
using DataProcessing.FilesReceiver.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace DataProcessing.FilesReceiver.Infrastructure;

/// <summary>
/// File reader for CSV format files
/// </summary>
public class CsvFileReader : IFileReader
{
    private readonly ILogger<CsvFileReader> _logger;

    public CsvFileReader(ILogger<CsvFileReader> logger)
    {
        _logger = logger;
    }

    public bool CanRead(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".csv" || contentType?.Contains("text/csv") == true;
    }

    public async Task<ProcessedFileInfo> ReadFileAsync(string filePath, string correlationId)
    {
        _logger.LogInformation("Reading CSV file: {FilePath}, CorrelationId: {CorrelationId}", filePath, correlationId);

        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            var records = new List<Dictionary<string, object>>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null, // Ignore missing fields
                HeaderValidated = null,   // Skip header validation
                BadDataFound = null       // Skip bad data
            };

            using var reader = new StreamReader(filePath, Encoding.UTF8);
            using var csv = new CsvReader(reader, config);

            // Read headers
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();

            // Read all records
            while (await csv.ReadAsync())
            {
                var record = new Dictionary<string, object>();
                
                for (int i = 0; i < headers.Length; i++)
                {
                    var header = headers[i];
                    var value = csv.TryGetField(i, out string? fieldValue) ? fieldValue : null;
                    record[header] = value ?? string.Empty;
                }
                
                records.Add(record);
            }

            // Convert to JSON bytes
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var jsonData = JsonSerializer.Serialize(records, jsonOptions);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            _logger.LogInformation("Successfully processed CSV file: {FileName}, Records: {RecordCount}", 
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
            _logger.LogError(ex, "Failed to read CSV file: {FilePath}", filePath);
            throw;
        }
    }
}
