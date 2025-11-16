using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DataProcessing.Shared.Converters;

/// <summary>
/// Reconstructs XML format from JSON
/// </summary>
public class JsonToXmlReconstructor : IFormatReconstructor
{
    private readonly ILogger<JsonToXmlReconstructor> _logger;

    public string TargetFormat => "xml";

    public JsonToXmlReconstructor(ILogger<JsonToXmlReconstructor> logger)
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
            var jsonElement = JsonDocument.Parse(jsonContent).RootElement;
            var rootElementName = metadata?.ContainsKey("RootElement") == true
                ? metadata["RootElement"].ToString() ?? "Root"
                : "Root";

            XElement rootElement;
            
            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                rootElement = new XElement(rootElementName);
                foreach (var item in jsonElement.EnumerateArray())
                {
                    rootElement.Add(JsonToXElement("Item", item));
                }
            }
            else
            {
                rootElement = JsonToXElement(rootElementName, jsonElement);
            }

            var xDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                rootElement
            );

            var memoryStream = new MemoryStream();
            xDoc.Save(memoryStream);
            memoryStream.Position = 0;

            return Task.FromResult<Stream>(memoryStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconstructing XML from JSON");
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

    private static XElement JsonToXElement(string elementName, JsonElement jsonElement)
    {
        var element = new XElement(elementName);

        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in jsonElement.EnumerateObject())
                {
                    element.Add(JsonToXElement(property.Name, property.Value));
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in jsonElement.EnumerateArray())
                {
                    element.Add(JsonToXElement("Item", item));
                }
                break;

            case JsonValueKind.String:
                element.Value = jsonElement.GetString() ?? "";
                break;

            case JsonValueKind.Number:
                element.Value = jsonElement.GetRawText();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                element.Value = jsonElement.GetBoolean().ToString().ToLower();
                break;

            case JsonValueKind.Null:
                element.Value = "";
                break;
        }

        return element;
    }
}
