using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FactoryProductManager.Models
{
    public class ProductPartMaterial : INotifyPropertyChanged
    {
        private decimal _quantity = 1;

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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

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

        public decimal TotalPrice => UnitPrice * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
