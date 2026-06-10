using System;
using System.Linq;

namespace FactoryProductManager.Models
{
    public class FactoryMaterial
    {
        public int Id { get; set; }
        public string FactoryMaterialCode { get; set; } = string.Empty;
        public string MyMaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public string Texture { get; set; } = string.Empty;
        public string Process { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal? CostPrice { get; set; }
        public string UsageScenario { get; set; } = string.Empty;
        public string Certifications { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int? FactoryId { get; set; }
        public string FactoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string CategoryDisplay => string.IsNullOrWhiteSpace(Category)
            ? string.Empty
            : Category.Split(new[] { " > " }, StringSplitOptions.None).LastOrDefault() ?? Category;
    }
}
