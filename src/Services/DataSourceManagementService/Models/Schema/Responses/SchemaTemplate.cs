namespace SchemaManagementService.Models.Responses;

/// <summary>
/// Schema template for creating new schemas from predefined patterns
/// </summary>
public class SchemaTemplate
{
    /// <summary>
    /// Template unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Template name in English
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template name in Hebrew
    /// </summary>
    public string NameHebrew { get; set; } = string.Empty;

    /// <summary>
    /// Template description in English
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Template description in Hebrew
    /// </summary>
    public string DescriptionHebrew { get; set; } = string.Empty;

    /// <summary>
    /// Template category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Number of fields in this template
    /// </summary>
    public int FieldsCount { get; set; }

    /// <summary>
    /// Template complexity level
    /// </summary>
    public string Complexity { get; set; } = string.Empty;

    /// <summary>
    /// JSON Schema content for this template
    /// </summary>
    public object JsonSchema { get; set; } = new object();

    /// <summary>
    /// Sample data that matches this template
    /// </summary>
    public List<object> SampleData { get; set; } = new();

    /// <summary>
    /// Documentation markdown for this template
    /// </summary>
    public string Documentation { get; set; } = string.Empty;

    /// <summary>
    /// Template tags for categorization and search
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Template creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Template version
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Whether this template is built-in or custom
    /// </summary>
    public bool IsBuiltIn { get; set; } = true;

    /// <summary>
    /// Template usage count
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Template rating (1-5 stars)
    /// </summary>
    public double Rating { get; set; } = 0.0;

    /// <summary>
    /// Template author/creator
    /// </summary>
    public string Author { get; set; } = "System";
}
