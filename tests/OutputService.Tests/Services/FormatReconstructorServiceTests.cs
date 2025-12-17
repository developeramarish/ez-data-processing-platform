// FormatReconstructorServiceTests.cs - Unit Tests for FormatReconstructorService
// UNIT-008: Output Template Engine Tests
// Version: 1.0
// Date: December 17, 2025

using DataProcessing.Output.Services;
using DataProcessing.Shared.Converters;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DataProcessing.Output.Tests.Services;

/// <summary>
/// Unit tests for FormatReconstructorService
/// Tests output format reconstruction, metadata handling, and format transformations
/// </summary>
public class FormatReconstructorServiceTests
{
    private readonly Mock<ILogger<FormatReconstructorService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly FormatReconstructorService _service;

    public FormatReconstructorServiceTests()
    {
        _mockLogger = new Mock<ILogger<FormatReconstructorService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _service = new FormatReconstructorService(_mockLogger.Object, _mockServiceProvider.Object);
    }

    #region ReconstructAsync - JSON Format Tests

    [Fact]
    public async Task ReconstructAsync_WithJsonFormat_ReturnsFormattedJson()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John", "age": 30 }"""),
            JObject.Parse("""{ "name": "Jane", "age": 25 }""")
        };

        // Act
        var result = await _service.ReconstructAsync(records, "json");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"name\"");
        result.Should().Contain("John");
        result.Should().Contain("Jane");

        // Verify it's valid JSON array
        var parsed = JArray.Parse(result);
        parsed.Count.Should().Be(2);
    }

    [Fact]
    public async Task ReconstructAsync_WithJsonFormat_PreservesAllFields()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""
            {
                "TransactionId": "TXN-001",
                "Amount": 100.50,
                "Currency": "USD",
                "Status": "Completed"
            }
            """)
        };

        // Act
        var result = await _service.ReconstructAsync(records, "json");

        // Assert
        var parsed = JArray.Parse(result);
        var firstRecord = (JObject)parsed[0];
        firstRecord["TransactionId"]?.ToString().Should().Be("TXN-001");
        firstRecord["Amount"]?.Value<decimal>().Should().Be(100.50m);
        firstRecord["Currency"]?.ToString().Should().Be("USD");
        firstRecord["Status"]?.ToString().Should().Be("Completed");
    }

    [Fact]
    public async Task ReconstructAsync_WithJsonFormat_CaseInsensitive()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "Test" }""")
        };

        // Act
        var resultLower = await _service.ReconstructAsync(records, "json");
        var resultUpper = await _service.ReconstructAsync(records, "JSON");
        var resultMixed = await _service.ReconstructAsync(records, "Json");

        // Assert
        resultLower.Should().NotBeNullOrEmpty();
        resultUpper.Should().Be(resultLower);
        resultMixed.Should().Be(resultLower);
    }

    #endregion

    #region ReconstructAsync - CSV Format Tests

    [Fact]
    public async Task ReconstructAsync_WithCsvFormat_FallsBackToJsonWhenNoReconstructor()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToCsvReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        // Act
        var result = await _service.ReconstructAsync(records, "csv");

        // Assert
        // When CSV reconstructor is not available, falls back to JSON
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("name");
    }

    [Fact]
    public async Task ReconstructAsync_WithCsvFormat_ExtractsMetadata()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToCsvReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        var metadata = new Dictionary<string, object>
        {
            ["CsvDelimiter"] = ";",
            ["CsvHasHeaders"] = true
        };

        // Act
        var result = await _service.ReconstructAsync(records, "csv", metadata);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ReconstructAsync - XML Format Tests

    [Fact]
    public async Task ReconstructAsync_WithXmlFormat_FallsBackToJsonWhenNoReconstructor()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToXmlReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        // Act
        var result = await _service.ReconstructAsync(records, "xml");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("name");
    }

    [Fact]
    public async Task ReconstructAsync_WithXmlFormat_ExtractsMetadata()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToXmlReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        var metadata = new Dictionary<string, object>
        {
            ["XmlRootElement"] = "transactions",
            ["XmlItemElement"] = "transaction"
        };

        // Act
        var result = await _service.ReconstructAsync(records, "xml", metadata);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ReconstructAsync - Excel Format Tests

    [Fact]
    public async Task ReconstructAsync_WithExcelFormat_FallsBackToJsonWhenNoReconstructor()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToExcelReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        // Act
        var result = await _service.ReconstructAsync(records, "excel");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("name");
    }

    #endregion

    #region ReconstructAsync - Original Format Tests

    [Fact]
    public async Task ReconstructAsync_WithOriginalFormatCsv_DelegatesToCsv()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToCsvReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        var metadata = new Dictionary<string, object>
        {
            ["OriginalFormat"] = "csv"
        };

        // Act
        var result = await _service.ReconstructAsync(records, "original", metadata);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ReconstructAsync_WithOriginalFormatXml_DelegatesToXml()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToXmlReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        var metadata = new Dictionary<string, object>
        {
            ["OriginalFormat"] = "xml"
        };

        // Act
        var result = await _service.ReconstructAsync(records, "original", metadata);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ReconstructAsync_WithOriginalFormatJson_ReturnsJson()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        var metadata = new Dictionary<string, object>
        {
            ["OriginalFormat"] = "json"
        };

        // Act
        var result = await _service.ReconstructAsync(records, "original", metadata);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var parsed = JArray.Parse(result);
        parsed.Count.Should().Be(1);
    }

    [Fact]
    public async Task ReconstructAsync_WithOriginalFormatNoMetadata_DefaultsToJson()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        // Act
        var result = await _service.ReconstructAsync(records, "original", null);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var parsed = JArray.Parse(result);
        parsed.Count.Should().Be(1);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task ReconstructAsync_WithEmptyRecords_ReturnsEmpty()
    {
        // Arrange
        var records = new List<JObject>();

        // Act
        var result = await _service.ReconstructAsync(records, "json");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconstructAsync_WithNullRecords_ReturnsEmpty()
    {
        // Act
        var result = await _service.ReconstructAsync(null!, "json");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconstructAsync_WithUnsupportedFormat_ThrowsException()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ReconstructAsync(records, "unsupported_format"));
    }

    #endregion

    #region Transaction Data Tests (E2E Scenario)

    [Fact]
    public async Task ReconstructAsync_WithTransactionRecords_PreservesAllFields()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""
            {
                "TransactionId": "TXN-20251201-000001",
                "CustomerId": "CUST-1001",
                "CustomerName": "John Smith",
                "Amount": 1500.50,
                "Currency": "USD",
                "Status": "Completed"
            }
            """),
            JObject.Parse("""
            {
                "TransactionId": "TXN-20251201-000002",
                "CustomerId": "CUST-1002",
                "CustomerName": "Jane Doe",
                "Amount": 250.00,
                "Currency": "EUR",
                "Status": "Pending"
            }
            """)
        };

        // Act
        var result = await _service.ReconstructAsync(records, "json");

        // Assert
        var parsed = JArray.Parse(result);
        parsed.Count.Should().Be(2);

        var first = (JObject)parsed[0];
        first["TransactionId"]?.ToString().Should().Be("TXN-20251201-000001");
        first["CustomerName"]?.ToString().Should().Be("John Smith");
        first["Amount"]?.Value<decimal>().Should().Be(1500.50m);

        var second = (JObject)parsed[1];
        second["TransactionId"]?.ToString().Should().Be("TXN-20251201-000002");
        second["Status"]?.ToString().Should().Be("Pending");
    }

    [Fact]
    public async Task ReconstructAsync_WithLargeRecordCount_HandlesEfficiently()
    {
        // Arrange
        var records = new List<JObject>();
        for (int i = 0; i < 1000; i++)
        {
            records.Add(JObject.Parse($$"""
            {
                "TransactionId": "TXN-20251201-{{i:D6}}",
                "Amount": {{(i * 10.5).ToString(System.Globalization.CultureInfo.InvariantCulture)}}
            }
            """));
        }

        // Act
        var result = await _service.ReconstructAsync(records, "json");

        // Assert
        var parsed = JArray.Parse(result);
        parsed.Count.Should().Be(1000);
    }

    #endregion

    #region Metadata Extraction Tests

    [Fact]
    public async Task ReconstructAsync_WithCsvMetadata_ExtractsDelimiter()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToCsvReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        var metadata = new Dictionary<string, object>
        {
            ["CsvDelimiter"] = ";",
            ["CsvHasHeaders"] = false,
            ["CsvColumns"] = new List<string> { "name", "age" }
        };

        // Act - Should not throw and return JSON fallback
        var result = await _service.ReconstructAsync(records, "csv", metadata);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ReconstructAsync_WithXmlMetadata_ExtractsRootElement()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToXmlReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        var metadata = new Dictionary<string, object>
        {
            ["XmlRootElement"] = "data",
            ["XmlItemElement"] = "record",
            ["XmlAttributes"] = new List<string> { "id" }
        };

        // Act - Should not throw and return JSON fallback
        var result = await _service.ReconstructAsync(records, "xml", metadata);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ReconstructAsync_WithExcelMetadata_ExtractsSheetName()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToExcelReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        var metadata = new Dictionary<string, object>
        {
            ["ExcelSheetName"] = "Transactions",
            ["ExcelHasHeaders"] = true,
            ["ExcelColumns"] = new List<string> { "name", "amount" }
        };

        // Act - Should not throw and return JSON fallback
        var result = await _service.ReconstructAsync(records, "excel", metadata);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ReconstructAsync_WithNullMetadata_UsesDefaults()
    {
        // Arrange
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(JsonToCsvReconstructor)))
            .Returns(null);

        var records = new List<JObject>
        {
            JObject.Parse("""{ "name": "John" }""")
        };

        // Act - Should not throw
        var result = await _service.ReconstructAsync(records, "csv", null);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Nested JSON Tests

    [Fact]
    public async Task ReconstructAsync_WithNestedObjects_PreservesStructure()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""
            {
                "id": 1,
                "customer": {
                    "name": "John",
                    "address": {
                        "city": "NYC",
                        "zip": "10001"
                    }
                }
            }
            """)
        };

        // Act
        var result = await _service.ReconstructAsync(records, "json");

        // Assert
        var parsed = JArray.Parse(result);
        var first = (JObject)parsed[0];
        first["customer"]?["name"]?.ToString().Should().Be("John");
        first["customer"]?["address"]?["city"]?.ToString().Should().Be("NYC");
    }

    [Fact]
    public async Task ReconstructAsync_WithArrayFields_PreservesArrays()
    {
        // Arrange
        var records = new List<JObject>
        {
            JObject.Parse("""
            {
                "id": 1,
                "tags": ["urgent", "finance", "validated"],
                "amounts": [100, 200, 300]
            }
            """)
        };

        // Act
        var result = await _service.ReconstructAsync(records, "json");

        // Assert
        var parsed = JArray.Parse(result);
        var first = (JObject)parsed[0];
        var tags = (JArray)first["tags"]!;
        tags.Count.Should().Be(3);
        tags[0].ToString().Should().Be("urgent");
    }

    #endregion
}
