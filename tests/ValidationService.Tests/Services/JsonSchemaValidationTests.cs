// JsonSchemaValidationTests.cs - Unit Tests for JSON Schema Validation Logic
// UNIT-006: JSON Schema Validation Logic Tests
// Version: 1.1
// Date: December 17, 2025
// Note: Uses Newtonsoft.Json.Schema for unit testing (simpler, no dynamic compilation)

using FluentAssertions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Xunit;

namespace DataProcessing.Validation.Tests.Services;

/// <summary>
/// Unit tests for JSON Schema validation concepts and record extraction
/// Tests validation logic patterns that mirror the production Corvus validator behavior
/// </summary>
public class JsonSchemaValidationTests
{
    #region Record Extraction Tests (From ValidationService)

    [Fact]
    public void ExtractRecords_FromJsonArray_ReturnsAllRecords()
    {
        // Arrange
        var jsonData = JToken.Parse("""
        [
            { "name": "John", "age": 30 },
            { "name": "Jane", "age": 25 }
        ]
        """);

        // Act
        var records = ExtractRecordsFromJson(jsonData);

        // Assert
        records.Should().HaveCount(2);
        records[0]["name"]?.ToString().Should().Be("John");
        records[1]["name"]?.ToString().Should().Be("Jane");
    }

    [Fact]
    public void ExtractRecords_FromSingleObject_ReturnsSingleRecord()
    {
        // Arrange
        var jsonData = JToken.Parse("""{ "name": "John", "age": 30 }""");

        // Act
        var records = ExtractRecordsFromJson(jsonData);

        // Assert
        records.Should().HaveCount(1);
        records[0]["name"]?.ToString().Should().Be("John");
    }

    [Fact]
    public void ExtractRecords_FromObjectWithNestedArray_ExtractsArrayItems()
    {
        // Arrange
        var jsonData = JToken.Parse("""
        {
            "transactions": [
                { "id": 1, "amount": 100 },
                { "id": 2, "amount": 200 }
            ]
        }
        """);

        // Act
        var records = ExtractRecordsFromJson(jsonData);

        // Assert
        records.Should().HaveCount(2);
        records[0]["id"]?.Value<int>().Should().Be(1);
    }

    [Fact]
    public void ExtractRecords_FromEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var jsonData = JToken.Parse("[]");

        // Act
        var records = ExtractRecordsFromJson(jsonData);

