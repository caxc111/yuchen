namespace MaterialImportTool.Models
{
    public class Category
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
    }
}
