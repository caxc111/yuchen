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

        public MaterialGroupEditorDialog(MaterialGroup group, DbService dbService, string cabinetName)
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

            // 加载子项行
            foreach (var item in group.Items.OrderBy(i => i.ItemOrder))
            {
                var row = new MaterialGroupItemRow(item, this);
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
            if (sender is Button btn && btn.Tag is MaterialGroupItemRow row)
            {
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
                var dlg = new MaterialSelectorDialog(materialType, _dbService) { Owner = this };
                if (dlg.ShowDialog() == true && dlg.SelectedMaterials.Count > 0)
                {
                    // 替换该子项的已选（Multiple 模式追加；Single 替换）
                    if (row.Item.SelectionRule == SelectionRuleType.Multiple)
                    {
                        foreach (var m in dlg.SelectedMaterials) row.AddMaterial(m);
                    }
                    else
                    {
                        row.ClearMaterials();
                        foreach (var m in dlg.SelectedMaterials) row.AddMaterial(m);
                    }
                    NotifyItemChanged();
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
            if (!OkButton.IsEnabled) return;

            // 使用传入的柜子名称（如"玄关柜"）
            var cabinetName = CabinetName;
            LogService.Info($"[MaterialGroupEditorDialog] OkButton_Click: CabinetName={CabinetName}, Group.GroupName={Group.GroupName}");

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
            foreach (var row in ItemRows)
            {
                foreach (var m in row.SelectedMaterials)
                {
                    LogService.Info($"[MaterialGroupEditorDialog] 子项: ItemName={m.ItemName}, MaterialName={m.MaterialName}");
                    main.Children.Add(m);
                }
            }

            Result = main;
            DialogResult = true;
            Close();
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
            var sm = new SelectedMaterial
            {
                IsComposite = true,
                FactoryMaterialId = m.Id,
                MaterialName = m.MaterialName,
                Specification = m.Specification,
                UnitPrice = m.CostPrice ?? 0,
                Quantity = 1,
                FactoryMaterialCode = m.FactoryMaterialCode,
                MyMaterialCode = m.MyMaterialCode,
                Brand = m.Brand,
                Unit = m.Unit,
                ImageUrl = m.ImageUrl ?? "",
                // 从父对话框获取，用于组合 FullDisplayName
                CabinetName = _owner.CabinetName,
                ItemName = Item.ItemName,
                ComponentName = "固装",
                // ParentRef 用于子项归属主行
                ParentRef = 0
            };
            SelectedMaterials.Add(sm);
        }

        public void ClearMaterials() => SelectedMaterials.Clear();

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
                    if (string.IsNullOrWhiteSpace(m.Specification))
                        return $"{m.MaterialName} × ¥{m.UnitPrice:F2}";
                    return $"{m.MaterialName} ({m.Specification}) × ¥{m.UnitPrice:F2}";
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
