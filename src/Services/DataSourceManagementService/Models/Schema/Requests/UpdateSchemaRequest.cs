using System.ComponentModel.DataAnnotations;
using DataProcessing.Shared.Entities;

namespace DataProcessing.DataSourceManagement.Models.Schema.Requests;

public class UpdateSchemaRequest
{
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    public string? DataSourceId { get; set; }
    
    [Required]
    public string JsonSchemaContent { get; set; } = string.Empty;
    
    public List<string> Tags { get; set; } = new();
    public SchemaStatus Status { get; set; }
    public string UpdatedBy { get; set; } = "System";
}
