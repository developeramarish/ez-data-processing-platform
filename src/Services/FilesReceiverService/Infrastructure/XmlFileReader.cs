using DataProcessing.FilesReceiver.Infrastructure;
using DataProcessing.FilesReceiver.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace DataProcessing.FilesReceiver.Infrastructure;

/// <summary>
/// File reader for XML format files
/// </summary>
public class XmlFileReader : IFileReader
{
    private readonly ILogger<XmlFileReader> _logger;

    public XmlFileReader(ILogger<XmlFileReader> logger)
    {
        _logger = logger;
    }

    public bool CanRead(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".xml" || 
               contentType?.Contains("application/xml") == true ||
               contentType?.Contains("text/xml") == true;
    }

    public async Task<ProcessedFileInfo> ReadFileAsync(string filePath, string correlationId)
    {
        _logger.LogInformation("Reading XML file: {FilePath}, CorrelationId: {CorrelationId}", filePath, correlationId);

        try
        {
            var fileName = Path.GetFileName(filePath);
            var xmlContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            
            // Parse and convert XML to JSON-compatible structure
            var jsonData = ConvertXmlToJson(xmlContent);
            var jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            // Count records by analyzing the JSON structure
            int recordCount = CountRecordsInJson(jsonData);

            _logger.LogInformation("Successfully processed XML file: {FileName}, Records: {RecordCount}", 
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
            _logger.LogError(ex, "Failed to read XML file: {FilePath}", filePath);
            throw;
        }
    }

    private string ConvertXmlToJson(string xmlContent)
    {
        try
        {
            // Load XML document
            var doc = XDocument.Parse(xmlContent);
            
            // Convert to a structured object that can be serialized to JSON
            var result = ConvertElementToObject(doc.Root);

            // Serialize to JSON
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            return JsonSerializer.Serialize(result, jsonOptions);
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Invalid XML format");
            throw new InvalidOperationException("Invalid XML format", ex);
        }
    }

    private object ConvertElementToObject(XElement? element)
    {
        if (element == null)
            return new { };

        var result = new Dictionary<string, object>();

        // Add attributes as properties
        foreach (var attr in element.Attributes())
        {
            result[$"@{attr.Name.LocalName}"] = attr.Value;
        }

        // Group child elements by name
        var elementGroups = element.Elements().GroupBy(e => e.Name.LocalName);

        foreach (var group in elementGroups)
        {
            var elements = group.ToList();
            
            if (elements.Count == 1)
            {
                // Single element - convert directly
                var child = elements[0];
                if (child.HasElements || child.Attributes().Any())
                {
                    result[child.Name.LocalName] = ConvertElementToObject(child);
                }
                else
                {
                    result[child.Name.LocalName] = child.Value;
                }
            }
            else
            {
                // Multiple elements with same name - create array
                var array = elements.Select(e =>
                {
                    if (e.HasElements || e.Attributes().Any())
                    {
                        return ConvertElementToObject(e);
                    }
                    else
                    {
                        return (object)e.Value;
                    }
                }).ToArray();

                result[group.Key] = array;
            }
        }

        // If element has text content but no child elements, include the text
        if (!element.HasElements && !string.IsNullOrWhiteSpace(element.Value))
        {
            if (result.Any())
            {
                result["#text"] = element.Value;
            }
            else
            {
                return element.Value;
            }
        }

        return result;
    }

    private int CountRecordsInJson(string jsonData)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonData);
            return CountRecordsInJsonElement(document.RootElement);
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private int CountRecordsInJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                return element.GetArrayLength();
            
            case JsonValueKind.Object:
                // Look for arrays within the object that might represent records
                var maxArrayLength = 0;
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var arrayLength = property.Value.GetArrayLength();
                        maxArrayLength = Math.Max(maxArrayLength, arrayLength);
                    }
                }
                
                // If we found arrays, return the largest one's length, otherwise it's 1 record
                return maxArrayLength > 0 ? maxArrayLength : 1;
            
            default:
                return 1; // Single value
        }
    }
}
