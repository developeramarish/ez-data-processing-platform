using MongoDB.Entities;

namespace DataProcessing.Shared.Entities;

public class DataProcessingSchema : DataProcessingBaseEntity
{
    public string Name { get; set; } = string.Empty;                    // e.g., "sales_transaction"
    public string DisplayName { get; set; } = string.Empty;             // Hebrew: "עסקאות מכירה"
    public string Description { get; set; } = string.Empty;
    public string? DataSourceId { get; set; }                           // 1:1 relationship (optional)
    public int SchemaVersionNumber { get; set; } = 1;                   // Schema version number (1, 2, 3, etc.)
    public SchemaStatus Status { get; set; } = SchemaStatus.Draft;      // Draft, Active, Inactive, Archived
    public string JsonSchemaContent { get; set; } = string.Empty;       // JSON Schema 2020-12
    public List<SchemaField> Fields { get; set; } = new();              // Parsed fields
    public List<string> Tags { get; set; } = new();                     // For categorization
    public DateTime? PublishedAt { get; set; }
    public DateTime? DeprecatedAt { get; set; }
    public string? DeprecationReason { get; set; }
    public int UsageCount { get; set; } = 0;                            // Number of active data sources using this schema
    public SchemaMetadata Metadata { get; set; } = new();               // Additional metadata
}

public class SchemaField
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;              // Hebrew display name
    public string Type { get; set; } = string.Empty;                     // string, number, boolean, array, object
    public bool Required { get; set; } = false;
    public string Description { get; set; } = string.Empty;
    public FieldValidation Validation { get; set; } = new();
    public object? DefaultValue { get; set; }
    public List<string> Examples { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class FieldValidation
{
    // String validations
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }                  // Regex pattern
    public string? Format { get; set; }                   // date-time, email, uuid, etc.
    
    // Number validations
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public decimal? MultipleOf { get; set; }
    public bool ExclusiveMinimum { get; set; } = false;
    public bool ExclusiveMaximum { get; set; } = false;
    
    // Array validations
    public int? MinItems { get; set; }
    public int? MaxItems { get; set; }
    public bool UniqueItems { get; set; } = false;
    
    // Enum validations
    public List<object> Enum { get; set; } = new();
    
    // Conditional validations (JSON Schema 2020-12)
    public Dictionary<string, object>? If { get; set; }
    public Dictionary<string, object>? Then { get; set; }
    public Dictionary<string, object>? Else { get; set; }
}

public class SchemaMetadata
{
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> RelatedSchemas { get; set; } = new();
    public string? DocumentationUrl { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

public enum SchemaStatus
{
    Draft,
    Active,
    Inactive,
    Archived
}
