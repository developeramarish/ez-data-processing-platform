using System.ComponentModel.DataAnnotations;

namespace SchemaManagementService.Models.Requests;

/// <summary>
/// Request model for testing regex pattern against strings
/// </summary>
public class TestRegexRequest
{
    /// <summary>
    /// Regex pattern to test
    /// </summary>
    [Required(ErrorMessage = "תבנית Regex היא חובה")]
    [StringLength(1000, ErrorMessage = "תבנית Regex ארוכה מדי - מקסימום 1000 תווים")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// List of strings to test against the pattern
    /// </summary>
    [Required(ErrorMessage = "רשימת מחרוזות לבדיקה היא חובה")]
    [MaxLength(50, ErrorMessage = "מקסימום 50 מחרוזות לבדיקה")]
    public List<string> TestStrings { get; set; } = new();

    /// <summary>
    /// Regex options (case sensitivity, multiline, etc.)
    /// </summary>
    public RegexTestOptions Options { get; set; } = new();

    /// <summary>
    /// Pattern description for documentation
    /// </summary>
    [StringLength(200, ErrorMessage = "תיאור התבנית חייב להיות עד 200 תווים")]
    public string? Description { get; set; }
}

/// <summary>
/// Options for regex testing
/// </summary>
public class RegexTestOptions
{
    /// <summary>
    /// Ignore case during matching
    /// </summary>
    public bool IgnoreCase { get; set; } = false;

    /// <summary>
    /// Multiline mode (^ and $ match line boundaries)
    /// </summary>
    public bool Multiline { get; set; } = false;

    /// <summary>
    /// Single line mode (. matches newline characters)
    /// </summary>
    public bool Singleline { get; set; } = false;

    /// <summary>
    /// Timeout in milliseconds for regex execution
    /// </summary>
    [Range(100, 10000, ErrorMessage = "זמן קצוב חייב להיות בין 100-10000 מילישניות")]
    public int TimeoutMs { get; set; } = 1000;
}
