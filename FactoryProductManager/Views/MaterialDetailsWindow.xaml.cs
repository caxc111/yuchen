using FactoryProductManager.Helpers;
using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class MaterialDetailsWindow : Window
    {
        public MaterialDetailsWindow(FactoryMaterial material)
        {
            InitializeComponent();
            DataContext = material;

            StateChanged += MaterialDetailsWindow_StateChanged;

            WindowPositionService.AddPositionProtection(this);
            this.EnableTrayMinimize();
        }

        private void MaterialDetailsWindow_StateChanged(object? sender, System.EventArgs e)
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
