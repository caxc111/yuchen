using MaterialImportTool.Services;
using MaterialImportTool.ViewModels;
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
            (_viewModel as ProductViewModel).BackToHomeCommand.Execute(null);
        }

        private void Category_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (_viewModel as ProductViewModel).CategoryChangedCommand.Execute(null);
        }
    }
}