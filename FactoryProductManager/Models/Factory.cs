using System;

namespace FactoryProductManager.Models
{
    public class Factory
    {
        public int Id { get; set; }
        public string FactoryCode { get; set; } = string.Empty;
        public string FactoryName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string FactoryType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Certifications { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Scale { get; set; } = string.Empty;
        public int? EmployeeCount { get; set; }
        public string ProductionCapacity { get; set; } = string.Empty;
        public string ControllingPerson { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}