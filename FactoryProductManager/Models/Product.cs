using System;

namespace FactoryProductManager.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string BusinessType { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string HouseType { get; set; } = string.Empty;
        public decimal Area { get; set; }
        public decimal CostTotalPrice { get; set; }
        public decimal? SellingTotalPrice { get; set; }
        public string FloorPlan { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}