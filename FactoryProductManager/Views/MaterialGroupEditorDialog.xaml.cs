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
    public partial class MaterialGroupEditorDialog : Window, INotifyPropertyChanged
    {
        private readonly DbService _dbService;
        public MaterialGroup Group { get; }
        public string CabinetName { get; }  // 柜子名称（如"玄关柜"）

        // UI 绑定的子项行（含运行时状态：已选物料等）
        public ObservableCollection<MaterialGroupItemRow> ItemRows { get; } = new();

        // 返回值：调用方读取 Result，构造 SelectedMaterial
        public SelectedMaterial? Result { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MaterialGroupEditorDialog(MaterialGroup group, DbService dbService, string cabinetName, List<SelectedMaterial>? existingChildren = null)
        {
            InitializeComponent();
            DataContext = this;

            Group = group ?? throw new ArgumentNullException(nameof(group));
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            CabinetName = cabinetName ?? throw new ArgumentNullException(nameof(cabinetName));

            TitleText.Text = $"配置{group.GroupName}";
            SubtitleText.Text = string.IsNullOrWhiteSpace(group.Description)
                ? "请为每个子项选择具体物料（带 * 为必选项）"
                : group.Description;

            // 按 ItemName 分组已有的子项
            var existingByItemName = (existingChildren ?? new List<SelectedMaterial>())
                .GroupBy(c => c.ItemName)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 加载子项行
            foreach (var item in group.Items.OrderBy(i => i.ItemOrder))
            {
                var row = new MaterialGroupItemRow(item, this);

                // 如果有已选择的子项，预填充到行中
                if (existingByItemName.TryGetValue(item.ItemName, out var existingForItem))
                {
                    foreach (var child in existingForItem)
                    {
                        row.AddExistingMaterial(child);
                    }
                }

                ItemRows.Add(row);
            }
            ItemsPanel.ItemsSource = ItemRows;

            UpdateOkButton();
            UpdateTotalPrice();

            WindowPositionService.AddPositionProtection(this);
        }

        public void NotifyItemChanged()
        {
            UpdateOkButton();
            UpdateTotalPrice();
        }

        private void UpdateOkButton()
        {
            bool allOk = true;
            foreach (var r in ItemRows)
            {
                if (r.IsRequired && !r.HasSelection) { allOk = false; break; }
            }
            OkButton.IsEnabled = allOk;
        }

        private void UpdateTotalPrice()
        {
            decimal total = 0;
            foreach (var r in ItemRows)
            {
                foreach (var sel in r.SelectedMaterials)
                {
                    total += sel.UnitPrice * sel.Quantity;
                }
            }
            TotalPriceText.Text = $"¥{total:F2}";
        }

        private void SelectItemMaterial_Click(object sender, RoutedEventArgs e)
        {
            LogService.Debug($"[MaterialGroupEditorDialog] SelectItemMaterial_Click called");
            if (sender is Button btn && btn.Tag is MaterialGroupItemRow row)
            {
                LogService.Debug($"[MaterialGroupEditorDialog] SelectItemMaterial_Click: btn found, row.ItemName={row.Item.ItemName}");
                // 子项的物料类型可能是 "板材"、"石英石,大理石,岩板"
                // 为支持 "石英石" 这类"按 texture 过滤"，循环每种类型合并结果
                var types = (row.Item.MaterialType ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                if (types.Count == 0)
                {
                    MessageBox.Show("该子项未配置可选物料类型", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 多类型时，把所有类型的物料并集展示，但台面这种"3 选 1"应当单选
                if (types.Count > 1)
                {
                    // 弹一个简单的单选对话框：从多类型里选 1 个具体类型再走 MaterialSelectorDialog
                    var typePicker = new MaterialTypePickerDialog(types)
                    {
                        Owner = this
                    };
                    if (typePicker.ShowDialog() != true || string.IsNullOrEmpty(typePicker.SelectedType))
                    {
                        return;
                    }
                    OpenSelector(row, typePicker.SelectedType);
                }
                else
                {
                    OpenSelector(row, types[0]);
                }
            }
        }

        private void OpenSelector(MaterialGroupItemRow row, string materialType)
        {
            try
            {
                // 传入当前行已有的物料用于预选
                var preselectedList = row.SelectedMaterials.ToList();
                LogService.Debug($"[MaterialGroupEditorDialog] OpenSelector: materialType={materialType}, row.ItemName={row.Item.ItemName}, 已有物料数={preselectedList.Count}");
                foreach (var m in preselectedList)
                {
                    LogService.Debug($"[MaterialGroupEditorDialog] OpenSelector 预选项: FactoryMaterialId={m.FactoryMaterialId}, MaterialName={m.MaterialName}");
                }
                // 根据 SelectionRule 决定是否允许多选
                bool allowMultiple = row.Item.SelectionRule == SelectionRuleType.Multiple;
                var dlg = new MaterialSelectorDialog(materialType, _dbService, preselectedList, allowMultiple) { Owner = this };
                LogService.Debug($"[MaterialGroupEditorDialog] OpenSelector: dlg created, allowMultiple={allowMultiple}, ShowDialog about to be called");
                if (dlg.ShowDialog() == true && dlg.SelectedMaterials.Count > 0)
                {
                    LogService.Debug($"[MaterialGroupEditorDialog] OpenSelector: dlg returned true, selectedCount={dlg.SelectedMaterials.Count}");
                    // 先清空该子项的已选，再添加新选择（避免重复）
                    row.ClearMaterials();
                    foreach (var m in dlg.SelectedMaterials) row.AddMaterial(m);
                    NotifyItemChanged();
                }
                else
                {
                    LogService.Debug($"[MaterialGroupEditorDialog] OpenSelector: dlg returned false or no materials selected");
                }
            }
            catch (Exception ex)
            {
                LogService.Error("[MaterialGroupEditorDialog] 选择物料失败", ex);
                MessageBox.Show($"选择物料失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!OkButton.IsEnabled)
            {
                LogService.Warning("[MaterialGroupEditorDialog] OkButton_Click: OkButton未启用，直接返回");
                return;
            }

            // 使用传入的柜子名称（如"玄关柜"）
            var cabinetName = CabinetName;
            LogService.Info($"[MaterialGroupEditorDialog] OkButton_Click: CabinetName={CabinetName}, Group.GroupName={Group.GroupName}");
            LogService.Info($"[MaterialGroupEditorDialog] ItemRows数量={ItemRows.Count}");

            // 构造一个 SelectedMaterial 主行 + 子行集合
            var main = new SelectedMaterial
            {
                IsComposite = true,
                GroupCode = Group.GroupCode,
                MaterialName = cabinetName,  // 柜子名称
                PartName = "",  // 由调用方填
                ComponentName = "固装",
                CabinetName = cabinetName,
                MaterialTypeName = cabinetName,
                Quantity = 1
            };

            LogService.Info($"[MaterialGroupEditorDialog] 构造的main: MaterialName={main.MaterialName}");

            int totalChildren = 0;
            foreach (var row in ItemRows)
            {
                LogService.Info($"[MaterialGroupEditorDialog] 处理行: ItemName={row.Item.ItemName}, SelectedCount={row.SelectedMaterials.Count}");
                foreach (var m in row.SelectedMaterials)
                {
                    totalChildren++;
                    LogService.Info($"[MaterialGroupEditorDialog] 子项: ItemName={m.ItemName}, MaterialName={m.MaterialName}");
                    main.Children.Add(m);
                }
            }

            LogService.Info($"[MaterialGroupEditorDialog] OkButton_Click: 共收集 {totalChildren} 个子项，main.Children.Count={main.Children.Count}");

            Result = main;
            DialogResult = true;
            LogService.Info($"[MaterialGroupEditorDialog] OkButton_Click: 设置DialogResult=true，准备关闭");
            LogService.Info($"[MaterialGroupEditorDialog] OkButton_Click: 调用Close()之前");
            Close();
            LogService.Info($"[MaterialGroupEditorDialog] OkButton_Click: 窗口已关闭");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            LogService.Info($"[MaterialGroupEditorDialog] Window_Closing: 开始, DialogResult={DialogResult}");
            // 如果是通过 OkButton 或 CancelButton 关闭的，不需要再次处理
            if (DialogResult.HasValue) return;

            // 如果用户还没有选择任何物料就关闭，直接取消
            if (ItemRows.All(r => !r.HasSelection))
            {
                DialogResult = false;
                return;
            }

            // 如果有必选项未选，提示用户
            bool hasRequiredUnselected = ItemRows.Any(r => r.IsRequired && !r.HasSelection);
            if (hasRequiredUnselected)
            {
                var result = MessageBox.Show(
                    "有必选项未选择，是否退出？",
                    "提示",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
                else
                {
                    DialogResult = false;
                }
                return;
            }

            // 有选择但用户点击了X，自动保存并关闭
            // 由于无法在 Closing 中直接调用 Close，需要延迟执行
            e.Cancel = true; // 先取消关闭
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // 构造主物料
                    // 使用传入的柜子名称（如"玄关柜"）
                    var cabinetName = CabinetName;
                    var main = new SelectedMaterial
                    {
                        IsComposite = true,
                        MaterialName = cabinetName,
                        PartName = "固装",
                        ComponentName = "固装",
                        CabinetName = cabinetName,
                        MaterialTypeName = cabinetName,
                        Quantity = 1
                    };

                    foreach (var row in ItemRows)
                    {
                        foreach (var m in row.SelectedMaterials)
                        {
                            main.Children.Add(m);
                        }
                    }

                    Result = main;
                    DialogResult = true;
                    LogService.Info($"[MaterialGroupEditorDialog] Window_Closing: 自动保存完成，准备关闭");
                }
                catch (Exception ex)
                {
                    LogService.Error("[MaterialGroupEditorDialog] Window_Closing 自动保存失败", ex);
                    DialogResult = false;
                }
                // 使用反射调用 Close，因为此时已不在 Closing 事件中
                var closeMethod = typeof(Window).GetMethod("Close", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                closeMethod?.Invoke(this, null);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// 单个子项的 UI 行（含已选物料集合 + 计算属性）。
    /// </summary>
    public class MaterialGroupItemRow : INotifyPropertyChanged
    {
        public MaterialGroupItem Item { get; }
        public ObservableCollection<SelectedMaterial> SelectedMaterials { get; } = new();
        private readonly MaterialGroupEditorDialog _owner;

        public MaterialGroupItemRow(MaterialGroupItem item, MaterialGroupEditorDialog owner)
        {
            Item = item;
            _owner = owner;
            SelectedMaterials.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (SelectedMaterial sm in e.NewItems)
                    {
                        sm.PropertyChanged += OnChildPropChanged;
                        sm.ItemName = Item.ItemName;
                    }
                if (e.OldItems != null)
                    foreach (SelectedMaterial sm in e.OldItems)
                        sm.PropertyChanged -= OnChildPropChanged;
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(SelectedDisplay));
                OnPropertyChanged(nameof(StatusText));
                _owner.NotifyItemChanged();
            };
        }

        private void OnChildPropChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedMaterial.TotalPrice) || e.PropertyName == nameof(SelectedMaterial.Quantity))
            {
                _owner.NotifyItemChanged();
            }
        }

        public void AddMaterial(FactoryMaterial m)
        {
            LogService.Debug($"[MaterialGroupItemRow] AddMaterial: MaterialName={m.MaterialName}, Quantity={m.Quantity}");
            var sm = new SelectedMaterial
            {
                IsComposite = false,  // 子项是普通物料，不是复合物料
                FactoryMaterialId = m.Id,
                MaterialName = m.MaterialName,
                Specification = m.Specification,
                UnitPrice = m.CostPrice ?? 0,
                Quantity = m.Quantity,  // 使用用户在选择对话框中修改的数量
                FactoryMaterialCode = m.FactoryMaterialCode,
                MyMaterialCode = m.MyMaterialCode,
                Brand = m.Brand,
                Unit = m.Unit,
                ImageUrl = m.ImageUrl ?? "",
                // 从父对话框获取，用于组合 FullDisplayName
                CabinetName = _owner.CabinetName,
                ItemName = Item.ItemName,
                ComponentName = "固装",
                MaterialTypeName = Item.MaterialType,
                // ParentRef 用于子项归属主行
                ParentRef = 0
            };
            LogService.Debug($"[MaterialGroupItemRow] AddMaterial 创建: Quantity={sm.Quantity}");
            SelectedMaterials.Add(sm);
        }

        public void ClearMaterials() => SelectedMaterials.Clear();

        public void AddExistingMaterial(SelectedMaterial existing)
        {
            LogService.Debug($"[MaterialGroupItemRow] AddExistingMaterial: ItemName={Item.ItemName}, existing.FactoryMaterialId={existing.FactoryMaterialId}, MaterialName={existing.MaterialName}, Quantity={existing.Quantity}");
            var sm = new SelectedMaterial
            {
                IsComposite = false,  // 子项是普通物料，不是复合物料
                FactoryMaterialId = existing.FactoryMaterialId,
                MaterialName = existing.MaterialName,
                Specification = existing.Specification,
                UnitPrice = existing.UnitPrice,
                Quantity = existing.Quantity,
                FactoryMaterialCode = existing.FactoryMaterialCode,
                MyMaterialCode = existing.MyMaterialCode,
                Brand = existing.Brand,
                Unit = existing.Unit,
                ImageUrl = existing.ImageUrl ?? "",
                CabinetName = _owner.CabinetName,
                ItemName = Item.ItemName,
                ComponentName = existing.ComponentName,
                MaterialTypeName = existing.MaterialTypeName,
                ParentRef = existing.ParentRef
            };
            LogService.Debug($"[MaterialGroupItemRow] AddExistingMaterial 创建后: sm.Quantity={sm.Quantity}");
            SelectedMaterials.Add(sm);
        }

        public bool HasSelection => SelectedMaterials.Count > 0;

        public string SelectedDisplay
        {
            get
            {
                if (SelectedMaterials.Count == 0)
                {
                    return $"未选择（{Item.MaterialType}）";
                }
                return string.Join("；", SelectedMaterials.Select(m =>
                {
                    var spec = string.IsNullOrWhiteSpace(m.Specification) ? "" : $"({m.Specification})";
                    var subtotal = m.UnitPrice * m.Quantity;
                    // 显示实际数量，保留合理的小数位
                    var qtyDisplay = m.Quantity == Math.Floor(m.Quantity) ? $"{m.Quantity:F0}" : $"{m.Quantity:F2}";
                    return $"{m.MaterialName}{spec}：{qtyDisplay}×{m.UnitPrice:F2}={subtotal:F2}";
                }));
            }
        }

        public string StatusText
        {
            get
            {
                if (SelectedMaterials.Count == 0)
                {
                    return Item.IsRequired ? "必选" : "可选";
                }
                decimal sub = 0;
                foreach (var m in SelectedMaterials) sub += m.UnitPrice * m.Quantity;
                return $"小计 ¥{sub:F2}";
            }
        }

        public bool IsRequired => Item.IsRequired;
        public string RequiredStar => Item.IsRequired ? " *" : "";
        public string ItemName => Item.ItemName;
        public string Prompt => Item.Prompt;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
