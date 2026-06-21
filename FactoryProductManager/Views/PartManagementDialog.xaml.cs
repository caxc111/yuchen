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

        // 自定义部件列表（不在预设选项中的部件）
        public ObservableCollection<ProductPart> CustomParts { get; } = new();

        // 暂存"待新增"的预设部件（_isNewProduct=true 时累积，Ok 时由外部落库）
        private readonly List<ProductPart> _pendingPresetParts = new();
        // 暂存"待新增"的自定义部件
        private readonly List<ProductPart> _pendingCustomParts = new();
        // 暂存"待删除"的预设部件 id（_isNewProduct=true 时累积，Ok 时由外部落库）
        private readonly List<int> _pendingRemovedPresetIds = new();
        // 暂存"待删除"的自定义部件 id
        private readonly List<int> _pendingRemovedCustomIds = new();
        // 调用方传入的"上次已选部件"（避免再次打开窗口时清空）
        private readonly IReadOnlyList<ProductPart>? _existingParts;

        // 预设部件名称
        private static readonly string[] PartNameOptions = new[]
        {
            "门厅", "客餐厨", "主卧室", "主卫生间",
            "次卧室", "次卫生间", "洗衣房", "书房", "阳台"
        };

        // 数量选项 1-5
        public static readonly int[] QuantityOptions = { 1, 2, 3, 4, 5 };

        // 自定义部件是否为空
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
#pragma warning disable CS0067 // PartsChanged 事件保留用于将来可能的使用
        public event Action? PartsChanged;
#pragma warning restore CS0067

        public PartManagementDialog(int productId, bool isNewProduct = false, IReadOnlyList<ProductPart>? existingParts = null)
        {
            InitializeComponent();
            _productId = productId;
            _isNewProduct = isNewProduct;
            _existingParts = existingParts;
            _dbService = new DbService();
            DataContext = this;

            CustomPartsPanel.ItemsSource = CustomParts;
            CreatePartNameOptions();
            LoadParts();

            WindowPositionService.AddPositionProtection(this);
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
                _pendingPresetParts.Add(newPart);
            }
            else
            {
                try
                {
                    newPart.ProductId = _productId;
                    int newId = _dbService.AddProductPart(newPart);
                    newPart.Id = newId;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"添加部件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除部件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    _pendingPresetParts.Remove(part);
                    _pendingRemovedPresetIds.Remove(part.Id);
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

            // 1) 先加载 DB 中已保存的部件（仅对已存在产品 _productId > 0）
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
                    MessageBox.Show($"加载部件数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // 2) 合并调用方传入的"上次已选部件"（覆盖 DB 数据，避免再次打开窗口时清空）
            //    _existingParts 是上次 Ok 时缓存的清单，按 PartName 去重合并
            //    自定义部件的 Quantity 在 UI 上没意义，强制为 1 保证下拉显示
            if (_existingParts != null)
            {
                foreach (var ep in _existingParts)
                {
                    if (string.IsNullOrWhiteSpace(ep.PartName)) continue;
                    if (PartNameOptions.Contains(ep.PartName))
                    {
                        if (!Parts.Any(p => p.PartName == ep.PartName))
                        {
                            Parts.Add(ep);
                        }
                    }
                    else
                    {
                        if (!CustomParts.Any(p => p.PartName == ep.PartName))
                        {
                            ep.Quantity = 1; // 防御：合并时强制 1
                            CustomParts.Add(ep);
                        }
                    }
                }
            }

            SyncCheckBoxesWithParts();
            OnPropertyChanged(nameof(CustomPartsEmpty));

            // 新加载的自定义部件行会在 DataTemplate 根 Grid Loaded 事件中
            // 自动通过 CustomPartRow_Loaded 初始化 ComboBox.SelectedItem = 1
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

                // 无论 PartEditorDialog 返回什么 Quantity，自定义部件都强制从 1 开始
                newPart.Quantity = 1;
                newPart.Unit = string.IsNullOrEmpty(newPart.Unit) ? "件" : newPart.Unit;

                if (_isNewProduct || _productId <= 0)
                {
                    newPart.Id = -(Parts.Count + CustomParts.Count + 1);
                    newPart.ProductId = 0;
                    _pendingCustomParts.Add(newPart);
                }
                else
                {
                    try
                    {
                        newPart.ProductId = _productId;
                        int newId = _dbService.AddProductPart(newPart);
                        newPart.Id = newId;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"添加部件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                CustomParts.Add(newPart);
                OnPropertyChanged(nameof(CustomPartsEmpty));
            }
        }

        // DataTemplate 根 Grid 加载完成时触发 —— 此时 ComboBox 已实例化，直接拿到引用
        private void CustomPartRow_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Grid row) return;
            if (row.DataContext is not ProductPart part) return;
            if (row.FindName("CustomPartQuantityComboBox") is not ComboBox combo) return;

            // 把 decimal 强转 int 默认 1，范围 1-5
            int q = 1;
            if (part.Quantity >= 1 && part.Quantity <= 5)
            {
                q = (int)part.Quantity;
            }
            combo.SelectedItem = q;
        }

        private void CustomPartQuantity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is ProductPart part && combo.SelectedItem is int q)
            {
                // 把 int 写回 decimal 字段，保持 ProductPart.Quantity 类型不变
                part.Quantity = q;
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除部件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    _pendingCustomParts.Remove(part);
                    _pendingRemovedCustomIds.Remove(part.Id);
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
                        var customIndex = CustomParts.IndexOf(part);
                        if (customIndex >= 0)
                        {
                            CustomParts[customIndex] = updatedPart;
                        }
                        var pendingPresetIndex = _pendingPresetParts.IndexOf(part);
                        if (pendingPresetIndex >= 0) _pendingPresetParts[pendingPresetIndex] = updatedPart;
                        var pendingCustomIndex = _pendingCustomParts.IndexOf(part);
                        if (pendingCustomIndex >= 0) _pendingCustomParts[pendingCustomIndex] = updatedPart;
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
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"更新部件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