        // Assert
        records.Should().BeEmpty();
    }

    [Fact]
    public void ExtractRecords_FromPrimitiveValue_WrapsInObject()
    {
        // Arrange
        var jsonData = JToken.Parse("\"single value\"");

        // Act
        var records = ExtractRecordsFromJson(jsonData);

        // Assert
        records.Should().HaveCount(1);
        records[0]["value"]?.ToString().Should().Be("single value");
    }

    #endregion

    #region Schema Validation Logic Tests

    [Fact]
    public void ValidateRequiredFields_WithMissingField_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" },
                "age": { "type": "integer" }
            },
            "required": ["name", "age"]
        }
        """);

        var record = JObject.Parse("""{ "name": "John" }"""); // Missing age

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("age"));
    }

    [Fact]
    public void ValidateTypeConstraint_WithWrongType_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "age": { "type": "integer" }
            }
        }
        """);

        var record = JObject.Parse("""{ "age": "not a number" }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateMinLength_WithTooShortString_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "code": { "type": "string", "minLength": 3 }
            }
        }
        """);

        var record = JObject.Parse("""{ "code": "AB" }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateMaxLength_WithTooLongString_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "code": { "type": "string", "maxLength": 5 }
            }
        }
        """);

        var record = JObject.Parse("""{ "code": "ABCDEF" }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateMinimum_WithBelowMinValue_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "amount": { "type": "number", "minimum": 0 }
            }
        }
        """);

        var record = JObject.Parse("""{ "amount": -10 }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateMaximum_WithAboveMaxValue_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "rating": { "type": "integer", "maximum": 5 }
            }
        }
        """);

        var record = JObject.Parse("""{ "rating": 6 }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateEnum_WithInvalidValue_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "status": { "type": "string", "enum": ["Pending", "Completed"] }
            }
        }
        """);

        var record = JObject.Parse("""{ "status": "Invalid" }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidatePattern_WithNonMatchingPattern_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "txnId": { "type": "string", "pattern": "^TXN-[0-9]{8}-[0-9]{6}$" }
            }
        }
        """);

        var record = JObject.Parse("""{ "txnId": "INVALID" }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Valid Data Tests

    [Fact]
    public void ValidateRecord_WithAllValidData_ReturnsValid()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "name": { "type": "string", "minLength": 1 },
                "age": { "type": "integer", "minimum": 0 },
                "status": { "type": "string", "enum": ["active", "inactive"] }
            },
            "required": ["name", "age", "status"]
        }
        """);

        var record = JObject.Parse("""
        {
            "name": "John",
            "age": 30,
            "status": "active"
        }
        """);

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTransactionRecord_WithValidE2EData_ReturnsValid()
    {
        // Arrange - E2E transaction schema
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "TransactionId": { "type": "string", "pattern": "^TXN-[0-9]{8}-[0-9]{6}$" },
                "CustomerId": { "type": "string", "pattern": "^CUST-[0-9]{4}$" },
                "Amount": { "type": "number", "minimum": 0 },
                "Currency": { "type": "string", "enum": ["USD", "EUR", "GBP", "ILS"] },
                "Status": { "type": "string", "enum": ["Pending", "Completed", "Processing", "Failed"] }
            },
            "required": ["TransactionId", "CustomerId", "Amount", "Currency", "Status"]
        }
        """);

        var record = JObject.Parse("""
        {
            "TransactionId": "TXN-20251201-000001",
            "CustomerId": "CUST-1001",
            "Amount": 1500.50,
            "Currency": "USD",
            "Status": "Completed"
        }
        """);

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion

    #region Nullable Fields Tests

    [Fact]
    public void ValidateNullableField_WithNull_ReturnsValid()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "description": { "type": ["string", "null"] }
            }
        }
        """);

        var record = JObject.Parse("""{ "description": null }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateNonNullableField_WithNull_ReturnsError()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" }
            },
            "required": ["name"]
        }
        """);

        var record = JObject.Parse("""{ "name": null }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Additional Properties Tests

    [Fact]
    public void ValidateAdditionalProperties_WhenAllowed_AcceptsExtra()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" }
            },
            "additionalProperties": true
        }
        """);

        var record = JObject.Parse("""{ "name": "John", "extra": "field" }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateAdditionalProperties_WhenForbidden_RejectsExtra()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" }
            },
            "additionalProperties": false
        }
        """);

        var record = JObject.Parse("""{ "name": "John", "extra": "field" }""");

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Batch Validation Tests

    [Fact]
    public void ValidateBatch_WithMixedRecords_IdentifiesInvalid()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "amount": { "type": "number", "minimum": 0 }
            },
            "required": ["amount"]
        }
        """);

        var records = new[]
        {
            JObject.Parse("""{ "amount": 100 }"""),  // Valid
            JObject.Parse("""{ "amount": -50 }"""),  // Invalid - negative
            JObject.Parse("""{ "amount": 200 }"""),  // Valid
            JObject.Parse("""{ "name": "test" }""") // Invalid - missing amount
        };

        // Act
        var results = records.Select(r => r.IsValid(schema)).ToList();

        // Assert
        results.Should().Equal(true, false, true, false);
        results.Count(r => r).Should().Be(2); // 2 valid
        results.Count(r => !r).Should().Be(2); // 2 invalid
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void ValidateRecord_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "name": { "type": "string", "minLength": 1 },
                "age": { "type": "integer", "minimum": 0 },
                "status": { "type": "string", "enum": ["active", "inactive"] }
            },
            "required": ["name", "age", "status"]
        }
        """);

        var record = JObject.Parse("""
        {
            "name": "",
            "age": -5,
            "status": "invalid"
        }
        """);

        // Act
        var isValid = record.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Count.Should().BeGreaterThan(1);
    }

    #endregion

    #region Helper Methods (Mirror ValidationService Logic)

    /// <summary>
    /// Extracts records from JSON data - mirrors ValidationService.ExtractRecordsFromJson
    /// </summary>
    private List<JObject> ExtractRecordsFromJson(JToken jsonData)
    {
        var records = new List<JObject>();

        switch (jsonData.Type)
        {
            case JTokenType.Array:
                foreach (var item in jsonData.Children<JObject>())
                {
                    records.Add(item);
                }
                break;

            case JTokenType.Object:
                var obj = (JObject)jsonData;

                var arrayFound = false;
                foreach (var property in obj.Properties())
                {
                    if (property.Value.Type == JTokenType.Array)
                    {
                        foreach (var item in property.Value.Children<JObject>())
                        {
                            records.Add(item);
                        }
                        arrayFound = true;
                        break;
                    }
                }

                if (!arrayFound)
                {
                    records.Add(obj);
                }
                break;

            default:
                records.Add(new JObject { ["value"] = jsonData });
                break;
        }

        return records;
    }

    #endregion
}
