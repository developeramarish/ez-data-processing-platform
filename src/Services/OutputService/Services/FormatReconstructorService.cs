// FormatReconstructorService.cs - Format Reconstruction Service
// Task-20: Multi-Destination Output Service
// Version: 1.0
// Date: December 1, 2025

using System.Text.Json;
using DataProcessing.Shared.Converters;
using Newtonsoft.Json.Linq;

namespace DataProcessing.Output.Services;

/// <summary>
/// Service for reconstructing original file formats from JSON data
/// Uses format reconstructors from Task-15
/// </summary>
public class FormatReconstructorService
{
    private readonly ILogger<FormatReconstructorService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public FormatReconstructorService(
        ILogger<FormatReconstructorService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Reconstruct content in the specified format from JSON
    /// </summary>
    /// <param name="jsonRecords">List of JSON records</param>
    /// <param name="outputFormat">Desired output format: "json", "csv", "xml", "excel", "original"</param>
    /// <param name="formatMetadata">Metadata from original format conversion</param>
    /// <returns>Reconstructed content as string</returns>
    public async Task<string> ReconstructAsync(
        List<JObject> jsonRecords,
        string outputFormat,
        Dictionary<string, object>? formatMetadata = null)
    {
        if (jsonRecords == null || !jsonRecords.Any())
        {
            _logger.LogWarning("No records provided for reconstruction");
            return string.Empty;
        }

        _logger.LogInformation(
            "Reconstructing {Count} records to format: {Format}",
            jsonRecords.Count,
            outputFormat);

        try
        {
            return outputFormat.ToLowerInvariant() switch
            {
                "json" => ReconstructJson(jsonRecords),
                "csv" => await ReconstructCsvAsync(jsonRecords, formatMetadata),
                "xml" => await ReconstructXmlAsync(jsonRecords, formatMetadata),
                "excel" => await ReconstructExcelAsync(jsonRecords, formatMetadata),
                "original" => await ReconstructOriginalAsync(jsonRecords, formatMetadata),
                _ => throw new ArgumentException($"Unsupported output format: {outputFormat}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to reconstruct format {Format}",
                outputFormat);
            throw;
        }
    }

    /// <summary>
    /// Reconstruct as JSON (simple serialization)
    /// </summary>
    private string ReconstructJson(List<JObject> jsonRecords)
    {
        var jsonArray = new JArray(jsonRecords);
        return jsonArray.ToString(Newtonsoft.Json.Formatting.Indented);
    }

    /// <summary>
    /// Reconstruct as CSV using JsonToCsvReconstructor
    /// </summary>
    private async Task<string> ReconstructCsvAsync(
        List<JObject> jsonRecords,
        Dictionary<string, object>? formatMetadata)
    {
        var reconstructor = _serviceProvider.GetService<JsonToCsvReconstructor>();

        if (reconstructor == null)
        {
            _logger.LogWarning("JsonToCsvReconstructor not registered, using simple JSON");
            return ReconstructJson(jsonRecords);
        }

        // Convert JObject list to JSON string
        var jsonArray = new JArray(jsonRecords);
        var jsonString = jsonArray.ToString(Newtonsoft.Json.Formatting.None);

        var csvMetadata = ExtractCsvMetadata(formatMetadata);

        // Call interface method and read stream
        using var stream = await reconstructor.ReconstructFromJsonAsync(jsonString, csvMetadata);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Reconstruct as XML using JsonToXmlReconstructor
    /// </summary>
    private async Task<string> ReconstructXmlAsync(
        List<JObject> jsonRecords,
        Dictionary<string, object>? formatMetadata)
    {
        var reconstructor = _serviceProvider.GetService<JsonToXmlReconstructor>();

        if (reconstructor == null)
        {
            _logger.LogWarning("JsonToXmlReconstructor not registered, using simple JSON");
            return ReconstructJson(jsonRecords);
        }

        // Convert JObject list to JSON string
        var jsonArray = new JArray(jsonRecords);
        var jsonString = jsonArray.ToString(Newtonsoft.Json.Formatting.None);

        var xmlMetadata = ExtractXmlMetadata(formatMetadata);

        // Call interface method and read stream
        using var stream = await reconstructor.ReconstructFromJsonAsync(jsonString, xmlMetadata);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Reconstruct as Excel using JsonToExcelReconstructor
    /// </summary>
    private async Task<string> ReconstructExcelAsync(
        List<JObject> jsonRecords,
        Dictionary<string, object>? formatMetadata)
    {
        var reconstructor = _serviceProvider.GetService<JsonToExcelReconstructor>();

        if (reconstructor == null)
        {
            _logger.LogWarning("JsonToExcelReconstructor not registered, using simple JSON");
            return ReconstructJson(jsonRecords);
        }

        // Convert JObject list to JSON string
        var jsonArray = new JArray(jsonRecords);
        var jsonString = jsonArray.ToString(Newtonsoft.Json.Formatting.None);

        var excelMetadata = ExtractExcelMetadata(formatMetadata);

        // Call interface method and read stream
        using var stream = await reconstructor.ReconstructFromJsonAsync(jsonString, excelMetadata);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Reconstruct in original format using metadata
    /// </summary>
    private async Task<string> ReconstructOriginalAsync(
        List<JObject> jsonRecords,
        Dictionary<string, object>? formatMetadata)
    {
        if (formatMetadata == null || !formatMetadata.ContainsKey("OriginalFormat"))
        {
            _logger.LogWarning("No OriginalFormat in metadata, defaulting to JSON");
            return ReconstructJson(jsonRecords);
        }

        var originalFormat = formatMetadata["OriginalFormat"].ToString();

        return originalFormat?.ToLowerInvariant() switch
        {
            "csv" => await ReconstructCsvAsync(jsonRecords, formatMetadata),
            "xml" => await ReconstructXmlAsync(jsonRecords, formatMetadata),
            "excel" => await ReconstructExcelAsync(jsonRecords, formatMetadata),
            "json" => ReconstructJson(jsonRecords),
            _ => ReconstructJson(jsonRecords)
        };
    }

    private Dictionary<string, object> ExtractCsvMetadata(Dictionary<string, object>? metadata)
    {
        if (metadata == null) return new Dictionary<string, object>();

        return new Dictionary<string, object>
        {
            ["Delimiter"] = metadata.ContainsKey("CsvDelimiter") ? metadata["CsvDelimiter"] : ",",
            ["HasHeaders"] = metadata.ContainsKey("CsvHasHeaders") ? metadata["CsvHasHeaders"] : true,
            ["ColumnHeaders"] = metadata.ContainsKey("CsvColumns") ? metadata["CsvColumns"] : new List<string>()
        };
    }

    private Dictionary<string, object> ExtractXmlMetadata(Dictionary<string, object>? metadata)
    {
        if (metadata == null) return new Dictionary<string, object>();

        return new Dictionary<string, object>
        {
            ["RootElement"] = metadata.ContainsKey("XmlRootElement") ? metadata["XmlRootElement"] : "root",
            ["ItemElement"] = metadata.ContainsKey("XmlItemElement") ? metadata["XmlItemElement"] : "item",
            ["AttributeFields"] = metadata.ContainsKey("XmlAttributes") ? metadata["XmlAttributes"] : new List<string>()
        };
    }

    private Dictionary<string, object> ExtractExcelMetadata(Dictionary<string, object>? metadata)
    {
        if (metadata == null) return new Dictionary<string, object>();

        return new Dictionary<string, object>
        {
            ["SheetName"] = metadata.ContainsKey("ExcelSheetName") ? metadata["ExcelSheetName"] : "Sheet1",
            ["HasHeaders"] = metadata.ContainsKey("ExcelHasHeaders") ? metadata["ExcelHasHeaders"] : true,
            ["ColumnHeaders"] = metadata.ContainsKey("ExcelColumns") ? metadata["ExcelColumns"] : new List<string>()
        };
    }
}
