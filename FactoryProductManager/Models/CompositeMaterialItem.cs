using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FactoryProductManager.Models
{
    /// <summary>
    /// 复合物料子项（柜体、门板、台面、五金等）
    /// </summary>
    public class CompositeMaterialItem : INotifyPropertyChanged
    {
        private int _id;
        private int _factoryMaterialId;
        private string _itemName = "";          // 如"柜体"、"门板"、"台面"、"五金"
        private string _materialName = "";
        private string _specification = "";
        private string _unit = "";
        private decimal _unitPrice;
        private double _quantity = 1;
        private string _factoryMaterialCode = "";
        private string _myMaterialCode = "";
        private string _brand = "";
        private string _imageUrl = "";

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int FactoryMaterialId
        {
            get => _factoryMaterialId;
            set { _factoryMaterialId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 子项名称：柜体、门板、台面、五金
        /// </summary>
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(); }
        }

        public string MaterialName
        {
            get => _materialName;
            set { _materialName = value; OnPropertyChanged(); }
        }

        public string Specification
        {
            get => _specification;
            set { _specification = value; OnPropertyChanged(); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set { _unitPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPrice)); }
        }

        /// <summary>
        /// 数量（支持小数，如门板 0.8）
        /// </summary>
        public double Quantity
        {
            get => _quantity;
            set
            {
                if (Math.Abs(_quantity - value) > 0.0001)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        /// <summary>
        /// 小计 = 单价 × 数量
        /// </summary>
        public decimal TotalPrice => (decimal)Quantity * UnitPrice;

        public string FactoryMaterialCode
        {
            get => _factoryMaterialCode;
            set { _factoryMaterialCode = value; OnPropertyChanged(); }
        }

        public string MyMaterialCode
        {
            get => _myMaterialCode;
            set { _myMaterialCode = value; OnPropertyChanged(); }
        }

        public string Brand
        {
            get => _brand;
            set { _brand = value; OnPropertyChanged(); }
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set { _imageUrl = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
