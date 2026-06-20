using System;
using System.ComponentModel;

namespace FactoryProductManager.Models
{
    public class Product : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private int _id;
        private string _businessType = string.Empty;
        private string _productCode = string.Empty;
        private string _productName = string.Empty;
        private string _projectCode = string.Empty;
        private string _houseType = string.Empty;
        private decimal _area;
        private decimal _costTotalPrice;
        private decimal? _sellingTotalPrice;
        private string _floorPlan = string.Empty;
        private bool _isActive;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string BusinessType
        {
            get => _businessType;
            set { _businessType = value; OnPropertyChanged(nameof(BusinessType)); }
        }

        public string ProductCode
        {
            get => _productCode;
            set { _productCode = value; OnPropertyChanged(nameof(ProductCode)); }
        }

        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(nameof(ProductName)); }
        }

        public string ProjectCode
        {
            get => _projectCode;
            set { _projectCode = value; OnPropertyChanged(nameof(ProjectCode)); }
        }

        public string HouseType
        {
            get => _houseType;
            set { _houseType = value; OnPropertyChanged(nameof(HouseType)); }
        }

        public decimal Area
        {
            get => _area;
            set { _area = value; OnPropertyChanged(nameof(Area)); }
        }

        public decimal CostTotalPrice
        {
            get => _costTotalPrice;
            set { _costTotalPrice = value; OnPropertyChanged(nameof(CostTotalPrice)); }
        }

        public decimal? SellingTotalPrice
        {
            get => _sellingTotalPrice;
            set { _sellingTotalPrice = value; OnPropertyChanged(nameof(SellingTotalPrice)); }
        }

        public string FloorPlan
        {
            get => _floorPlan;
            set { _floorPlan = value; OnPropertyChanged(nameof(FloorPlan)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(nameof(UpdatedAt)); }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
