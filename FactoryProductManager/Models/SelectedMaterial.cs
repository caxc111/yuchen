using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FactoryProductManager.Models
{
    public class SelectedMaterial : INotifyPropertyChanged
    {
        private int _quantity = 1;

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

        // 复合物料
        public bool IsComposite { get; set; }
        public string GroupCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int? ParentRef { get; set; }

        // 柜子名称（如"电视柜"、"玄关柜"），用于组合显示名
        public string CabinetName { get; set; } = "";

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
                    _parentForNotify?.OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal TotalPrice
        {
            get
            {
                // 复合物料主行：TotalPrice = 所有子项的 TotalPrice 之和
                if (IsComposite && Children.Count > 0)
                {
                    return Children.Sum(c => c.TotalPrice);
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
        protected void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
