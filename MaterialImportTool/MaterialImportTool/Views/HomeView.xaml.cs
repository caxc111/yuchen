using MaterialImportTool.ViewModels;
using System.Windows.Controls;

namespace MaterialImportTool.Views
{
    public partial class HomeView : UserControl
    {
        private readonly MainViewModel _mainViewModel;

        public HomeView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
        }

        private void FactoryButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _mainViewModel.ShowFactoryCommand.Execute(null);
        }

        private void ProductButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _mainViewModel.ShowProductCommand.Execute(null);
        }

        private void SettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _mainViewModel.ShowSettingsCommand.Execute(null);
        }
    }
}