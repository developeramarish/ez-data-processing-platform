using MongoDB.Bson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataProcessing.DataSourceManagement.Infrastructure;

/// <summary>
/// Custom JSON converter for BsonDocument to handle MongoDB BSON types during JSON serialization
/// </summary>
public class BsonDocumentJsonConverter : JsonConverter<BsonDocument>
{
    public override BsonDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        return BsonDocument.Parse(document.RootElement.GetRawText());
    }

    public override void Write(Utf8JsonWriter writer, BsonDocument value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Convert BsonDocument to a safe JSON representation
        var jsonDocument = ConvertBsonToJsonSafe(value);
        JsonSerializer.Serialize(writer, jsonDocument, options);
    }

    /// <summary>
    /// Safely converts BsonDocument to a JSON-compatible object, handling type mismatches
    /// </summary>
    private static object? ConvertBsonToJsonSafe(BsonValue value)
    {
        return value?.BsonType switch
        {
            BsonType.Document => ConvertBsonDocumentToJsonSafe(value.AsBsonDocument),
            BsonType.Array => value.AsBsonArray.Select(ConvertBsonToJsonSafe).ToArray(),
            BsonType.String => value.AsString,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => value.AsDouble,
            BsonType.Decimal128 => (double)value.AsDecimal128,
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.ToUniversalTime().ToString("O"), // ISO 8601 format
            BsonType.ObjectId => value.AsObjectId.ToString(),
            BsonType.Null => null,
            BsonType.Undefined => null,
            _ => value?.ToString() // Fallback for other types
        };
    }

    /// <summary>
    /// Safely converts BsonDocument to a dictionary, handling type mismatches
    /// </summary>
    private static Dictionary<string, object?> ConvertBsonDocumentToJsonSafe(BsonDocument document)
    {
        var result = new Dictionary<string, object?>();
        
        foreach (var element in document)
        {
            try
            {
                result[element.Name] = ConvertBsonToJsonSafe(element.Value);
            }
            catch (Exception)
            {
                // If conversion fails, store as string representation
                result[element.Name] = element.Value.ToString();
            }
        }
        
        return result;
    }
}
