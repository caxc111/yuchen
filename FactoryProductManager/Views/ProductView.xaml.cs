using FactoryProductManager.Models;
using FactoryProductManager.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FactoryProductManager.Views
{
    public partial class ProductView : UserControl
    {
        private ProductViewModel _viewModel;

        public ProductView()
        {
            InitializeComponent();
            _viewModel = new ProductViewModel();
            DataContext = _viewModel;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProductDialog();
            dialog.ShowDialog();
            if (dialog.IsSaved)
            {
                _viewModel.AddProduct(dialog.Product);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.Tag as FactoryProduct;
            if (product != null)
            {
                var dialog = new ProductDialog(product);
                dialog.ShowDialog();
                if (dialog.IsSaved)
                {
                    _viewModel.UpdateProduct(dialog.Product);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.Tag as FactoryProduct;
            if (product != null)
            {
                if (MessageBox.Show($"确定要删除产品 \"{product.ProductName}\" 吗？", "确认删除", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteProduct(product.Id);
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Refresh();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToExcel();
        }
    }
}