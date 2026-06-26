using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
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
        private readonly List<string> _materialTypes; // 支持多类型
        public List<FactoryMaterial> SelectedMaterials { get; private set; } = new();
        public IReadOnlyCollection<FactoryMaterial> AllMaterials => _materials;
        private ObservableCollection<FactoryMaterial> _materials = new();

        // 预选的物料列表
        private readonly List<SelectedMaterial>? _preselectedMaterials;

        // 是否允许多选（默认单选）
        public bool AllowMultipleSelection { get; set; } = false;

        // 新增构造函数，支持预选
        public MaterialSelectorDialog(string materialType, DbService dbService, List<SelectedMaterial>? preselectedMaterials = null, bool allowMultiple = false)
        {
            try
            {
                LogService.Debug("[MaterialSelectorDialog] 构造函数开始");
                InitializeComponent();
                LogService.Debug("[MaterialSelectorDialog] InitializeComponent 完成");

                _materialType = materialType;
                _materialTypes = new List<string> { materialType };
                _dbService = dbService;
                _preselectedMaterials = preselectedMaterials;
                AllowMultipleSelection = allowMultiple;

                TitleText.Text = $"选择{materialType}";
                LogService.Debug("[MaterialSelectorDialog] 开始 LoadMaterials");
                LoadMaterials();
                LogService.Debug($"[MaterialSelectorDialog] LoadMaterials 完成，物料数={_materials.Count}");

                StateChanged += MaterialSelectorDialog_StateChanged;

                WindowPositionService.AddPositionProtection(this);
                LogService.Debug("[MaterialSelectorDialog] 构造函数完成");
            }
            catch (Exception ex)
            {
                LogService.Error("[MaterialSelectorDialog] 构造函数异常", ex);
                throw;
            }
        }

        // 新增：支持多类型构造函数
        public MaterialSelectorDialog(List<string> materialTypes, DbService dbService, List<SelectedMaterial>? preselectedMaterials = null, bool allowMultiple = false)
        {
            try
            {
                LogService.Debug("[MaterialSelectorDialog] 多类型构造函数开始");
                InitializeComponent();
                LogService.Debug("[MaterialSelectorDialog] 多类型 InitializeComponent 完成");

                _materialType = materialTypes.Count > 0 ? materialTypes[0] : "";
                _materialTypes = materialTypes;
                _dbService = dbService;
                _preselectedMaterials = preselectedMaterials;
                AllowMultipleSelection = allowMultiple;

                TitleText.Text = $"选择{materialTypes[0]}";
                LogService.Debug($"[MaterialSelectorDialog] 多类型 LoadMaterials 开始，types={string.Join(",", materialTypes)}");
                LoadMaterials();
                LogService.Debug($"[MaterialSelectorDialog] 多类型 LoadMaterials 完成，物料数={_materials.Count}");

                StateChanged += MaterialSelectorDialog_StateChanged;

                WindowPositionService.AddPositionProtection(this);
                LogService.Debug("[MaterialSelectorDialog] 多类型构造函数完成");
            }
            catch (Exception ex)
            {
                LogService.Error("[MaterialSelectorDialog] 多类型构造函数异常", ex);
                throw;
            }
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
            var allMaterials = new List<FactoryMaterial>();
            foreach (var type in _materialTypes)
            {
                var materials = _dbService.GetFactoryMaterialsByType(type);
                allMaterials.AddRange(materials);
            }
            _materials = new ObservableCollection<FactoryMaterial>(allMaterials);
            MaterialsDataGrid.ItemsSource = _materials;

            // 预选已有的物料
            if (_preselectedMaterials != null && _preselectedMaterials.Count > 0)
            {
                LogService.Debug($"[MaterialSelectorDialog] 预选物料数: {_preselectedMaterials.Count}");
                foreach (var preselected in _preselectedMaterials)
                {
                    LogService.Debug($"[MaterialSelectorDialog] 预选项: FactoryMaterialId={preselected.FactoryMaterialId}, MaterialName={preselected.MaterialName}");
                }
                foreach (var mat in _materials)
                {
                    var matched = _preselectedMaterials.FirstOrDefault(p =>
                        p.FactoryMaterialId > 0 && p.FactoryMaterialId == mat.Id);
                    if (matched != null)
                    {
                        mat.IsSelected = true;
                        // 同步数量
                        mat.QuantityDisplay = matched.Quantity.ToString();
                        LogService.Debug($"[MaterialSelectorDialog] 匹配成功: mat.Id={mat.Id}, MaterialName={mat.MaterialName}");
                    }
                }
            }

            LogService.Debug($"[MaterialSelectorDialog] 加载了 {_materials.Count} 个物料");
        }

        private void MaterialsDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            while (dep != null && dep is not DataGridRow)
                dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
            if (dep is DataGridRow row && row.DataContext is FactoryMaterial material)
            {
                if (AllowMultipleSelection)
                {
                    // 多选模式：切换当前项的选择状态
                    material.IsSelected = !material.IsSelected;
                    LogService.Debug($"[MaterialSelectorDialog] 多选切换: {material.MaterialName}, IsSelected={material.IsSelected}");
                }
                else
                {
                    // 单选模式：先取消所有选择，再选中当前项
                    foreach (var m in _materials)
                    {
                        m.IsSelected = false;
                    }
                    material.IsSelected = true;
                    LogService.Debug($"[MaterialSelectorDialog] 单选切换: {material.MaterialName}");
                }
                UpdateOkButtonState();
            }
        }

        private void MaterialsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MaterialsDataGrid.SelectedItem is FactoryMaterial material)
            {
                if (AllowMultipleSelection)
                {
                    // 多选模式：切换选择状态
                    material.IsSelected = !material.IsSelected;
                    LogService.Debug($"[MaterialSelectorDialog] 多选双击切换: {material.MaterialName}, IsSelected={material.IsSelected}");
                }
                else
                {
                    // 单选模式：选中并关闭
                    material.IsSelected = true;
                }
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
                    var imageViewer = new ImageViewerWindow(material.ImageUrl, "物料图片");
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

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                var material = textBox.DataContext as FactoryMaterial;
                if (material == null) return;

                bool allowDecimal = material.QuantityDecimalPlaces > 0;
                bool isDecimalPoint = e.Text == ".";
                bool isDigit = IsDigit(textBox.Text, e.Text);

                if (!isDigit && !isDecimalPoint)
                {
                    e.Handled = true;
                    return;
                }

                if (isDecimalPoint && !allowDecimal)
                {
                    e.Handled = true;
                    return;
                }

                if (isDecimalPoint && textBox.Text.Contains("."))
                {
                    e.Handled = true;
                    return;
                }

                if (allowDecimal && textBox.Text.Contains("."))
                {
                    int dotIndex = textBox.Text.IndexOf('.');
                    int caretIndex = textBox.CaretIndex;
                    if (caretIndex > dotIndex)
                    {
                        int decimalPlaces = textBox.Text.Length - dotIndex - 1;
                        if (decimalPlaces >= material.QuantityDecimalPlaces)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
        }

        private bool IsDigit(string currentText, string inputText)
        {
            if (inputText.Length != 1) return false;
            char c = inputText[0];
            return c >= '0' && c <= '9';
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox &&
                textBox.DataContext is FactoryMaterial material)
            {
                string text = textBox.Text;

                // 防御：空文本或空白直接返回
                if (string.IsNullOrWhiteSpace(text))
                {
                    // 允许清空（还原为默认值 0）
                    if (material.Quantity != 0)
                    {
                        material.Quantity = 0;
                    }
                    return;
                }

                try
                {
                    // 记录当前数量值，避免重复更新
                    decimal currentQuantity = material.Quantity;

                    // 解析输入值
                    if (decimal.TryParse(text.Trim(), out decimal val))
                    {
                        // 负数不允许
                        if (val < 0) return;

                        // 根据单位计算小数位数
                        int decimals = material.QuantityDecimalPlaces;
                        decimal rounded = decimals == 0
                            ? System.Math.Round(val, 0)
                            : System.Math.Round(val, decimals);

                        // 只在值真正变化时更新
                        if (rounded != currentQuantity)
                        {
                            material.Quantity = rounded;
                        }
                    }
                    // 非数字输入会被 PreviewTextInput 阻止，这里只是容错
                }
                catch (Exception ex)
                {
                    LogService.Error($"[MaterialSelectorDialog] TextChanged 异常: {ex.Message}", ex);
                }
            }
        }

        // 图纸编号输入框获得焦点时
        private void DrawingNumberInDialogTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        // 图纸编号输入框失去焦点时（可以在这里添加验证逻辑）
        private void DrawingNumberInDialogTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // 暂时不需要特殊处理，Binding 已经更新了数据
        }
    }
}

