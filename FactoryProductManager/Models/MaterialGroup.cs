using System;
using System.Collections.Generic;
using System.Linq;

namespace FactoryProductManager.Models
{
    /// <summary>
    /// 物料组合模板（如"定制橱柜"、"洗衣柜"、"中岛台"）。
    /// 一个组合由若干子项（MaterialGroupItem）组成，例如橱柜 = 柜体 + 门板 + 台面 + 五金 + 拉手。
    /// </summary>
    public class MaterialGroup
    {
        public int Id { get; set; }
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 内存中临时挂的子项模板（不落库到 MaterialGroups 表，存到 MaterialGroupItems 表）
        public List<MaterialGroupItem> Items { get; set; } = new();
    }
}
