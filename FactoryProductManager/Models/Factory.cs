using System;
using System.Collections.Generic;
using System.Linq;

namespace FactoryProductManager.Models
{
    public class Factory
    {
        public int Id { get; set; }
        public string FactoryCode { get; set; } = string.Empty;
        public string FactoryName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        
        /// <summary>
        /// 工厂类型（存储格式：逗号分隔，如 "厨卫陶瓷,厨卫五金"）
        /// </summary>
        public string FactoryType { get; set; } = string.Empty;
        
        /// <summary>
        /// 工厂类型列表（内存中使用）
        /// </summary>
        public List<string> FactoryTypes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FactoryType))
                    return new List<string>();
                return FactoryType.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            set
            {
                FactoryType = string.Join(",", value ?? new List<string>());
            }
        }
        
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
