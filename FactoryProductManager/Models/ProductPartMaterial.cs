using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FactoryProductManager.Models
{
    public class ProductPartMaterial : INotifyPropertyChanged
    {
        private decimal _quantity = 1;
        // 复合物料主行的总价需要可写（=子项之和），普通行用计算值
        private decimal? _totalPriceOverride;

        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public string MaterialTypeName { get; set; } = string.Empty;
        public int? MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string FactoryMaterialCode { get; set; } = string.Empty;
        public string MyMaterialCode { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string FactoryName { get; set; } = string.Empty;

        // 图片路径（使用 Remarks 字段存储）
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ===== 复合物料相关字段（方案 B） =====
        // 1=主行（代表"定制橱柜"这种整体）；0=普通行或子行
        public bool IsComposite { get; set; }
        // 组合编码（如 CB-001），与 MaterialGroups.group_code 关联
        public string GroupCode { get; set; } = string.Empty;
        // 子项名（如"柜体"/"门板"/"台面"），普通行为空
        public string ItemName { get; set; } = string.Empty;
        // 子行指向主行 id；主行此字段为 null
        public int? ParentId { get; set; }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        // 复合物料主行用 _totalPriceOverride；普通行用 UnitPrice * Quantity
        public decimal TotalPrice
        {
            get => _totalPriceOverride ?? (UnitPrice * Quantity);
            set
            {
                _totalPriceOverride = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
