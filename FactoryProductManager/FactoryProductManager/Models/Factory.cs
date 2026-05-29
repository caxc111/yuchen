using System;

namespace FactoryProductManager.Models
{
    public class Factory
    {
        public int Id { get; set; }
        public string FactoryCode { get; set; }
        public string FactoryName { get; set; }
        public string FactoryType { get; set; }
        public string Address { get; set; }
        public string Certifications { get; set; }
        public string Description { get; set; }
        public string Scale { get; set; }
        public int? EmployeeCount { get; set; }
        public string ProductionCapacity { get; set; }
        public string ControllingPerson { get; set; }
        public string ContactPerson { get; set; }
        public string ContactInfo { get; set; }
        public string ContactMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}