using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System.Collections.Generic;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class MaterialSelectorDialog : Window
    {
        private readonly DbService _dbService;
        private readonly string _materialType;
        public FactoryMaterial? SelectedMaterial { get; private set; }

        public MaterialSelectorDialog(string materialType, DbService dbService)
        {
            InitializeComponent();

            _materialType = materialType;
            _dbService = dbService;

            TitleText.Text = $"选择{materialType}";
            LoadMaterials();

            StateChanged += MaterialSelectorDialog_StateChanged;
        }

        private void MaterialSelectorDialog_StateChanged(object? sender, System.EventArgs e)
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

        private void LoadMaterials()
        {
            var materials = _dbService.GetFactoryMaterialsByType(_materialType);
            MaterialsDataGrid.ItemsSource = materials;
        }

        private void MaterialsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            OkButton.IsEnabled = MaterialsDataGrid.SelectedItem != null;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsDataGrid.SelectedItem is FactoryMaterial material)
            {
                SelectedMaterial = material;
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
            Close();
        }
    }
}
