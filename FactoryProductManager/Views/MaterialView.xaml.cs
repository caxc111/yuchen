using FactoryProductManager.Models;
using FactoryProductManager.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FactoryProductManager.Views
{
    public partial class MaterialView : UserControl
    {
        private readonly MaterialViewModel _viewModel;

        public MaterialView()
        {
            InitializeComponent();
            _viewModel = new MaterialViewModel();
            DataContext = _viewModel;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MaterialDialogWindow();
            dialog.ShowDialog();
            if (dialog.IsSaved)
            {
                _viewModel.AddMaterial(dialog.Material);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var material = button?.Tag as FactoryMaterial;
            if (material != null)
            {
                var dialog = new MaterialDialogWindow(material);
                dialog.ShowDialog();
                if (dialog.IsSaved)
                {
                    _viewModel.UpdateMaterial(dialog.Material);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var material = button?.Tag as FactoryMaterial;
            if (material != null)
            {
                if (MessageBox.Show($"确定要删除物料 \"{material.MaterialName}\" 吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteMaterial(material.Id);
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchTextBox?.Text;
            _viewModel.Search(searchText);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToExcel();
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var material = button?.Tag as FactoryMaterial;
            if (material != null)
            {
                var detailsWindow = new MaterialDetailsWindow(material);
                detailsWindow.Owner = Window.GetWindow(this);
                detailsWindow.ShowDialog();
            }
        }
    }
}
