using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class FactoryDetailsWindow : Window
    {
        public FactoryDetailsWindow(Factory factory)
        {
            InitializeComponent();
            DataContext = factory;

            StateChanged += FactoryDetailsWindow_StateChanged;

            WindowPositionService.AddPositionProtection(this);
        }

        private void FactoryDetailsWindow_StateChanged(object? sender, System.EventArgs e)
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
