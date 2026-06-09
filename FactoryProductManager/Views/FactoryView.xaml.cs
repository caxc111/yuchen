using FactoryProductManager.Models;
using FactoryProductManager.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FactoryProductManager.Views
{
    public partial class FactoryView : UserControl
    {
        private FactoryViewModel _viewModel;

        public FactoryView()
        {
            InitializeComponent();
            _viewModel = new FactoryViewModel();
            DataContext = _viewModel;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FactoryDialogWindow();
            dialog.ShowDialog();
            if (dialog.IsSaved)
            {
                _viewModel.AddFactory(dialog.Factory);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var factory = button?.Tag as Factory;
            if (factory != null)
            {
                var dialog = new FactoryDialogWindow(factory);
                if (dialog.ShowDialog() == true && dialog.IsSaved)
                {
                    _viewModel.UpdateFactory(dialog.Factory);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var factory = button?.Tag as Factory;
            if (factory != null)
            {
                if (MessageBox.Show($"确定要删除工厂 \"{factory.FactoryName}\" 吗？", "确认删除", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _viewModel.DeleteFactory(factory.Id);
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = SearchTextBox?.Text;
            _viewModel.Search(searchText);
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var factory = button?.Tag as Factory;
            if (factory != null)
            {
                var detailsWindow = new FactoryDetailsWindow(factory);
                detailsWindow.Owner = Window.GetWindow(this);
                detailsWindow.ShowDialog();
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToExcel();
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}