using System.ComponentModel.DataAnnotations;
using DataProcessing.Shared.Entities;

namespace DataProcessing.DataSourceManagement.Models.Schema.Requests;

public class CreateSchemaRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    public string? DataSourceId { get; set; }
    
    [Required]
    public string JsonSchemaContent { get; set; } = string.Empty;
    
    public List<string> Tags { get; set; } = new();
    public SchemaStatus Status { get; set; } = SchemaStatus.Draft;
    public int Version { get; set; } = 1;
    public string CreatedBy { get; set; } = "System";
}
