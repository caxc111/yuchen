using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FactoryProductManager.Models
{
    /// <summary>
    /// 复合物料（一个组合，如"定制橱柜"包含柜体+门板+台面+五金）
    /// </summary>
    public class CompositeMaterial : INotifyPropertyChanged
    {
        private int _id;
        private string _partName = "";
        private string _componentName = "";
        private string _materialTypeName = "";
        private string _groupCode = "";
        private string _cabinetName = "";  // 如"定制橱柜"、"玄关柜"
        private int _quantity = 1;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 部品名称：门厅、主卧室等
        /// </summary>
        public string PartName
        {
            get => _partName;
            set { _partName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        /// <summary>
        /// 部品类型名称：固装、灯具等
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set { _componentName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string MaterialTypeName
        {
            get => _materialTypeName;
            set { _materialTypeName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 组合模板编码：如 GX-001
        /// </summary>
        public string GroupCode
        {
            get => _groupCode;
            set { _groupCode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 柜子名称：如"定制橱柜"、"电视柜"
        /// </summary>
        public string CabinetName
        {
            get => _cabinetName;
            set { _cabinetName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public int Quantity
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

        /// <summary>
        /// 子项列表（柜体、门板、台面、五金等）
        /// </summary>
        public ObservableCollection<CompositeMaterialItem> Items { get; } = new();

        /// <summary>
        /// 总价 = 所有子项总价之和 × 数量
        /// </summary>
        public decimal TotalPrice => Items.Sum(i => i.TotalPrice) * Quantity;

        /// <summary>
        /// 显示名称：部品类型-柜子名（如"固装-定制橱柜"）
        /// </summary>
        public string DisplayName => $"{ComponentName}-{CabinetName}";

        /// <summary>
        /// 缩略图：取第一个子项的图片
        /// </summary>
        public string ImageUrl => Items.FirstOrDefault()?.ImageUrl ?? "";

        public CompositeMaterial()
        {
            Items.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (CompositeMaterialItem item in e.NewItems)
                        item.PropertyChanged += Item_PropertyChanged;
                if (e.OldItems != null)
                    foreach (CompositeMaterialItem item in e.OldItems)
                        item.PropertyChanged -= Item_PropertyChanged;
                OnPropertyChanged(nameof(TotalPrice));
            };
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompositeMaterialItem.TotalPrice))
                OnPropertyChanged(nameof(TotalPrice));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
