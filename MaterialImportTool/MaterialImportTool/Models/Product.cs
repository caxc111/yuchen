using System;

namespace MaterialImportTool.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string FactoryProductCode { get; set; } = string.Empty;
        public string? MyProductCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Specification { get; set; }
        public string? Texture { get; set; }
        public string? Process { get; set; }
        public string? UsageScenario { get; set; }
        public string? Certifications { get; set; }
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public string? ImageUrl { get; set; }
        public int? FactoryId { get; set; }
        public string? FactoryCode { get; set; }
        public string? FactoryName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
