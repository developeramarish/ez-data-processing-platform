namespace SchemaManagementService.Models.Responses;

/// <summary>
/// Result of regex pattern testing
/// </summary>
public class RegexTestResult
{
    /// <summary>
    /// Whether the regex pattern is valid
    /// </summary>
    public bool IsValidPattern { get; set; } = false;

    /// <summary>
    /// Regex pattern that was tested
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Test results for each string
    /// </summary>
    public List<RegexStringTestResult> TestResults { get; set; } = new();

    /// <summary>
    /// Pattern compilation error message (if any)
    /// </summary>
    public string? PatternError { get; set; }

    /// <summary>
    /// Pattern compilation error in English (if any)
    /// </summary>
    public string? PatternErrorEnglish { get; set; }

    /// <summary>
    /// Summary of test results
    /// </summary>
    public RegexTestSummary Summary { get; set; } = new();
}

/// <summary>
/// Test result for a single string against the regex pattern
/// </summary>
public class RegexStringTestResult
{
    /// <summary>
    /// Test string
    /// </summary>
    public string TestString { get; set; } = string.Empty;

    /// <summary>
    /// Whether the string matches the pattern
    /// </summary>
    public bool IsMatch { get; set; } = false;

    /// <summary>
    /// Matched groups (if any)
    /// </summary>
    public List<string> MatchedGroups { get; set; } = new();

    /// <summary>
    /// Full match text
    /// </summary>
    public string? MatchedText { get; set; }

    /// <summary>
    /// Match position in the test string
    /// </summary>
    public int? MatchPosition { get; set; }

    /// <summary>
    /// Match length
    /// </summary>
    public int? MatchLength { get; set; }
}

/// <summary>
/// Summary of regex test results
/// </summary>
public class RegexTestSummary
{
    /// <summary>
    /// Total number of test strings
    /// </summary>
    public int TotalTests { get; set; } = 0;

    /// <summary>
    /// Number of strings that matched
    /// </summary>
    public int MatchCount { get; set; } = 0;

    /// <summary>
    /// Number of strings that did not match
    /// </summary>
    public int NoMatchCount { get; set; } = 0;

    /// <summary>
    /// Match success percentage
    /// </summary>
    public double SuccessRate { get; set; } = 0.0;
}
