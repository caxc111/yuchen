using FactoryProductManager.Models;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class ProductDetailsWindow : Window
    {
        public ProductDetailsWindow(Product product)
        {
            InitializeComponent();
            DataContext = product;

            StateChanged += ProductDetailsWindow_StateChanged;
        }

        private void ProductDetailsWindow_StateChanged(object? sender, System.EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.ToolTip = "还原";
            }
            else
            {
                MaximizeButton.ToolTip = "最大化";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
