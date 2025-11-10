using System.ComponentModel.DataAnnotations;

namespace DataProcessing.DataSourceManagement.Models.Schema.Requests;

public class ValidateJsonSchemaRequest
{
    [Required]
    public string JsonSchemaContent { get; set; } = string.Empty;
}

public class TestRegexRequest
{
    [Required]
    public string Pattern { get; set; } = string.Empty;
    
    public List<string> TestStrings { get; set; } = new();
}
