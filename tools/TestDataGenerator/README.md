# TestDataGenerator - EZ Platform Test Data Generation Tool

**Version:** 1.0
**Date:** December 2, 2025
**Purpose:** Generate systematic, reproducible test data files for E2E testing

---

## Overview

TestDataGenerator is a .NET command-line tool that generates test data files for all EZ Platform E2E test scenarios. It supports multiple file formats (CSV, XML, Excel, JSON) and can generate both valid and invalid data for comprehensive testing.

---

## Features

- ✅ Multiple file formats (CSV, XML, Excel, JSON)
- ✅ Configurable record counts (10 to 1,000,000)
- ✅ Valid and invalid data scenarios
- ✅ Reproducible generation (fixed seed)
- ✅ Schema-based data generation
- ✅ Realistic fake data (using Bogus library)
- ✅ Command-line interface
- ✅ Batch generation for all scenarios

---

## Usage

### Generate All Test Files (Recommended)
```bash
cd tools/TestDataGenerator
dotnet run -- generate-all
```

**Output:**
```
====================================
EZ Platform - Test Data Generator
Version 1.0
====================================

Generating test files for all E2E scenarios...

Generating E2E-001...
  ✅ Generated: customer-transactions-100.csv (100 records)

Generating E2E-002...
  ✅ Generated: banking-transactions-200.csv (200 records)

Generating E2E-003...
  ✅ Generated: transactions-150.csv (150 records)
  ✅ Generated: transactions-150.xml (150 records)
  ✅ Generated: transactions-150.xlsx (150 records)
  ✅ Generated: transactions-150.json (150 records)

Generating E2E-004...
  ✅ Generated: transactions-all-valid.csv (100 valid)
  ✅ Generated: transactions-with-errors.csv (70 valid, 30 invalid)

Generating E2E-005...
  ✅ Generated: test-file-50.csv (50 records)

Generating E2E-006...
  ⏳ Generating large file (10,000 records)...
  ✅ Generated: large-file-10000.csv (10,000 records)

====================================
Generation Complete!
====================================
Total scenarios: 6
Total files generated: 9
Total records: 11,250
```

---

### Generate Specific Scenario
```bash
dotnet run -- generate-scenario E2E-001
```

**Output:**
```
Generating test files for scenario: E2E-001

Generating E2E-001...
  ✅ Generated: customer-transactions-100.csv (100 records)

✅ Success!
Files generated: 1
Records generated: 100
Location: TestFiles/E2E-001/
```

---

### Clean All Generated Files
```bash
dotnet run -- clean
```

**Output:**
```
Cleaning all generated test files...
✅ All test files deleted.
```

---

## Generated Test Files

### E2E-001: Complete Pipeline
- **File:** `customer-transactions-100.csv`
- **Format:** CSV
- **Records:** 100 valid
- **Schema:** customer-transaction-v1
- **Purpose:** Test complete end-to-end pipeline

---

### E2E-002: Multi-Destination Output
- **File:** `banking-transactions-200.csv`
- **Format:** CSV
- **Records:** 200 valid
- **Schema:** banking-transaction-v1
- **Purpose:** Test multi-destination output (4 destinations)

---

### E2E-003: Multiple File Formats
- **Files:**
  - `transactions-150.csv` (CSV format)
  - `transactions-150.xml` (XML format)
  - `transactions-150.xlsx` (Excel format)
  - `transactions-150.json` (JSON format)
- **Records:** 150 each
- **Schema:** customer-transaction-v1
- **Purpose:** Test format conversion (CSV/XML/Excel → JSON)

---

### E2E-004: Schema Validation
- **Files:**
  - `transactions-all-valid.csv` (100 valid records)
  - `transactions-with-errors.csv` (70 valid, 30 invalid)
- **Format:** CSV
- **Schema:** customer-transaction-v1
- **Purpose:** Test validation logic and invalid record handling

**Error Types Included:**
- Missing required fields
- Invalid data types
- Out of range values
- Invalid formats
- Null values
- Empty strings

---

### E2E-005: Connection Failures
- **File:** `test-file-50.csv`
- **Format:** CSV
- **Records:** 50 valid
- **Schema:** customer-transaction-v1
- **Purpose:** Test error handling when connections fail

---

### E2E-006: High Load Testing
- **File:** `large-file-10000.csv`
- **Format:** CSV
- **Records:** 10,000 valid
- **Schema:** customer-transaction-v1
- **Purpose:** Test system performance under load

---

## Project Structure

