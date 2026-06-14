using System;
using System.Collections.Generic;
using System.Linq;

namespace FactoryProductManager.Models
{
    public class CustomPart
    {
        public int Id { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string Components { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<string> ComponentList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Components)) return new List<string>();
                return Components.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim())
                                 .Where(s => !string.IsNullOrEmpty(s))
                                 .ToList();
            }
            set
            {
                Components = value == null ? string.Empty : string.Join(",", value);
            }
        }
    }
}
