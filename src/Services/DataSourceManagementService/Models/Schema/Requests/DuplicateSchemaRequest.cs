namespace DataProcessing.DataSourceManagement.Models.Schema.Requests;

public class DuplicateSchemaRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "System";
    public bool CopyTags { get; set; } = true;
    public List<string> AdditionalTags { get; set; } = new();
}
