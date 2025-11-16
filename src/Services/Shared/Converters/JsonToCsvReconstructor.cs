using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;

namespace DataProcessing.Shared.Converters;

/// <summary>
/// Reconstructs CSV format from JSON
/// </summary>
public class JsonToCsvReconstructor : IFormatReconstructor
{
    private readonly ILogger<JsonToCsvReconstructor> _logger;

    public string TargetFormat => "csv";

    public JsonToCsvReconstructor(ILogger<JsonToCsvReconstructor> logger)
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

            var delimiter = metadata?.ContainsKey("Delimiter") == true 
                ? metadata["Delimiter"].ToString() ?? ","
                : ",";

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = true
            };

            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
            using (var csv = new CsvWriter(writer, config))
            {
                // Write headers
                var headers = records[0].Keys.ToList();
                foreach (var header in headers)
                {
                    csv.WriteField(header);
                }
                csv.NextRecord();

                // Write records
                foreach (var record in records)
                {
                    foreach (var header in headers)
                    {
                        csv.WriteField(record.ContainsKey(header) ? record[header] : "");
                    }
                    csv.NextRecord();
                }
            }

            memoryStream.Position = 0;
            return Task.FromResult<Stream>(memoryStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconstructing CSV from JSON");
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
