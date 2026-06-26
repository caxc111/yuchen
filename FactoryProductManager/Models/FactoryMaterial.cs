using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FactoryProductManager.Models
{
    public class FactoryMaterial : INotifyPropertyChanged
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
        public string SupplyCycle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int? FactoryId { get; set; }
        public string FactoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 图纸编号（用于物料选择对话框中显示/编辑）
        private string _drawingNumber = string.Empty;
        public string DrawingNumber
        {
            get => _drawingNumber;
            set
            {
                if (_drawingNumber != value)
                {
                    _drawingNumber = value;
                    OnPropertyChanged(nameof(DrawingNumber));
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(Subtotal));
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

        public decimal Subtotal => (CostPrice ?? 0m) * Quantity;

        /// <summary>
        /// 用于 DataGrid 显示的数量文本（带格式化）
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
                    _quantity = rounded;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        public string CategoryDisplay => string.IsNullOrWhiteSpace(Category)
            ? string.Empty
            : Category.Split(new[] { " > " }, StringSplitOptions.None).LastOrDefault() ?? Category;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
