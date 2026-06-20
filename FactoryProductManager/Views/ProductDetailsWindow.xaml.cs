using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Linq;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class ProductDetailsWindow : Window
    {
        public string BusinessType { get; set; } = "";
        public string ProjectCode { get; set; } = "";
        public string PartsSummary { get; set; } = "";
        public string HouseType { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public decimal Area { get; set; }
        public decimal CostTotalPrice { get; set; }
        public string FloorPlan { get; set; } = "";
        public bool IsActive { get; set; }

        public ProductDetailsWindow(Product product)
        {
            InitializeComponent();

            BusinessType = product.BusinessType;
            ProjectCode = product.ProjectCode;
            HouseType = product.HouseType;
            ProductCode = product.ProductCode;
            Area = product.Area;
            CostTotalPrice = product.CostTotalPrice;
            FloorPlan = product.FloorPlan;
            IsActive = product.IsActive;

            // 加载部件摘要
            if (product.Id > 0)
            {
                try
                {
                    var db = new DbService();
                    var parts = db.GetProductParts(product.Id);
                    if (parts.Count > 0)
                    {
                        PartsSummary = string.Join("，", parts.Select(p => $"{p.PartName}*{p.Quantity}"));
                    }
                }
                catch { }
            }

            DataContext = this;
            StateChanged += ProductDetailsWindow_StateChanged;
        }

        private void ProductDetailsWindow_StateChanged(object? sender, System.EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.ToolTip = "还原";
            }
            else
            {
                MaximizeButton.ToolTip = "最大化";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
