using FactoryProductManager.Helpers;
using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FactoryProductManager.Views
{
    public partial class PartEditorDialog : Window
    {
        public ProductPart? Part { get; private set; }

        private readonly ProductPart? _originalPart;

        public PartEditorDialog(ProductPart? part = null)
        {
            InitializeComponent();
            _originalPart = part;
            Part = part != null ? ClonePart(part) : new ProductPart { Quantity = 1 };

            if (part != null)
            {
                Title = "编辑自定义部件";
                PartNameTextBox.Text = part.PartName;
            }
            else
            {
                Title = "添加自定义部件";
            }

            DataContext = this;

            WindowPositionService.AddPositionProtection(this);
            this.EnableTrayMinimize();
        }

        private static ProductPart ClonePart(ProductPart source)
        {
            return new ProductPart
            {
                Id = source.Id,
                ProductId = source.ProductId,
                PartName = source.PartName,
                PartCode = source.PartCode,
                PartType = source.PartType,
                Material = source.Material,
                Specification = source.Specification,
                Quantity = source.Quantity,
                Unit = source.Unit,
                UnitPrice = source.UnitPrice,
                TotalPrice = source.TotalPrice,
                Remarks = source.Remarks,
                IsActive = source.IsActive,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PartNameTextBox.Text))
            {
                MessageBox.Show("请输入部件名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Part != null)
            {
                Part.PartName = PartNameTextBox.Text.Trim();
                Part.Unit = "件";
                Part.UpdatedAt = DateTime.Now;

                if (Part.Id == 0)
                {
                    Part.CreatedAt = DateTime.Now;
                }
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
    }
}
