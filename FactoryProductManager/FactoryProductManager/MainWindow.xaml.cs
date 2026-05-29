using FactoryProductManager.ViewModels;
using System.Windows;

namespace FactoryProductManager
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