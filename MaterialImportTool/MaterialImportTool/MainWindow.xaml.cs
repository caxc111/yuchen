using MaterialImportTool.ViewModels;
using System.Windows;

namespace MaterialImportTool
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}