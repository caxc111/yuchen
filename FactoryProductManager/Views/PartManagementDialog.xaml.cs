using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FactoryProductManager.Views
{
    public partial class PartManagementDialog : Window, INotifyPropertyChanged
    {
        private readonly DbService _dbService;
        private readonly int _productId;
        private readonly bool _isNewProduct;
        private ProductPart? _selectedPart;
        public ObservableCollection<ProductPart> Parts { get; } = new();
        private readonly Dictionary<string, ComboBox> _partQuantityComboBoxes = new();
        private readonly Dictionary<string, CheckBox> _partCheckBoxes = new();

        // 自定义部位列表（不在预设选项中的部位）
        public ObservableCollection<ProductPart> CustomParts { get; } = new();

        // 预设部位名称
        private static readonly string[] PartNameOptions = new[]
        {
            "门厅", "客餐厨", "主卧室", "主卫生间",
            "次卧室", "次卫生间", "洗衣房", "书房", "阳台"
        };

        // 数量选项 1-5
        public static readonly int[] QuantityOptions = { 1, 2, 3, 4, 5 };

        // 自定义部位是否为空
        public bool CustomPartsEmpty => CustomParts.Count == 0;

        public ProductPart? SelectedPart
        {
            get => _selectedPart;
            set
            {
                _selectedPart = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? PartsChanged;

        public PartManagementDialog(int productId, bool isNewProduct = false)
        {
            InitializeComponent();
            _productId = productId;
            _isNewProduct = isNewProduct;
            _dbService = new DbService();
            DataContext = this;

            CustomPartsPanel.ItemsSource = CustomParts;
            CreatePartNameOptions();
            LoadParts();
        }

        private void CreatePartNameOptions()
        {
            PartNamePanel.Children.Clear();
            _partQuantityComboBoxes.Clear();
            _partCheckBoxes.Clear();

            foreach (var option in PartNameOptions)
            {
                var container = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 8)
                };
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var checkBox = new CheckBox
                {
                    Content = option,
                    Tag = option,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                checkBox.Checked += PartCheckBox_Changed;
                checkBox.Unchecked += PartCheckBox_Changed;

                var quantityComboBox = new ComboBox
                {
                    Style = (Style)FindResource("PartOptionComboBoxStyle"),
                    ItemsSource = QuantityOptions,
                    SelectedItem = 1,
                    Tag = option,
                    IsEnabled = false,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                quantityComboBox.SelectionChanged += QuantityComboBox_SelectionChanged;

                Grid.SetColumn(checkBox, 0);
                Grid.SetColumn(quantityComboBox, 2);

                container.Children.Add(checkBox);
                container.Children.Add(quantityComboBox);

                _partCheckBoxes[option] = checkBox;
                _partQuantityComboBoxes[option] = quantityComboBox;

                PartNamePanel.Children.Add(container);
            }
        }

        private void PartCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var partName = checkBox.Tag?.ToString();
                if (!string.IsNullOrEmpty(partName) && _partQuantityComboBoxes.TryGetValue(partName, out var comboBox))
                {
                    comboBox.IsEnabled = checkBox.IsChecked == true;

                    if (checkBox.IsChecked == true)
                    {
                        AddPartByName(partName, (int)(comboBox.SelectedItem ?? 1));
                    }
                    else
                    {
                        RemovePartByName(partName);
                    }
                }
            }
        }

        private void QuantityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                var partName = comboBox.Tag?.ToString();
                if (!string.IsNullOrEmpty(partName) && comboBox.SelectedItem is int quantity)
                {
                    UpdatePartQuantity(partName, quantity);
                }
            }
        }

        private void AddPartByName(string partName, int quantity = 1)
        {
            var existingPart = Parts.FirstOrDefault(p => p.PartName == partName);
            if (existingPart != null)
            {
                existingPart.Quantity = quantity;
                OnPropertyChanged(nameof(Parts));
                return;
            }

            var newPart = new ProductPart
            {
                PartName = partName,
                Quantity = quantity,
                Unit = "件"
            };

            if (_isNewProduct || _productId <= 0)
            {
                newPart.Id = -(Parts.Count + 1);
                newPart.ProductId = 0;
            }
            else
            {
                try
                {
                    newPart.ProductId = _productId;
                    int newId = _dbService.AddProductPart(newPart);
                    newPart.Id = newId;
                    PartsChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"添加部位失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            Parts.Add(newPart);
            SelectedPart = newPart;
        }

        private void RemovePartByName(string partName)
        {
            var part = Parts.FirstOrDefault(p => p.PartName == partName);
            if (part != null)
            {
                if (!_isNewProduct && _productId > 0 && part.Id > 0)
                {
                    try
                    {
                        _dbService.DeleteProductPart(part.Id);
                        PartsChanged?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除部位失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                Parts.Remove(part);
            }
        }

        private void UpdatePartQuantity(string partName, int quantity)
        {
            var part = Parts.FirstOrDefault(p => p.PartName == partName);
            if (part != null)
            {
                part.Quantity = quantity;
                if (!_isNewProduct && _productId > 0 && part.Id > 0)
                {
                    try
                    {
                        _dbService.UpdateProductPart(part);
                        PartsChanged?.Invoke();
                    }
                    catch { }
                }
                OnPropertyChanged(nameof(Parts));
            }
        }

        private void LoadParts()
        {
            Parts.Clear();
            CustomParts.Clear();

            if (!_isNewProduct && _productId > 0)
            {
                try
                {
                    var parts = _dbService.GetProductParts(_productId);
                    foreach (var part in parts)
                    {
                        if (PartNameOptions.Contains(part.PartName))
                        {
                            Parts.Add(part);
                        }
                        else
                        {
                            CustomParts.Add(part);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载部位数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            SyncCheckBoxesWithParts();
            OnPropertyChanged(nameof(CustomPartsEmpty));
        }

        private void SyncCheckBoxesWithParts()
        {
            foreach (var kvp in _partCheckBoxes)
            {
                var partName = kvp.Key;
                var checkBox = kvp.Value;
                var existingPart = Parts.FirstOrDefault(p => p.PartName == partName);

                if (existingPart != null)
                {
                    checkBox.IsChecked = true;
                    if (_partQuantityComboBoxes.TryGetValue(partName, out var comboBox))
                    {
                        comboBox.IsEnabled = true;
                        comboBox.SelectedItem = existingPart.Quantity >= 1 && existingPart.Quantity <= 5 
                            ? existingPart.Quantity : 1;
                    }
                }
                else
                {
                    checkBox.IsChecked = false;
                    if (_partQuantityComboBoxes.TryGetValue(partName, out var comboBox))
                    {
                        comboBox.IsEnabled = false;
                        comboBox.SelectedItem = 1;
                    }
                }
            }
        }

        private void AddCustomPartButton_Click(object sender, RoutedEventArgs e)
        {
            var partEditor = new PartEditorDialog();
            if (partEditor.ShowDialog() == true && partEditor.Part != null)
            {
                var newPart = partEditor.Part;

                if (_isNewProduct || _productId <= 0)
                {
                    newPart.Id = -(Parts.Count + CustomParts.Count + 1);
                    newPart.ProductId = 0;
                    newPart.Quantity = 1;
                    CustomParts.Add(newPart);
                }
                else
                {
                    try
                    {
                        newPart.ProductId = _productId;
                        newPart.Quantity = 1;
                        int newId = _dbService.AddProductPart(newPart);
                        newPart.Id = newId;
                        CustomParts.Add(newPart);
                        PartsChanged?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"添加部位失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                OnPropertyChanged(nameof(CustomPartsEmpty));
            }
        }

        private void DeleteCustomPart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductPart part)
            {
                if (!_isNewProduct && _productId > 0 && part.Id > 0)
                {
                    try
                    {
                        _dbService.DeleteProductPart(part.Id);
                        PartsChanged?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除部位失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                CustomParts.Remove(part);
                OnPropertyChanged(nameof(CustomPartsEmpty));
            }
        }

        private void EditPartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ProductPart part)
            {
                var partEditor = new PartEditorDialog(part);
                if (partEditor.ShowDialog() == true && partEditor.Part != null)
                {
                    var updatedPart = partEditor.Part;

                    if (_isNewProduct || _productId <= 0 || part.Id < 0)
                    {
                        var index = Parts.IndexOf(part);
                        if (index >= 0)
                        {
                            Parts[index] = updatedPart;
                            OnPropertyChanged(nameof(Parts));
                        }
                    }
                    else
                    {
                        try
                        {
                            updatedPart.Id = part.Id;
                            updatedPart.ProductId = _productId;
                            _dbService.UpdateProductPart(updatedPart);
                            var index = Parts.IndexOf(part);
                            if (index >= 0)
                            {
                                Parts[index] = updatedPart;
                                OnPropertyChanged(nameof(Parts));
                            }
                            PartsChanged?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"更新部位失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
