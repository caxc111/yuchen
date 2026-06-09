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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductManagementDialog();
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
            if (dialog.IsSaved)
            {
                _viewModel.AddProduct(dialog.Product);
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
                _viewModel.UpdateProduct(dialog.Product);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Product product)
            {
                return;
            }

            if (MessageBox.Show($"确定要删除产品 \"{product.ProductName}\" 吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _viewModel.DeleteProduct(product.Id);
            }
        }
    }
}
