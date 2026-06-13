using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace FactoryProductManager.Views
{
    public partial class MaterialSelectorDialog : Window
    {
        private readonly DbService _dbService;
        private readonly string _materialType;
        public List<FactoryMaterial> SelectedMaterials { get; private set; } = new();
        private ObservableCollection<FactoryMaterial> _materials = new();

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
            _materials = new ObservableCollection<FactoryMaterial>(materials);
            MaterialsDataGrid.ItemsSource = _materials;
            LogService.Debug($"[MaterialSelectorDialog] 加载了 {_materials.Count} 个物料");
        }

        private void MaterialsDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            while (dep != null && dep is not DataGridRow)
                dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
            if (dep is DataGridRow row && row.DataContext is FactoryMaterial material)
            {
                LogService.Debug($"[MaterialSelectorDialog] 单击选中: {material.MaterialName}");
                material.IsSelected = !material.IsSelected;
                UpdateOkButtonState();
            }
        }

        private void MaterialsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MaterialsDataGrid.SelectedItem is FactoryMaterial material)
            {
                material.IsSelected = true;
                OkButton_Click(null!, null!);
            }
        }

        private void UpdateOkButtonState()
        {
            var selectedItems = _materials.Where(m => m.IsSelected).ToList();
            OkButton.IsEnabled = selectedItems.Count > 0;
            OkButton.Content = selectedItems.Count > 0 ? $"确认选择 ({selectedItems.Count})" : "确认选择";
        }

        private void Thumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.DataContext is FactoryMaterial material)
            {
                if (!string.IsNullOrEmpty(material.ImageUrl))
                {
                    var imageViewer = new ImageViewerWindow(material.ImageUrl);
                    imageViewer.Owner = this;
                    imageViewer.ShowDialog();
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedMaterials = _materials.Where(m => m.IsSelected).ToList();
            if (SelectedMaterials.Count > 0)
            {
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

