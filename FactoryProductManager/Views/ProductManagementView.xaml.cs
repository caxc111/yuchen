using FactoryProductManager.Models;
using FactoryProductManager.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FactoryProductManager.Views
{
    public partial class ProductManagementView : UserControl
    {
        private readonly ProductManagementViewModel _viewModel;

        public ProductManagementView()
        {
            InitializeComponent();
            _viewModel = new ProductManagementViewModel();
            DataContext = _viewModel;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Refresh();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToExcel();
        }

        private void ToggleInactiveButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowInactiveProducts = !_viewModel.ShowInactiveProducts;
        }

        private void StatusToggle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not Product product)
            {
                return;
            }

            if (product.IsActive)
            {
                if (MessageBox.Show($"确定要停用产品 \"{product.ProductCode}\" 吗？停用后将不在列表中显示。", "确认停用",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _viewModel.DisableProduct(product.Id);
                }
            }
            else
            {
                if (MessageBox.Show($"确定要启用产品 \"{product.ProductCode}\" 吗？", "确认启用",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _viewModel.EnableProduct(product.Id);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductManagementDialog();
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
            if (dialog.IsSaved)
            {
                _viewModel.AddProduct(dialog.Product, dialog.PendingParts, dialog.PendingMaterials);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Product product)
            {
                return;
            }

            var dialog = new ProductManagementDialog(product);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
            if (dialog.IsSaved)
            {
                _viewModel.UpdateProduct(dialog.Product, dialog.PendingParts, dialog.PendingMaterials);
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Product product)
            {
                return;
            }

            var detailsWindow = new ProductDetailsWindow(product);
            detailsWindow.Owner = Window.GetWindow(this);
            detailsWindow.ShowDialog();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Product product)
            {
                return;
            }

            if (MessageBox.Show($"确定要删除产品编码为 \"{product.ProductCode}\" 的产品吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _viewModel.DeleteProduct(product.Id);
            }
        }
    }
}
