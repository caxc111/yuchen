using System;

namespace FactoryProductManager.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Specification { get; set; }
        public string Unit { get; set; }
        public decimal TotalCost { get; set; }
        public decimal? SellingPrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}