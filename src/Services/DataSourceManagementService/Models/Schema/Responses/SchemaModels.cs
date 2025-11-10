namespace DataProcessing.DataSourceManagement.Models.Schema.Responses;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class DataValidationResult
{
    public bool IsValid { get; set; }
    public List<FieldValidationError> Errors { get; set; } = new();
}

public class FieldValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MessageHebrew { get; set; } = string.Empty;
    public object? ExpectedValue { get; set; }
    public object? ActualValue { get; set; }
}

public class RegexTestResult
{
    public bool IsValidPattern { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public List<RegexStringTestResult> TestResults { get; set; } = new();
    public RegexTestSummary Summary { get; set; } = new();
}

public class RegexStringTestResult
{
    public string TestString { get; set; } = string.Empty;
    public bool IsMatch { get; set; }
}

public class RegexTestSummary
{
    public int TotalTests { get; set; }
    public int MatchCount { get; set; }
    public int NoMatchCount { get; set; }
    public double SuccessRate { get; set; }
}

public class SchemaTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameHebrew { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionHebrew { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int FieldsCount { get; set; }
    public string Complexity { get; set; } = string.Empty;
    public object? JsonSchema { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class SchemaUsageStatistics
{
    public string SchemaId { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int CurrentUsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime CollectedAt { get; set; }
    public long TotalRecordsProcessed { get; set; }
    public long TotalValidationAttempts { get; set; }
    public long SuccessfulValidations { get; set; }
    public long FailedValidations { get; set; }
    public double SuccessRate { get; set; }
    public double AverageValidationTimeMs { get; set; }
}
