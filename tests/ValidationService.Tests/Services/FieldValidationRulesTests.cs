// FieldValidationRulesTests.cs - Unit Tests for Field Validation Rules
// UNIT-007: Field Validation Rules Tests
// Version: 1.1
// Date: December 17, 2025
// Note: Uses Newtonsoft.Json.Schema for unit testing (simpler, no dynamic compilation)

using FluentAssertions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Xunit;

namespace DataProcessing.Validation.Tests.Services;

/// <summary>
/// Unit tests for field-level validation rules
/// Tests business-specific field validations including transaction IDs,
/// customer IDs, amounts, dates, and other domain-specific constraints
/// </summary>
public class FieldValidationRulesTests
{
    #region Transaction ID Field Tests

    [Theory]
    [InlineData("TXN-20251201-000001", true)]
    [InlineData("TXN-20251231-999999", true)]
    [InlineData("TXN-20250101-000000", true)]
    [InlineData("INVALID-ID", false)]
    [InlineData("TXN-2025121-000001", false)] // Wrong date length
    [InlineData("TXN-20251201-00001", false)] // Wrong sequence length
    [InlineData("txn-20251201-000001", false)] // Lowercase
    [InlineData("", false)]
    public void TransactionIdField_WithPattern_ValidatesCorrectly(string transactionId, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "TransactionId": {
                    "type": "string",
                    "pattern": "^TXN-[0-9]{8}-[0-9]{6}$"
                }
            },
            "required": ["TransactionId"]
        }
        """);

        var record = JObject.Parse($$"""{ "TransactionId": "{{transactionId}}" }""");

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid,
            $"TransactionId '{transactionId}' should be {(expectedValid ? "valid" : "invalid")}");
    }

    #endregion

    #region Customer ID Field Tests

    [Theory]
    [InlineData("CUST-0001", true)]
    [InlineData("CUST-9999", true)]
    [InlineData("CUST-1234", true)]
    [InlineData("CUSTOMER-001", false)]
    [InlineData("CUST-12345", false)] // Too many digits
    [InlineData("CUST-123", false)] // Too few digits
    [InlineData("cust-1234", false)] // Lowercase
    public void CustomerIdField_WithPattern_ValidatesCorrectly(string customerId, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "CustomerId": {
                    "type": "string",
                    "pattern": "^CUST-[0-9]{4}$"
                }
            },
            "required": ["CustomerId"]
        }
        """);

        var record = JObject.Parse($$"""{ "CustomerId": "{{customerId}}" }""");

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    #endregion

    #region Amount Field Tests

    [Theory]
    [InlineData(0.01, true)]
    [InlineData(100.00, true)]
    [InlineData(9999999.99, true)]
    [InlineData(0, false)] // Zero not allowed
    [InlineData(-100, false)] // Negative not allowed
    public void AmountField_WithMinimumConstraint_ValidatesCorrectly(double amount, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "Amount": {
                    "type": "number",
                    "exclusiveMinimum": 0
                }
            },
            "required": ["Amount"]
        }
        """);

        var record = new JObject { ["Amount"] = amount };

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Fact]
    public void AmountField_WithRangeConstraints_ValidatesMinMax()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "Amount": {
                    "type": "number",
                    "minimum": 10,
                    "maximum": 10000
                }
            },
            "required": ["Amount"]
        }
        """);

        // Act & Assert - Below minimum
        new JObject { ["Amount"] = 5 }.IsValid(schema).Should().BeFalse();

        // Act & Assert - At minimum
        new JObject { ["Amount"] = 10 }.IsValid(schema).Should().BeTrue();

        // Act & Assert - In range
        new JObject { ["Amount"] = 5000 }.IsValid(schema).Should().BeTrue();

        // Act & Assert - At maximum
        new JObject { ["Amount"] = 10000 }.IsValid(schema).Should().BeTrue();

        // Act & Assert - Above maximum
        new JObject { ["Amount"] = 15000 }.IsValid(schema).Should().BeFalse();
    }

    #endregion

    #region Currency Field Tests

    [Theory]
    [InlineData("USD", true)]
    [InlineData("EUR", true)]
    [InlineData("GBP", true)]
    [InlineData("ILS", true)]
    [InlineData("JPY", false)] // Not in enum
    [InlineData("usd", false)] // Case sensitive
    [InlineData("USDD", false)]
    [InlineData("", false)]
    public void CurrencyField_WithEnum_ValidatesAllowedValues(string currency, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "Currency": {
                    "type": "string",
                    "enum": ["USD", "EUR", "GBP", "ILS"]
                }
            },
            "required": ["Currency"]
        }
        """);

        var record = JObject.Parse($$"""{ "Currency": "{{currency}}" }""");

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    #endregion

    #region Status Field Tests

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Completed", true)]
    [InlineData("Processing", true)]
    [InlineData("Failed", true)]
    [InlineData("Cancelled", false)] // Not in enum
    [InlineData("pending", false)] // Case sensitive
    [InlineData("COMPLETED", false)] // Wrong case
    public void StatusField_WithEnum_ValidatesAllowedValues(string status, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "Status": {
                    "type": "string",
                    "enum": ["Pending", "Completed", "Processing", "Failed"]
                }
            },
            "required": ["Status"]
        }
        """);

        var record = JObject.Parse($$"""{ "Status": "{{status}}" }""");

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    #endregion

    #region Customer Name Field Tests

    [Theory]
    [InlineData("John Smith", true)]
    [InlineData("J", true)] // Minimum 1 character
    [InlineData("", false)] // Empty not allowed
    [InlineData("A very long name that exceeds one hundred characters in length which should definitely fail validation tests", false)]
    public void CustomerNameField_WithLengthConstraints_ValidatesCorrectly(string name, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "CustomerName": {
                    "type": "string",
                    "minLength": 1,
                    "maxLength": 100
                }
            },
            "required": ["CustomerName"]
        }
        """);

        var record = new JObject { ["CustomerName"] = name };

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    #endregion

    #region Email Field Tests

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.org", true)]
    [InlineData("user+tag@domain.co.uk", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@domain.com", false)]
    [InlineData("user@", false)]
    public void EmailField_WithFormat_ValidatesCorrectly(string email, bool expectedValid)
    {
        // Arrange - Using pattern as email format support varies
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "Email": {
                    "type": "string",
                    "pattern": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
                }
            },
            "required": ["Email"]
        }
        """);

        var record = new JObject { ["Email"] = email };

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    #endregion

    #region Date String Field Tests

    [Theory]
    [InlineData("2025-12-01", true)]
    [InlineData("2025-01-31", true)]
    [InlineData("2025-12-31", true)]
    [InlineData("25-12-01", false)] // Wrong year format
    [InlineData("2025/12/01", false)] // Wrong separator
    [InlineData("12-01-2025", false)] // Wrong order
    public void DateField_WithPattern_ValidatesISOFormat(string date, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "Date": {
                    "type": "string",
                    "pattern": "^[0-9]{4}-[0-9]{2}-[0-9]{2}$"
                }
            },
            "required": ["Date"]
        }
        """);

        var record = new JObject { ["Date"] = date };

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    #endregion

    #region Complex Record Validation Tests

    [Fact]
    public void CompleteTransactionRecord_WithAllFieldRules_ValidatesCorrectly()
    {
        // Arrange - Full transaction schema with all field rules
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "TransactionId": {
                    "type": "string",
                    "pattern": "^TXN-[0-9]{8}-[0-9]{6}$"
                },
                "CustomerId": {
                    "type": "string",
                    "pattern": "^CUST-[0-9]{4}$"
                },
                "CustomerName": {
                    "type": "string",
                    "minLength": 1,
                    "maxLength": 100
                },
                "Amount": {
                    "type": "number",
                    "exclusiveMinimum": 0,
                    "maximum": 1000000
                },
                "Currency": {
                    "type": "string",
                    "enum": ["USD", "EUR", "GBP", "ILS"]
                },
                "Status": {
                    "type": "string",
                    "enum": ["Pending", "Completed", "Processing", "Failed"]
                },
                "TransactionDate": {
                    "type": "string",
                    "pattern": "^[0-9]{4}-[0-9]{2}-[0-9]{2}$"
                }
            },
            "required": ["TransactionId", "CustomerId", "Amount", "Currency", "Status"]
        }
        """);

        var validRecord = JObject.Parse("""
        {
            "TransactionId": "TXN-20251201-000001",
            "CustomerId": "CUST-1001",
            "CustomerName": "John Smith",
            "Amount": 1500.50,
            "Currency": "USD",
            "Status": "Completed",
            "TransactionDate": "2025-12-01"
        }
        """);

        // Act
        var isValid = validRecord.IsValid(schema);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("INVALID-TXN", "CUST-1001", 100, "USD", "Completed", false)] // Invalid TransactionId
    [InlineData("TXN-20251201-000001", "INVALID", 100, "USD", "Completed", false)] // Invalid CustomerId
    [InlineData("TXN-20251201-000001", "CUST-1001", -100, "USD", "Completed", false)] // Negative Amount
    [InlineData("TXN-20251201-000001", "CUST-1001", 100, "JPY", "Completed", false)] // Invalid Currency
    [InlineData("TXN-20251201-000001", "CUST-1001", 100, "USD", "Cancelled", false)] // Invalid Status
    public void CompleteTransactionRecord_WithInvalidField_FailsValidation(
        string txnId, string custId, double amount, string currency, string status, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "TransactionId": { "type": "string", "pattern": "^TXN-[0-9]{8}-[0-9]{6}$" },
                "CustomerId": { "type": "string", "pattern": "^CUST-[0-9]{4}$" },
                "Amount": { "type": "number", "exclusiveMinimum": 0 },
                "Currency": { "type": "string", "enum": ["USD", "EUR", "GBP", "ILS"] },
                "Status": { "type": "string", "enum": ["Pending", "Completed", "Processing", "Failed"] }
            },
            "required": ["TransactionId", "CustomerId", "Amount", "Currency", "Status"]
        }
        """);

        var record = new JObject
        {
            ["TransactionId"] = txnId,
            ["CustomerId"] = custId,
            ["Amount"] = amount,
            ["Currency"] = currency,
            ["Status"] = status
        };

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    #endregion

    #region Optional vs Required Field Tests

    [Fact]
    public void OptionalField_WhenMissing_PassesValidation()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "required_field": { "type": "string" },
                "optional_field": { "type": "string" }
            },
            "required": ["required_field"]
        }
        """);

        var recordWithoutOptional = JObject.Parse("""{ "required_field": "value" }""");

        // Act
        var isValid = recordWithoutOptional.IsValid(schema);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void RequiredField_WhenMissing_FailsValidation()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "required_field": { "type": "string" },
                "optional_field": { "type": "string" }
            },
            "required": ["required_field"]
        }
        """);

        var recordMissingRequired = JObject.Parse("""{ "optional_field": "value" }""");

        // Act
        var isValid = recordMissingRequired.IsValid(schema);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Multiple Error Detection Tests

    [Fact]
    public void Record_WithMultipleInvalidFields_ReportsAllErrors()
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "TransactionId": { "type": "string", "pattern": "^TXN-[0-9]{8}-[0-9]{6}$" },
                "Amount": { "type": "number", "minimum": 0 },
                "Currency": { "type": "string", "enum": ["USD", "EUR", "GBP"] }
            },
            "required": ["TransactionId", "Amount", "Currency"]
        }
        """);

        var invalidRecord = new JObject
        {
            ["TransactionId"] = "INVALID",
            ["Amount"] = -100,
            ["Currency"] = "INVALID"
        };

        // Act
        var isValid = invalidRecord.IsValid(schema, out IList<string> errors);

        // Assert
        isValid.Should().BeFalse();
        errors.Count.Should().BeGreaterThan(1, "multiple validation errors should be reported");
    }

    #endregion

    #region Boundary Value Tests

    [Theory]
    [InlineData(0.001, true)] // Just above zero
    [InlineData(0.0001, true)] // Even closer to zero
    [InlineData(999999.99, true)] // Just below max
    [InlineData(1000000, true)] // At max
    [InlineData(1000000.01, false)] // Just above max
    public void AmountField_BoundaryValues_ValidatesCorrectly(double amount, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "Amount": {
                    "type": "number",
                    "exclusiveMinimum": 0,
                    "maximum": 1000000
                }
            },
            "required": ["Amount"]
        }
        """);

        var record = new JObject { ["Amount"] = amount };

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("X", true)] // Minimum length 1
    [InlineData("AB", true)] // 2 chars
    [InlineData("", false)] // Below minimum
    public void StringField_MinLengthBoundary_ValidatesCorrectly(string value, bool expectedValid)
    {
        // Arrange
        var schema = JSchema.Parse("""
        {
            "type": "object",
            "properties": {
                "Name": { "type": "string", "minLength": 1 }
            },
            "required": ["Name"]
        }
        """);

        var record = new JObject { ["Name"] = value };

        // Act
        var isValid = record.IsValid(schema);

        // Assert
        isValid.Should().Be(expectedValid);
    }

    #endregion
}
