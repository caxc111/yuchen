using MaterialImportTool.Models;
using MaterialImportTool.Services;
using MaterialImportTool.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MaterialImportTool.Views
{
    public partial class ProductView : UserControl
    {
        private readonly ProductViewModel _viewModel;

        public ProductView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _viewModel = new ProductViewModel(mainViewModel.DbService, mainViewModel);
            DataContext = _viewModel;
        }

        private void BackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.BackToHomeCommand.Execute(null);
        }

        private void Category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.CategoryChangedCommand.Execute(null);
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Product product)
            {
                return;
            }

            var details = $"工厂物料编码：{product.FactoryProductCode}\n" +
                          $"宇辰物料编码：{product.MyProductCode ?? string.Empty}\n" +
                          $"产品名称：{product.ProductName}\n" +
                          $"品牌：{product.Brand ?? string.Empty}\n" +
                          $"规格：{product.Specification ?? string.Empty}\n" +
                          $"材质：{product.Texture ?? string.Empty}\n" +
                          $"工艺：{product.Process ?? string.Empty}\n" +
                          $"分类：{product.Category ?? string.Empty}\n" +
                          $"二级分类：{product.SubCategory ?? string.Empty}\n" +
                          $"适用场景：{product.UsageScenario ?? string.Empty}\n" +
                          $"认证情况：{product.Certifications ?? string.Empty}\n" +
                          $"所属工厂：{product.FactoryName ?? string.Empty}";

            MessageBox.Show(details, "产品详细信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
