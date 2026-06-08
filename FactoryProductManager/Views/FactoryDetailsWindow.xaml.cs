using FactoryProductManager.Models;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class FactoryDetailsWindow : Window
    {
        public FactoryDetailsWindow(Factory factory)
        {
            InitializeComponent();
            DataContext = factory;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}