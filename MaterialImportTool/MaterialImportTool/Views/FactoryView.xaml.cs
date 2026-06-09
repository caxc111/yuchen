using MaterialImportTool.Services;
using MaterialImportTool.ViewModels;
using System.Windows.Controls;

namespace MaterialImportTool.Views
{
    public partial class FactoryView : UserControl
    {
        private readonly FactoryViewModel _viewModel;

        public FactoryView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _viewModel = new FactoryViewModel(mainViewModel.DbService, mainViewModel);
            DataContext = _viewModel;
        }

        private void BackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            (_viewModel as FactoryViewModel).BackToHomeCommand.Execute(null);
        }
    }
}