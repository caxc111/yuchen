using FactoryProductManager.Models;
using FactoryProductManager.Services;
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
            LogService.Debug($"[ProductManagementView] StatusToggle_Click: ClickCount={e.ClickCount}");
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
            var dialog = new AddProductDialog();
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
            if (dialog.IsSaved)
            {
                _viewModel.Refresh();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Product product)
            {
                return;
            }

            var dialog = new EditProductDialog(product);
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
            if (dialog.IsSaved || dialog.IsDeleted)
            {
                _viewModel.Refresh();
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Debug("[ProductManagementView] DetailsButton_Click 开始");
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

        private void FloorPlanImage_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not Product product)
            {
                return;
            }

            if (string.IsNullOrEmpty(product.FloorPlan))
            {
                MessageBox.Show("该产品没有平面图", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var viewer = new ImageViewerWindow(product.FloorPlan, product.ProductCode + " - 平面图");
            viewer.Owner = Window.GetWindow(this);
            viewer.ShowDialog();
        }
    }
}
