using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using FactoryProductManager.Services;

namespace FactoryProductManager.Models
{
    public class SelectedMaterial : INotifyPropertyChanged
    {
        private decimal _quantity = 1;

        public int Id { get; set; }
        public int FactoryMaterialId { get; set; }
        public string PartName { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public string Specification { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public string ComponentName { get; set; } = "";
        public string MaterialTypeName { get; set; } = "";
        public string FactoryMaterialCode { get; set; } = "";
        public string MyMaterialCode { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Unit { get; set; } = "";
        public string ImageUrl { get; set; } = "";

        // 导出时需要的额外字段（从 FactoryProducts 关联获取）
        public string Remarks { get; set; } = "";
        public string FactoryName { get; set; } = "";
        public string SupplyCycle { get; set; } = "";

        // 复合物料
        public bool IsComposite { get; set; }
        public string GroupCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int? ParentRef { get; set; }

        // 柜子名称（如"电视柜"、"玄关柜"），用于组合显示名
        public string CabinetName { get; set; } = "";

        // 图纸编号（用于关联 CAD 图纸）
        private string _drawingNumber = "";
        public string DrawingNumber
        {
            get => _drawingNumber;
            set
            {
                if (_drawingNumber != value)
                {
                    _drawingNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<SelectedMaterial> Children { get; } = new();
        // 归属的组合物料名称（如五金属于"定制橱柜"）
        private string _subGroupName = "";
        public string SubGroupName
        {
            get => _subGroupName;
            set
            {
                if (_subGroupName != value)
                {
                    _subGroupName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FullDisplayName));
                }
            }
        }

        /// <summary>
        /// 根据单位返回数量的小数位数：米/平方米/立方米 → 2位，其他 → 0位
        /// </summary>
        public int QuantityDecimalPlaces
        {
            get
            {
                var u = Unit?.Trim().ToLowerInvariant() ?? "";
                if (u == "m" || u == "米" || u == "㎡" || u == "m²" || u == "m2" ||
                    u == "m³" || u == "m3" || u.Contains("²") || u.Contains("³") || u.Contains("3"))
                    return 2;
                return 0;
            }
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(QuantityDisplay));
                    OnPropertyChanged(nameof(TotalPrice));
                    _parentForNotify?.OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        /// <summary>
        /// 用于 TextBox 显示的数量文本（带格式化）
        /// </summary>
        public string QuantityDisplay
        {
            get => QuantityDecimalPlaces == 0
                ? ((int)Quantity).ToString()
                : Quantity.ToString($"F{QuantityDecimalPlaces}");
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                string trimmed = value.Trim();
                if (!decimal.TryParse(trimmed, out decimal val) || val < 0) return;
                int decimals = QuantityDecimalPlaces;
                decimal rounded = decimals == 0 ? Math.Round(val, 0) : Math.Round(val, decimals);
                if (rounded != Quantity)
                {
                    LogService.Info($"[QuantityDisplay] MaterialName={MaterialName}, OldQuantity={Quantity}, NewQuantity={rounded}");
                    Quantity = rounded;
                }
            }
        }

        public decimal TotalPrice
        {
            get
            {
                // 复合物料主行：TotalPrice = 主行数量 × 所有子项的小计之和
                if (IsComposite && Children.Count > 0)
                {
                    return Quantity * Children.Sum(c => c.UnitPrice * c.Quantity);
                }
                return UnitPrice * Quantity;
            }
        }

        /// <summary>
        /// 完整名称：
        /// - 复合物料子项：部品类型-柜子名-物料名（如"固装-电视柜-铰链"）
        /// - 普通物料：部品类型-物料名（如"固装-三聚氰胺饰面板"）
        /// </summary>
        public string FullDisplayName
        {
            get
            {
                // 复合物料子项：有 ItemName 时显示 部品-柜子名-物料名
                if (IsComposite && !string.IsNullOrEmpty(ItemName))
                    return $"{ComponentName}-{CabinetName}-{MaterialName}";
                // 普通物料或复合物料主行：部品类型-物料名
                return $"{ComponentName}-{MaterialName}";
            }
        }

        /// <summary>
        /// 复合物料主行的缩略图：取第一个子项的图片
        /// </summary>
        public string FirstChildImageUrl => Children.FirstOrDefault()?.ImageUrl ?? "";

        public SelectedMaterial()
        {
            Children.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (SelectedMaterial item in e.NewItems) item.AttachParent(this);
                if (e.OldItems != null)
                    foreach (SelectedMaterial item in e.OldItems) item.DetachParent(this);
            };
        }

        private SelectedMaterial? _parentForNotify;
        public void AttachParent(SelectedMaterial parent)
        {
            _parentForNotify = parent;
            foreach (var child in Children)
                child.AttachParent(parent);
        }
        public void DetachParent(SelectedMaterial parent)
        {
            if (_parentForNotify == parent) _parentForNotify = null;
            foreach (var child in Children)
                child.DetachParent(parent);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
