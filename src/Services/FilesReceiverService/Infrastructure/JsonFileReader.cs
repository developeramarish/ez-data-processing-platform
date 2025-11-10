using DataProcessing.FilesReceiver.Infrastructure;
using DataProcessing.FilesReceiver.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DataProcessing.FilesReceiver.Infrastructure;

/// <summary>
/// File reader for JSON format files
/// </summary>
public class JsonFileReader : IFileReader
{
    private readonly ILogger<JsonFileReader> _logger;

    public JsonFileReader(ILogger<JsonFileReader> logger)
    {
        _logger = logger;
    }

    public bool CanRead(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".json" || contentType?.Contains("application/json") == true;
    }

    public async Task<ProcessedFileInfo> ReadFileAsync(string filePath, string correlationId)
    {
        _logger.LogInformation("Reading JSON file: {FilePath}, CorrelationId: {CorrelationId}", filePath, correlationId);

        try
        {
            var fileName = Path.GetFileName(filePath);
            var jsonContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);

            // Validate JSON and count records
            int recordCount = 0;
            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    recordCount = document.RootElement.GetArrayLength();
                }
                else if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    recordCount = 1; // Single object
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON format in file: {FilePath}", filePath);
                throw new InvalidOperationException($"Invalid JSON format in file {fileName}", ex);
            }

            _logger.LogInformation("Successfully processed JSON file: {FileName}, Records: {RecordCount}", 
                fileName, recordCount);

            return new ProcessedFileInfo
            {
                FileName = fileName,
                FileContent = jsonBytes,
                ContentType = "application/json",
                FileSize = jsonBytes.Length,
                RecordCount = recordCount,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read JSON file: {FilePath}", filePath);
            throw;
        }
    }
}
