using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using FactoryProductManager.Helpers;
using FactoryProductManager.Services;

namespace FactoryProductManager.Views
{
    public partial class MaterialTypePickerDialog : Window
    {
        public string? SelectedType { get; private set; }

        public MaterialTypePickerDialog(List<string> types)
        {
            InitializeComponent();
            TypeList.ItemsSource = types;
            if (types.Count == 1)
            {
                TitleText.Text = $"请选择：{types[0]}";
            }
            else
            {
                TitleText.Text = "请选择物料类型（单选）";
            }

            WindowPositionService.AddPositionProtection(this);
            this.EnableTrayMinimize();
        }

        private void Item_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.Tag is string t)
            {
                SelectedType = t;
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
            DialogResult = false;
            Close();
        }
    }
}