```
TestDataGenerator/
├── TestDataGenerator.csproj        # .NET 10 project file
├── Program.cs                      # CLI entry point
├── README.md                       # This file
├── Generators/
│   ├── MasterGenerator.cs          # Orchestrates all generation
│   ├── CsvFileGenerator.cs         # CSV file generation
│   ├── XmlFileGenerator.cs         # XML file generation
│   ├── ExcelFileGenerator.cs       # Excel file generation
│   └── JsonFileGenerator.cs        # JSON file generation
├── Templates/
│   ├── IDataTemplate.cs            # Template interface
│   ├── CustomerTransactionTemplate.cs  # Customer transaction data
│   └── BankingTransactionTemplate.cs   # Banking transaction data
└── TestFiles/                      # Generated output (not in git)
    ├── E2E-001/
    ├── E2E-002/
    ├── E2E-003/
    ├── E2E-004/
    ├── E2E-005/
    └── E2E-006/
```

---

## Dependencies

- **CsvHelper 33.1.0** - CSV file generation
- **EPPlus 8.2.1** - Excel file generation
- **System.Xml.Serialization 4.3.0** - XML generation
- **Newtonsoft.Json 13.0.3** - JSON serialization
- **Bogus 35.6.1** - Realistic fake data generation
- **System.CommandLine 2.0.0-beta4** - CLI interface

---

## Data Templates

### CustomerTransactionTemplate
**Schema:** customer-transaction-v1

**Fields:**
- TransactionId (TXN-YYYYMMDD-XXXXXX)
- CustomerId (CUST-XXXX)
- CustomerName (Full name)
- TransactionDate (yyyy-MM-dd HH:mm:ss)
- Amount (Decimal, 10-10,000)
- Currency (USD, EUR, GBP, ILS)
- TransactionType (Purchase, Refund, Transfer, etc.)
- Status (Completed, Pending, Processing)
- Description (Text)

---

### BankingTransactionTemplate
**Schema:** banking-transaction-v1

**Fields:**
- AccountNumber (8 digits)
- TransactionId (BNK-YYYYMMDD-XXXXXXXX)
- TransactionDate (yyyy-MM-dd HH:mm:ss)
- TransactionType (Deposit, Withdrawal, Transfer, etc.)
- Amount (Decimal, 100-50,000)
- Balance (Calculated based on transaction type)
- Currency (ILS)
- BranchCode (3 digits)
- TellerID (TXXXX)
- ReferenceNumber (REFXXXXXXXXX)
- Notes (Text)

---

## Reproducibility

All data generation uses **fixed seeds** to ensure reproducibility:
- Same run = Same data
- Enables consistent testing
- Baseline comparisons possible

**Seed:** 12345 (hardcoded)

---

## Extending the Generator

### Add New Template

1. Create new template class implementing `IDataTemplate`:
```csharp
public class MyTemplate : IDataTemplate
{
    public string[] GetHeaders() { ... }
    public Dictionary<string, object> GenerateValidRecord(int index) { ... }
    public Dictionary<string, object> GenerateInvalidRecord(int index, ErrorType errorType) { ... }
    public string GetSchemaName() { ... }
}
```

2. Use in MasterGenerator:
```csharp
var template = MyTemplate.CreateValid();
csvGenerator.Generate(new TestFileConfig { Template = template, ... });
```

---

### Add New Scenario

Update `MasterGenerator.cs`:
```csharp
public GenerationResult GenerateScenario(string scenarioId)
{
    return scenarioId switch
    {
        "E2E-001" => GenerateE2E001(scenarioPath),
        // ... existing scenarios
        "E2E-007" => GenerateE2E007(scenarioPath), // New scenario
        _ => new GenerationResult { Success = false }
    };
}

private GenerationResult GenerateE2E007(string scenarioPath)
{
    // Your generation logic
}
```

---

## Testing

### Build and Test
```bash
cd tools/TestDataGenerator

# Build project
dotnet build

# Generate all test files
dotnet run -- generate-all

# Verify files created
ls TestFiles/

# Check file contents
head TestFiles/E2E-001/customer-transactions-100.csv
```

---

## Notes

- **Generated files are NOT committed to git** (.gitignore)
- **Templates use Bogus** for realistic fake data
- **EPPlus requires NonCommercial license** (set in ExcelFileGenerator)
- **File sizes:** E2E-006 generates ~1.5MB file (10k records)
- **Generation time:** ~1-2 seconds for all scenarios

---

**Tool Status:** ✅ Complete & Functional
**Ready For:** Week 3 E2E Testing
**Last Updated:** December 2, 2025
