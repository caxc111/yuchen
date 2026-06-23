using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        public new bool IsActive { get; set; }

        public ObservableCollection<MaterialDisplayItem> MaterialsList { get; } = new();

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

                    // 加载所有物料详情
                    var materials = db.LoadProductMaterialsFromLibrary(product.Id);
                    foreach (var m in materials)
                    {
                        decimal compositeUnitPrice = 0;
                        if (m.IsComposite)
                        {
                            // 子项小计之和（不含主行数量）
                            compositeUnitPrice = m.Children.Sum(c => c.UnitPrice * c.Quantity);
                        }

                        MaterialsList.Add(new MaterialDisplayItem
                        {
                            PartName = m.PartName ?? "",
                            ComponentName = m.ComponentName ?? "",
                            MaterialName = m.MaterialName ?? "",
                            Specification = m.Specification ?? "",
                            Unit = m.Unit ?? "",
                            UnitPrice = m.UnitPrice,
                            Quantity = m.Quantity,
                            TotalPrice = m.TotalPrice,
                            Remarks = m.Remarks ?? "",
                            IsComposite = m.IsComposite,
                            CompositeUnitPrice = compositeUnitPrice
                        });
                    }
                }
                catch { }
            }

            DataContext = this;
            StateChanged += ProductDetailsWindow_StateChanged;

            WindowPositionService.AddPositionProtection(this);
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

    public class MaterialDisplayItem
    {
        public string PartName { get; set; } = "";
        public string ComponentName { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public string Specification { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string Remarks { get; set; } = "";
        public bool IsComposite { get; set; }
        public decimal CompositeUnitPrice { get; set; }

        public string DisplayText
        {
            get
            {
                if (IsComposite)
                {
                    // 复合物料显示：主行数量 × 子项合价 = 总价
                    return $"{PartName}-{ComponentName}：{MaterialName}" +
                        (string.IsNullOrEmpty(Specification) ? "" : $"（{Specification}）") +
                        $" {Quantity}套 × 子项合价¥{CompositeUnitPrice:F2} = ¥{TotalPrice:F2}";
                }
                else
                {
                    // 普通物料显示：数量 × 单价 = 小计
                    return $"{PartName}-{ComponentName}：{MaterialName}" +
                        (string.IsNullOrEmpty(Specification) ? "" : $"（{Specification}）") +
                        $" {Quantity}{Unit} × ¥{UnitPrice:F2} = ¥{TotalPrice:F2}";
                }
            }
        }
    }
}
