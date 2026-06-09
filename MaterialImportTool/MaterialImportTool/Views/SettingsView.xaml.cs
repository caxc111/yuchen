using MaterialImportTool.Services;
using MaterialImportTool.ViewModels;
using System.Windows.Controls;

namespace MaterialImportTool.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel(mainViewModel.DbService, mainViewModel);
            DataContext = _viewModel;
        }

        private void BackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.BackToHomeCommand.Execute(null);
        }
    }
}
