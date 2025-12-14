using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Globalization;
using System.Text.Json;

namespace DataProcessing.Shared.Converters;

/// <summary>
/// Converts CSV format to JSON
/// </summary>
public class CsvToJsonConverter : IFormatConverter
{
    private readonly ILogger<CsvToJsonConverter> _logger;

    public string SourceFormat => "csv";

    public CsvToJsonConverter(ILogger<CsvToJsonConverter> logger)
    {
        _logger = logger;
    }

    public Task<string> ConvertToJsonAsync(
        Stream sourceStream,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var reader = new StreamReader(sourceStream);
            using var csv = new CsvReader(reader, config);

            // Get records as dynamic objects (all fields are strings by default)
            var records = csv.GetRecords<dynamic>().ToList();

            // Convert numeric and boolean strings to their actual types
            var convertedRecords = records.Select(record => ConvertTypes((IDictionary<string, object>)record)).ToList();

            return Task.FromResult(JsonSerializer.Serialize(convertedRecords));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting CSV to JSON");
            throw;
        }
    }

    /// <summary>
    /// Converts string values to appropriate types (numbers, booleans) based on content
    /// </summary>
    private Dictionary<string, object> ConvertTypes(IDictionary<string, object> record)
    {
        var converted = new Dictionary<string, object>();

        foreach (var kvp in record)
        {
            var value = kvp.Value?.ToString() ?? "";

            // Try to convert to number (int or decimal)
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numValue))
            {
                // Use decimal for values with decimal points, int for whole numbers
                if (value.Contains('.') || value.Contains(','))
                {
                    converted[kvp.Key] = numValue;
                }
                else if (int.TryParse(value, out var intValue))
                {
                    converted[kvp.Key] = intValue;
                }
                else
                {
                    converted[kvp.Key] = numValue;
                }
            }
            // Try to convert to boolean
            else if (bool.TryParse(value, out var boolValue))
            {
                converted[kvp.Key] = boolValue;
            }
            // Keep as string
            else
            {
                converted[kvp.Key] = value;
            }
        }

        return converted;
    }

    public async Task<bool> IsValidFormatAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var firstLine = await reader.ReadLineAsync(cancellationToken);
            stream.Position = 0;
            return !string.IsNullOrEmpty(firstLine) && firstLine.Contains(',');
        }
        catch
        {
            stream.Position = 0;
            return false;
        }
    }

    public async Task<Dictionary<string, object>> ExtractMetadataAsync(
        Stream sourceStream,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(sourceStream, leaveOpen: true);
        var firstLine = await reader.ReadLineAsync(cancellationToken);
        sourceStream.Position = 0;
        
        return new Dictionary<string, object>
        {
            ["Delimiter"] = ",",
            ["HasHeader"] = true,
            ["Encoding"] = "UTF-8",
            ["Headers"] = firstLine ?? ""
        };
    }
}
