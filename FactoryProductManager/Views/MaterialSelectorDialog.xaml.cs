using FactoryProductManager.Helpers;
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
        private readonly DbService? _projectDbService;
        private readonly string _materialType;
        private readonly List<string> _materialTypes; // 支持多类型
        public List<FactoryMaterial> SelectedMaterials { get; private set; } = new();
        public IReadOnlyCollection<FactoryMaterial> AllMaterials => _materials;
        private ObservableCollection<FactoryMaterial> _materials = new();

        // 预选的物料列表
        private readonly List<SelectedMaterial>? _preselectedMaterials;

        // 是否允许多选（默认单选）
        public bool AllowMultipleSelection { get; set; } = false;

        // 项目代码（用于查询项目内已有的图纸编号）
        public string ProjectCode { get; set; } = "";

        // 产品ID（用于排除当前产品，仅在同一产品内检查唯一性）
        public int ProductId { get; set; } = 0;

        // 是否跳过图纸编号唯一性检查（子物料不需要检查）
        public bool SkipDrawingNumberCheck { get; set; } = false;

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
                _projectDbService = null;
                _preselectedMaterials = preselectedMaterials;
                AllowMultipleSelection = allowMultiple;

                TitleText.Text = $"选择{materialType}";
                LogService.Debug("[MaterialSelectorDialog] 开始 LoadMaterials");
                LoadMaterials();
                LogService.Debug($"[MaterialSelectorDialog] LoadMaterials 完成，物料数={_materials.Count}");

                StateChanged += MaterialSelectorDialog_StateChanged;

                WindowPositionService.AddPositionProtection(this);
                this.EnableTrayMinimize();
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
                _projectDbService = null;
                _preselectedMaterials = preselectedMaterials;
                AllowMultipleSelection = allowMultiple;

                TitleText.Text = $"选择{materialTypes[0]}";
                LogService.Debug($"[MaterialSelectorDialog] 多类型 LoadMaterials 开始，types={string.Join(",", materialTypes)}");
                LoadMaterials();
                LogService.Debug($"[MaterialSelectorDialog] 多类型 LoadMaterials 完成，物料数={_materials.Count}");

                StateChanged += MaterialSelectorDialog_StateChanged;

                WindowPositionService.AddPositionProtection(this);
                this.EnableTrayMinimize();
                LogService.Debug("[MaterialSelectorDialog] 多类型构造函数完成");
            }
            catch (Exception ex)
            {
                LogService.Error("[MaterialSelectorDialog] 多类型构造函数异常", ex);
                throw;
            }
        }

        // 新增：支持项目数据库和项目代码的构造函数
        public MaterialSelectorDialog(string materialType, DbService dbService, DbService projectDbService, string projectCode, List<SelectedMaterial>? preselectedMaterials = null, bool allowMultiple = false)
        {
            try
            {
                LogService.Debug($"[MaterialSelectorDialog] 项目构造函数开始, materialType={materialType}, projectCode={projectCode}");
                InitializeComponent();
                LogService.Debug("[MaterialSelectorDialog] 项目 InitializeComponent 完成");

                _materialType = materialType;
                _materialTypes = new List<string> { materialType };
                _dbService = dbService;
                _projectDbService = projectDbService;
                _preselectedMaterials = preselectedMaterials;
                AllowMultipleSelection = allowMultiple;
                ProjectCode = projectCode;

                TitleText.Text = $"选择{materialType}";
                LogService.Debug("[MaterialSelectorDialog] 项目 LoadMaterials 开始");
                LoadMaterials();
                LogService.Debug($"[MaterialSelectorDialog] 项目 LoadMaterials 完成，物料数={_materials.Count}");

                StateChanged += MaterialSelectorDialog_StateChanged;

                WindowPositionService.AddPositionProtection(this);
                this.EnableTrayMinimize();
                LogService.Debug("[MaterialSelectorDialog] 项目构造函数完成");
            }
            catch (Exception ex)
            {
                LogService.Error("[MaterialSelectorDialog] 项目构造函数异常", ex);
                throw;
            }
        }

        // 新增：支持项目数据库和项目代码的构造函数（多类型版本）
        public MaterialSelectorDialog(List<string> materialTypes, DbService dbService, DbService projectDbService, string projectCode, List<SelectedMaterial>? preselectedMaterials = null, bool allowMultiple = false)
        {
            try
            {
                LogService.Debug($"[MaterialSelectorDialog] 多类型项目构造函数开始, types={string.Join(",", materialTypes)}, projectCode={projectCode}");
                InitializeComponent();
                LogService.Debug("[MaterialSelectorDialog] 多类型项目 InitializeComponent 完成");

                _materialType = materialTypes.Count > 0 ? materialTypes[0] : "";
                _materialTypes = materialTypes;
                _dbService = dbService;
                _projectDbService = projectDbService;
                _preselectedMaterials = preselectedMaterials;
                AllowMultipleSelection = allowMultiple;
                ProjectCode = projectCode;

                TitleText.Text = $"选择{materialTypes[0]}";
                LogService.Debug("[MaterialSelectorDialog] 多类型项目 LoadMaterials 开始");
                LoadMaterials();
                LogService.Debug($"[MaterialSelectorDialog] 多类型项目 LoadMaterials 完成，物料数={_materials.Count}");

                StateChanged += MaterialSelectorDialog_StateChanged;

                WindowPositionService.AddPositionProtection(this);
                this.EnableTrayMinimize();
                LogService.Debug("[MaterialSelectorDialog] 多类型项目构造函数完成");
            }
            catch (Exception ex)
            {
                LogService.Error("[MaterialSelectorDialog] 多类型项目构造函数异常", ex);
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

            // 预选已有的物料（同时复制 DrawingNumber）
            if (_preselectedMaterials != null && _preselectedMaterials.Count > 0)
            {
                LogService.Debug($"[MaterialSelectorDialog] 预选物料数: {_preselectedMaterials.Count}");
                foreach (var preselected in _preselectedMaterials)
                {
                    LogService.Debug($"[MaterialSelectorDialog] 预选项: FactoryMaterialId={preselected.FactoryMaterialId}, MaterialName={preselected.MaterialName}, DrawingNumber={preselected.DrawingNumber}");
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
                        // 同步图纸编号（直接从预选数据获取）
                        mat.DrawingNumber = matched.DrawingNumber ?? "";

                        LogService.Debug($"[MaterialSelectorDialog] 匹配成功: mat.Id={mat.Id}, MaterialName={mat.MaterialName}, DrawingNumber={mat.DrawingNumber}");
                    }
                }
            }

            // 对所有物料查询图纸编号（有项目数据库时，用于新增物料场景）
            if (_projectDbService != null && !string.IsNullOrEmpty(ProjectCode))
            {
                foreach (var mat in _materials)
                {
                    // 只有未设置图纸编号时才查询
                    if (!string.IsNullOrEmpty(mat.DrawingNumber))
                        continue;
                    try
                    {
                        var drawingNumber = _projectDbService.GetDrawingNumberForMaterialInProject(ProjectCode, mat.Id);
                        if (!string.IsNullOrEmpty(drawingNumber))
                        {
                            mat.DrawingNumber = drawingNumber;
                            LogService.Debug($"[MaterialSelectorDialog] 从数据库加载图纸编号: mat.Id={mat.Id}, MaterialName={mat.MaterialName}, DrawingNumber={drawingNumber}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Error($"[MaterialSelectorDialog] 查询图纸编号失败: mat.Id={mat.Id}", ex);
                    }
                }
            }

            LogService.Debug($"[MaterialSelectorDialog] 加载了 {_materials.Count} 个物料");
        }

        private void MaterialsDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 查找点击的元素
            var dep = e.OriginalSource as DependencyObject;
            while (dep != null)
            {
                // 点击了 TextBox（数量列）→ 跳过，让数量框自己的 GotFocus 事件处理
                if (dep is System.Windows.Controls.TextBox)
                {
                    return;
                }
                // 点击了复选框列中的 Border → 切换选中
                if (dep is System.Windows.Controls.Border border)
                {
                    // 检查是否是复选框列的 Border
                    var parent = System.Windows.Media.VisualTreeHelper.GetParent(dep);
                    while (parent != null)
                    {
                        if (parent is DataGridCell cell)
                        {
                            // 复选框列是第 0 列
                            if (cell.Column?.DisplayIndex == 0)
                            {
                                var row = FindParent<DataGridRow>(cell);
                                if (row?.DataContext is FactoryMaterial material)
                                {
                                    if (AllowMultipleSelection)
                                    {
                                        material.IsSelected = !material.IsSelected;
                                    }
                                    else
                                    {
                                        if (material.IsSelected)
                                        {
                                            // 已选中 → 取消选中
                                            material.IsSelected = false;
                                        }
                                        else
                                        {
                                            // 未选中 → 取消所有 + 选中当前
                                            foreach (var m in _materials)
                                            {
                                                m.IsSelected = false;
                                            }
                                            material.IsSelected = true;
                                        }
                                    }
                                    UpdateOkButtonState();
                                    e.Handled = true;
                                    return;
                                }
                            }
                            break;
                        }
                        parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                    }
                }
                // 点击了物料名称列 → 切换选中
                if (dep is System.Windows.Controls.TextBlock)
                {
                    var parent = System.Windows.Media.VisualTreeHelper.GetParent(dep);
                    while (parent != null)
                    {
                        if (parent is DataGridCell cell)
                        {
                            // 物料名称列是第 1 列
                            if (cell.Column?.DisplayIndex == 1)
                            {
                                var row = FindParent<DataGridRow>(cell);
                                if (row?.DataContext is FactoryMaterial material)
                                {
                                    if (AllowMultipleSelection)
                                    {
                                        material.IsSelected = !material.IsSelected;
                                    }
                                    else
                                    {
                                        if (material.IsSelected)
                                        {
                                            // 已选中 → 取消选中
                                            material.IsSelected = false;
                                        }
                                        else
                                        {
                                            // 未选中 → 取消所有 + 选中当前
                                            foreach (var m in _materials)
                                            {
                                                m.IsSelected = false;
                                            }
                                            material.IsSelected = true;
                                        }
                                    }
                                    UpdateOkButtonState();
                                    e.Handled = true;
                                    return;
                                }
                            }
                            break;
                        }
                        parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                    }
                }
                dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // 弹出图纸编号输入框
        private void PromptForDrawingNumber(FactoryMaterial material)
        {
            // 优先使用物料已有的 DrawingNumber（从预选数据复制过来的）
            string existingDrawingNumber = material.DrawingNumber ?? "";

            // 如果没有已有的图纸编号，再尝试从数据库查询
            if (string.IsNullOrEmpty(existingDrawingNumber) && _projectDbService != null && !string.IsNullOrEmpty(ProjectCode))
            {
                try
                {
                    existingDrawingNumber = _projectDbService.GetDrawingNumberForMaterialInProject(ProjectCode, material.Id);
                    LogService.Debug($"[MaterialSelectorDialog] 从数据库查询项目图纸编号: materialId={material.Id}, drawingNumber={existingDrawingNumber}");
                }
                catch (Exception ex)
                {
                    LogService.Error($"[MaterialSelectorDialog] 查询图纸编号失败", ex);
                }
            }

            var inputDialog = new DrawingNumberInputDialog(existingDrawingNumber) { Owner = this };

            // 循环让用户输入，直到输入有效或取消
            while (inputDialog.ShowDialog() == true)
            {
                string drawingNumber = inputDialog.DrawingNumber;

                // 验证通过
                material.DrawingNumber = drawingNumber;
                LogService.Debug($"[MaterialSelectorDialog] 图纸编号已设置: {material.DrawingNumber}");

                // 设置图纸编号时自动勾选该物料（避免用户忘记勾选导致图纸编号无法保存）
                if (!material.IsSelected)
                {
                    material.IsSelected = true;
                    LogService.Debug($"[MaterialSelectorDialog] 自动勾选物料: Id={material.Id}, MaterialName={material.MaterialName}");
                    UpdateOkButtonState();
                }
                return;
            }

            // 用户取消，保持 DrawingNumber 不变
            LogService.Debug($"[MaterialSelectorDialog] 用户取消输入图纸编号，保持为: {material.DrawingNumber}");
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
                // 检查每个选中物料是否有图纸编号
                var missingDrawingNumbers = SelectedMaterials
                    .Where(m => string.IsNullOrEmpty(m.DrawingNumber))
                    .ToList();

                if (missingDrawingNumbers.Count > 0)
                {
                    var inputDialog = new DrawingNumberInputDialog()
                    {
                        Owner = this
                    };

                    if (inputDialog.ShowDialog() == true && !string.IsNullOrEmpty(inputDialog.DrawingNumber))
                    {
                        // 先检查图纸编号是否重复
                        missingDrawingNumbers.First().DrawingNumber = inputDialog.DrawingNumber;
                        var stillMissing = SelectedMaterials.Where(m => string.IsNullOrEmpty(m.DrawingNumber)).ToList();
                        if (stillMissing.Count > 0)
                        {
                            MessageBox.Show($"还有 {stillMissing.Count} 个物料缺少图纸编号，请先设置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else
                    {
                        return; // 用户取消或未输入
                    }
                }

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

        // 数量输入框获得焦点时：弹出图纸编号输入框
        private void QuantityTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // 用延迟确保焦点真的在 TextBox 上
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (sender is System.Windows.Controls.TextBox textBox &&
                    textBox.DataContext is FactoryMaterial material)
                {
                    // 再次检查焦点是否还在这个 TextBox 上
                    if (!textBox.IsFocused) return;

                    LogService.Debug($"[MaterialSelectorDialog] 数量框GotFocus: material={material.MaterialName}, IsSelected={material.IsSelected}, DrawingNumber={(material.DrawingNumber ?? "null")}");

                    // 弹出图纸编号输入框（不需要先选中物料）
                    if (string.IsNullOrEmpty(material.DrawingNumber))
                    {
                        PromptForDrawingNumber(material);
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // 图纸编号输入框获得焦点时
        private void DrawingNumberInDialogTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        // 图纸编号输入框失去焦点时
        private void DrawingNumberInDialogTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // 同项目内图纸编号可通用，不再检查唯一性
        }
    }
}

