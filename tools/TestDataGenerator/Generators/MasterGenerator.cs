// MasterGenerator.cs - Orchestrates all test data generation
// Date: December 2, 2025

using TestDataGenerator.Templates;

namespace TestDataGenerator.Generators;

public class MasterGenerator
{
    private readonly string _outputBasePath;

    public MasterGenerator()
    {
        _outputBasePath = Path.Combine(Directory.GetCurrentDirectory(), "TestFiles");
        Directory.CreateDirectory(_outputBasePath);
    }

    public List<GenerationResult> GenerateAllScenarios()
    {
        var results = new List<GenerationResult>();

        // E2E-001: Complete Pipeline (100 records, CSV)
        results.Add(GenerateScenario("E2E-001"));

        // E2E-002: Multi-Destination (200 records, CSV)
        results.Add(GenerateScenario("E2E-002"));

        // E2E-003: Multiple Formats (150 records each: CSV, XML, Excel, JSON)
        results.Add(GenerateScenario("E2E-003"));

        // E2E-004: Schema Validation (100 valid + 100 with 30 invalid)
        results.Add(GenerateScenario("E2E-004"));

        // E2E-005: Connection Failures (50 records, CSV)
        results.Add(GenerateScenario("E2E-005"));

        // E2E-006: High Load (10,000 records, CSV)
        results.Add(GenerateScenario("E2E-006"));

        return results;
    }

    public GenerationResult GenerateScenario(string scenarioId)
    {
        var scenarioPath = Path.Combine(_outputBasePath, scenarioId);
        Directory.CreateDirectory(scenarioPath);

        Console.WriteLine($"Generating {scenarioId}...");

        try
        {
            return scenarioId switch
            {
                "E2E-001" => GenerateE2E001(scenarioPath),
                "E2E-002" => GenerateE2E002(scenarioPath),
                "E2E-003" => GenerateE2E003(scenarioPath),
                "E2E-004" => GenerateE2E004(scenarioPath),
                "E2E-005" => GenerateE2E005(scenarioPath),
                "E2E-006" => GenerateE2E006(scenarioPath),
                _ => new GenerationResult
                {
                    ScenarioId = scenarioId,
                    Success = false,
                    ErrorMessage = $"Unknown scenario: {scenarioId}"
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error generating {scenarioId}: {ex.Message}");
            return new GenerationResult
            {
                ScenarioId = scenarioId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private GenerationResult GenerateE2E001(string scenarioPath)
    {
        // E2E-001: Complete File Processing Pipeline
        // 100 valid customer transactions in CSV format

        var generator = new CsvFileGenerator();
        var template = CustomerTransactionTemplate.CreateValid();

        var config = new TestFileConfig
        {
            ScenarioId = "E2E-001",
            FileName = "customer-transactions-100.csv",
            FilePath = Path.Combine(scenarioPath, "customer-transactions-100.csv"),
            RecordCount = 100,
            ValidRecords = 100,
            InvalidRecords = 0,
            Template = template
        };

        generator.Generate(config);

        Console.WriteLine($"  ✅ Generated: customer-transactions-100.csv (100 records)");

        return new GenerationResult
        {
            ScenarioId = "E2E-001",
            Success = true,
            FilesGenerated = 1,
            RecordsGenerated = 100
        };
    }

    private GenerationResult GenerateE2E002(string scenarioPath)
    {
        // E2E-002: Multi-Destination Output
        // 200 valid banking transactions in CSV format

        var generator = new CsvFileGenerator();
        var template = BankingTransactionTemplate.CreateValid();

        var config = new TestFileConfig
        {
            ScenarioId = "E2E-002",
            FileName = "banking-transactions-200.csv",
            FilePath = Path.Combine(scenarioPath, "banking-transactions-200.csv"),
            RecordCount = 200,
            ValidRecords = 200,
            InvalidRecords = 0,
            Template = template
        };

        generator.Generate(config);

        Console.WriteLine($"  ✅ Generated: banking-transactions-200.csv (200 records)");

        return new GenerationResult
        {
            ScenarioId = "E2E-002",
            Success = true,
            FilesGenerated = 1,
            RecordsGenerated = 200
        };
    }

    private GenerationResult GenerateE2E003(string scenarioPath)
    {
        // E2E-003: Multiple File Formats
        // 150 records in each format: CSV, XML, Excel, JSON

        int totalRecords = 0;
        int filesGenerated = 0;

        // CSV
        var csvGenerator = new CsvFileGenerator();
        var csvTemplate = CustomerTransactionTemplate.CreateValid();
        csvGenerator.Generate(new TestFileConfig
        {
            ScenarioId = "E2E-003",
            FileName = "transactions-150.csv",
            FilePath = Path.Combine(scenarioPath, "transactions-150.csv"),
            RecordCount = 150,
            ValidRecords = 150,
            InvalidRecords = 0,
            Template = csvTemplate
        });
        Console.WriteLine($"  ✅ Generated: transactions-150.csv (150 records)");
        totalRecords += 150;
        filesGenerated++;

        // XML
        var xmlGenerator = new XmlFileGenerator();
        xmlGenerator.Generate(new TestFileConfig
        {
            ScenarioId = "E2E-003",
            FileName = "transactions-150.xml",
            FilePath = Path.Combine(scenarioPath, "transactions-150.xml"),
            RecordCount = 150,
            ValidRecords = 150,
            InvalidRecords = 0,
            Template = CustomerTransactionTemplate.CreateValid()
        });
        Console.WriteLine($"  ✅ Generated: transactions-150.xml (150 records)");
        totalRecords += 150;
        filesGenerated++;

        // Excel - Temporarily disabled (EPPlus 8.x license configuration issue)
        // Will be enabled after license configuration resolved
        // var excelGenerator = new ExcelFileGenerator();
        // excelGenerator.Generate(...);
        Console.WriteLine($"  ⚠️  Skipped: transactions-150.xlsx (EPPlus license config - will fix in Week 2)");

        // JSON
        var jsonGenerator = new JsonFileGenerator();
        jsonGenerator.Generate(new TestFileConfig
        {
            ScenarioId = "E2E-003",
            FileName = "transactions-150.json",
            FilePath = Path.Combine(scenarioPath, "transactions-150.json"),
            RecordCount = 150,
            ValidRecords = 150,
            InvalidRecords = 0,
            Template = CustomerTransactionTemplate.CreateValid()
        });
        Console.WriteLine($"  ✅ Generated: transactions-150.json (150 records)");
        totalRecords += 150;
        filesGenerated++;

        return new GenerationResult
        {
            ScenarioId = "E2E-003",
            Success = true,
            FilesGenerated = filesGenerated,
            RecordsGenerated = totalRecords
        };
    }

    private GenerationResult GenerateE2E004(string scenarioPath)
    {
        // E2E-004: Schema Validation
        // File 1: 100 valid records
        // File 2: 70 valid + 30 invalid records

        var csvGenerator = new CsvFileGenerator();
        int totalRecords = 0;

        // All valid
        csvGenerator.Generate(new TestFileConfig
        {
            ScenarioId = "E2E-004",
            FileName = "transactions-all-valid.csv",
            FilePath = Path.Combine(scenarioPath, "transactions-all-valid.csv"),
            RecordCount = 100,
            ValidRecords = 100,
            InvalidRecords = 0,
            Template = CustomerTransactionTemplate.CreateValid()
        });
        Console.WriteLine($"  ✅ Generated: transactions-all-valid.csv (100 valid)");
        totalRecords += 100;

        // With errors
        csvGenerator.Generate(new TestFileConfig
        {
            ScenarioId = "E2E-004",
            FileName = "transactions-with-errors.csv",
            FilePath = Path.Combine(scenarioPath, "transactions-with-errors.csv"),
            RecordCount = 100,
            ValidRecords = 70,
            InvalidRecords = 30,
            Template = CustomerTransactionTemplate.CreateWithErrors()
        });
        Console.WriteLine($"  ✅ Generated: transactions-with-errors.csv (70 valid, 30 invalid)");
        totalRecords += 100;

        return new GenerationResult
        {
            ScenarioId = "E2E-004",
            Success = true,
            FilesGenerated = 2,
            RecordsGenerated = totalRecords
        };
    }

    private GenerationResult GenerateE2E005(string scenarioPath)
    {
        // E2E-005: Connection Failures
        // 50 records for testing failure scenarios

        var csvGenerator = new CsvFileGenerator();
        csvGenerator.Generate(new TestFileConfig
        {
            ScenarioId = "E2E-005",
            FileName = "test-file-50.csv",
            FilePath = Path.Combine(scenarioPath, "test-file-50.csv"),
            RecordCount = 50,
            ValidRecords = 50,
            InvalidRecords = 0,
            Template = CustomerTransactionTemplate.CreateValid()
        });

        Console.WriteLine($"  ✅ Generated: test-file-50.csv (50 records)");

        return new GenerationResult
        {
            ScenarioId = "E2E-005",
            Success = true,
            FilesGenerated = 1,
            RecordsGenerated = 50
        };
    }

    private GenerationResult GenerateE2E006(string scenarioPath)
    {
        // E2E-006: High Load Testing
        // 10,000 records in CSV format

        Console.WriteLine($"  ⏳ Generating large file (10,000 records)...");

        var csvGenerator = new CsvFileGenerator();
        csvGenerator.Generate(new TestFileConfig
        {
            ScenarioId = "E2E-006",
            FileName = "large-file-10000.csv",
            FilePath = Path.Combine(scenarioPath, "large-file-10000.csv"),
            RecordCount = 10000,
            ValidRecords = 10000,
            InvalidRecords = 0,
            Template = CustomerTransactionTemplate.CreateValid()
        });

        Console.WriteLine($"  ✅ Generated: large-file-10000.csv (10,000 records)");

        return new GenerationResult
        {
            ScenarioId = "E2E-006",
            Success = true,
            FilesGenerated = 1,
            RecordsGenerated = 10000
        };
    }
}

public class GenerationResult
{
    public string ScenarioId { get; set; } = "";
    public bool Success { get; set; }
    public int FilesGenerated { get; set; }
    public int RecordsGenerated { get; set; }
    public string ErrorMessage { get; set; } = "";
}

public class TestFileConfig
{
    public string ScenarioId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int RecordCount { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public IDataTemplate Template { get; set; } = null!;
}
