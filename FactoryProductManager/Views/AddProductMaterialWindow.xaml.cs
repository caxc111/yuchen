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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FactoryProductManager.Views
{
    public partial class AddProductMaterialWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<SelectedMaterial> SelectedMaterials { get; } = new();

        // 传入的部件列表
        private readonly List<ProductPart> _parts;

        // 关联的产品 id（>0 表示编辑已存在产品；<=0 表示新建产品暂存）
        private readonly int _productId;

        // 项目代码（用于图纸编号在项目内唯一性检查）
        private readonly string _projectCode;

        // 当前选中的部件；初值在 InitializeNavPanel 中按 _parts[0] 动态设置
        private string _selectedPartName = "";
        public string SelectedPartName
        {
            get => _selectedPartName;
            set
            {
                if (_selectedPartName == value) return;
                _selectedPartName = value;
                OnPropertyChanged();
                UpdatePartContent();
                UpdateNavSelection();
            }
        }

        // 部件 → 部品（一个部件下可挂多个部品）
        private readonly Dictionary<string, List<string>> _partComponents = new()
        {
            ["门厅"]   = new List<string> { "地面", "固装", "灯具" },
            ["客餐厨"] = new List<string> { "地面", "墙面", "固装", "五金洁具", "电器", "灯具" },
            ["主卧室"] = new List<string> { "地面", "固装", "灯具", "木门" },
            ["主卫生间"] = new List<string> { "地面", "墙面", "固装", "五金洁具", "灯具", "木门" },
            ["次卧室"] = new List<string> { "地面", "固装", "灯具", "木门" },
            ["次卫生间"] = new List<string> { "地面", "墙面", "固装", "五金洁具", "灯具", "木门" },
            ["洗衣房"] = new List<string> { "地面", "固装", "五金洁具", "灯具", "木门" },
            ["书房"]   = new List<string> { "地面", "固装", "灯具", "木门" },
            ["阳台"]   = new List<string> { "地面", "墙面", "灯具" }
        };

        // 从数据库加载自定义部件并合并到 _partComponents
        private void LoadCustomPartsFromDatabase()
        {
            try
            {
                var db = new DbService();
                var customParts = db.GetCustomParts();
                foreach (var cp in customParts)
                {
                    if (string.IsNullOrWhiteSpace(cp.PartName)) continue;
                    if (_partComponents.ContainsKey(cp.PartName)) continue; // 默认部件不覆盖
                    _partComponents[cp.PartName] = cp.ComponentList ?? new List<string>();
                }
                LogService.Info($"[AddProductMaterialWindow] 加载自定义部件 {customParts.Count} 个");
            }
            catch (Exception ex)
            {
                LogService.Error("[AddProductMaterialWindow] 加载自定义部件失败", ex);
            }
        }

        // 加载该产品已有的物料（从产品物料库加载）
        private void LoadExistingMaterials()
        {
            try
            {
                var db = new DbService();
                var existingMaterials = db.LoadProductMaterialsFromLibrary(_productId);

                // 直接使用返回的 SelectedMaterial 列表（已包含 Children 关系）
                foreach (var sm in existingMaterials)
                {
                    SelectedMaterials.Add(sm);
                }

                LogService.Info($"[AddProductMaterialWindow] 从产品物料库加载物料 {existingMaterials.Count} 条");
            }
            catch (Exception ex)
            {
                LogService.Error("[AddProductMaterialWindow] 从产品物料库加载物料失败", ex);
            }
        }

        // 部件 × 部品 → 物料类型
        // key 为 "{部件}-{部品}"，门厅数据保留为 fallback（即只用 "部品" 也能查到）
        private readonly Dictionary<string, List<MaterialType>> _componentMaterials = new()
        {
            ["门厅-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/片", DefaultPrice = 45 },
                new MaterialType { Name = "木地板", Unit = "元/m²", DefaultPrice = 300 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 },
                new MaterialType { Name = "地毯", Unit = "元/m²", DefaultPrice = 100 }
            },
            ["门厅-固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "玄关柜", Unit = "元/套", DefaultPrice = 1680, IsComposite = true, GroupCode = "GX-001" },
                new MaterialType { Name = "鞋柜", Unit = "元/套", DefaultPrice = 1680, IsComposite = true, GroupCode = "GX-001" }
            },
            ["门厅-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            },

            ["洗衣房-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/片", DefaultPrice = 45 },
                new MaterialType { Name = "木地板", Unit = "元/m²", DefaultPrice = 300 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["洗衣房-固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "洗衣机柜", Unit = "元/套", DefaultPrice = 2500, IsComposite = true, GroupCode = "GX-001" }
            },
            ["洗衣房-五金洁具"] = new List<MaterialType>
            {
                new MaterialType { Name = "台盆", Unit = "元/只", DefaultPrice = 600 },
                new MaterialType { Name = "龙头", Unit = "元/只", DefaultPrice = 350 }
            },
            ["洗衣房-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 }
            },
            ["洗衣房-木门"] = new List<MaterialType>
            {
                new MaterialType { Name = "木门", Unit = "元/樘", DefaultPrice = 1500 }
            },

            ["客餐厨-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/m²", DefaultPrice = 180 },
                new MaterialType { Name = "木地板", Unit = "元/m²", DefaultPrice = 300 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["客餐厨-墙面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/m²", DefaultPrice = 180 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["客餐厨-固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "定制橱柜", Unit = "元/套", DefaultPrice = 6000, IsComposite = true, GroupCode = "GX-001" },
                new MaterialType { Name = "洗衣柜", Unit = "元/套", DefaultPrice = 2500, IsComposite = true, GroupCode = "GX-001" },
                new MaterialType { Name = "餐厨中岛台", Unit = "元/m", DefaultPrice = 3500, IsComposite = true, GroupCode = "GX-001" },
                new MaterialType { Name = "电视柜", Unit = "元/m", DefaultPrice = 2000, IsComposite = true, GroupCode = "GX-001" }
            },
            ["客餐厨-五金洁具"] = new List<MaterialType>
            {
                new MaterialType { Name = "水槽", Unit = "元/个", DefaultPrice = 800 },
                new MaterialType { Name = "龙头", Unit = "元/个", DefaultPrice = 350 }
            },
            ["客餐厨-电器"] = new List<MaterialType>
            {
                new MaterialType { Name = "油烟机", Unit = "元/个", DefaultPrice = 3500 },
                new MaterialType { Name = "灶具", Unit = "元/个", DefaultPrice = 2000, Children = new List<MaterialType>
                {
                    new MaterialType { Name = "燃气灶", Unit = "元/个", DefaultPrice = 2000 },
                    new MaterialType { Name = "电磁灶", Unit = "元/个", DefaultPrice = 2000 }
                }},
                new MaterialType { Name = "洗碗机", Unit = "元/个", DefaultPrice = 4000 },
                new MaterialType { Name = "烤箱", Unit = "元/个", DefaultPrice = 3500 },
                new MaterialType { Name = "冰箱", Unit = "元/个", DefaultPrice = 8000 }
            },
            ["客餐厨-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            },

            ["主卧室-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "木地板", Unit = "元/m²", DefaultPrice = 300 },
                new MaterialType { Name = "地毯", Unit = "元/m²", DefaultPrice = 100 }
            },
            ["主卧室-固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "电视柜", Unit = "元/m", DefaultPrice = 2000, IsComposite = true, GroupCode = "GX-001" },
                new MaterialType { Name = "衣柜", Unit = "元/m²", DefaultPrice = 2500, IsComposite = true, GroupCode = "GX-001" }
            },
            ["主卧室-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            },
            ["主卧室-木门"] = new List<MaterialType>
            {
                new MaterialType { Name = "木门", Unit = "元/樘", DefaultPrice = 1500 }
            },

            ["主卫生间-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/m²", DefaultPrice = 180 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["主卫生间-墙面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/m²", DefaultPrice = 180 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["主卫生间-固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "浴室柜", Unit = "元/m", DefaultPrice = 2200, IsComposite = true, GroupCode = "GX-001" },
                new MaterialType { Name = "镜柜", Unit = "元/m²", DefaultPrice = 1800, IsComposite = true, GroupCode = "GX-001" }
            },
            ["主卫生间-五金洁具"] = new List<MaterialType>
            {
                new MaterialType { Name = "台盆", Unit = "元/个", DefaultPrice = 600 },
                new MaterialType { Name = "龙头", Unit = "元/个", DefaultPrice = 350 },
                new MaterialType { Name = "坐便器", Unit = "元/个", DefaultPrice = 1800 },
                new MaterialType { Name = "花洒", Unit = "元/个", DefaultPrice = 800 },
                new MaterialType { Name = "毛巾架", Unit = "元/个", DefaultPrice = 200 },
                new MaterialType { Name = "卫生纸架", Unit = "元/个", DefaultPrice = 100 },
                new MaterialType { Name = "浴缸", Unit = "元/个", DefaultPrice = 4500 },
                new MaterialType { Name = "浴巾杆", Unit = "元/个", DefaultPrice = 150 },
                new MaterialType { Name = "淋浴屏", Unit = "元/m²", DefaultPrice = 1200 }
            },
            ["主卫生间-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            },
            ["主卫生间-木门"] = new List<MaterialType>
            {
                new MaterialType { Name = "木门", Unit = "元/樘", DefaultPrice = 1500 }
            },

            ["次卧室-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "木地板", Unit = "元/m²", DefaultPrice = 300 },
                new MaterialType { Name = "地毯", Unit = "元/m²", DefaultPrice = 100 }
            },
            ["次卧室-固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "电视柜", Unit = "元/m", DefaultPrice = 2000, IsComposite = true, GroupCode = "GX-001" },
                new MaterialType { Name = "衣柜", Unit = "元/m²", DefaultPrice = 2500, IsComposite = true, GroupCode = "GX-001" }
            },
            ["次卧室-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            },
            ["次卧室-木门"] = new List<MaterialType>
            {
                new MaterialType { Name = "木门", Unit = "元/樘", DefaultPrice = 1500 }
            },

            ["次卫生间-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/m²", DefaultPrice = 180 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["次卫生间-墙面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/m²", DefaultPrice = 180 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["次卫生间-固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "浴室柜", Unit = "元/m", DefaultPrice = 2200, IsComposite = true, GroupCode = "GX-001" },
                new MaterialType { Name = "镜柜", Unit = "元/m²", DefaultPrice = 1800, IsComposite = true, GroupCode = "GX-001" }
            },
            ["次卫生间-五金洁具"] = new List<MaterialType>
            {
                new MaterialType { Name = "台盆", Unit = "元/个", DefaultPrice = 600 },
                new MaterialType { Name = "龙头", Unit = "元/个", DefaultPrice = 350 },
                new MaterialType { Name = "坐便器", Unit = "元/个", DefaultPrice = 1800 },
                new MaterialType { Name = "花洒", Unit = "元/个", DefaultPrice = 800 },
                new MaterialType { Name = "毛巾架", Unit = "元/个", DefaultPrice = 200 },
                new MaterialType { Name = "卫生纸架", Unit = "元/个", DefaultPrice = 100 },
                new MaterialType { Name = "浴巾杆", Unit = "元/个", DefaultPrice = 150 },
                new MaterialType { Name = "淋浴屏", Unit = "元/m²", DefaultPrice = 1200 }
            },
            ["次卫生间-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            },
            ["次卫生间-木门"] = new List<MaterialType>
            {
                new MaterialType { Name = "木门", Unit = "元/樘", DefaultPrice = 1500 }
            },

            ["书房-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "木地板", Unit = "元/m²", DefaultPrice = 300 },
                new MaterialType { Name = "地毯", Unit = "元/m²", DefaultPrice = 100 }
            },
            ["书房-固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "书柜", Unit = "元/m²", DefaultPrice = 2000, IsComposite = true, GroupCode = "GX-001" }
            },
            ["书房-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            },
            ["书房-木门"] = new List<MaterialType>
            {
                new MaterialType { Name = "木门", Unit = "元/樘", DefaultPrice = 1500 }
            },

            ["阳台-地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/m²", DefaultPrice = 180 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["阳台-墙面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/m²", DefaultPrice = 180 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 }
            },
            ["阳台-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            }
        };

        private List<MaterialType> GetMaterialTypes(string partName, string componentName)
        {
            string key = $"{partName}-{componentName}";
            if (_componentMaterials.TryGetValue(key, out var list))
                return list;
            // fallback：按部品名查（兼容门厅等历史 key）
            if (_componentMaterials.TryGetValue(componentName, out list))
                return list;
            return new List<MaterialType>();
        }

        // 导航按钮字典
        private readonly Dictionary<string, Border> _navButtons = new();

        // 部品行字典
        private readonly Dictionary<string, Grid> _partItemRows = new();

        // 图纸编号输入框字典
        private readonly Dictionary<string, List<TextBox>> _drawingNumberTextBoxes = new();

        // 选中和未选中颜色（选中色与下拉框一致 WarmInputBrush）
        // 注意：每个 Border 需要独立的 Brush 实例，不能共享
        private static readonly Color SelectedColor = (Color)ColorConverter.ConvertFromString("#FFFFFBF7");
        private static readonly Brush UnselectedBrush = Brushes.Transparent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddProductMaterialWindow(int productId, string projectCode, List<ProductPart>? parts = null, List<SelectedMaterial>? existingMaterials = null)
        {
            try
            {
                LogService.Debug($"[AddProductMaterialWindow] 开始构造，productId={productId}, projectCode={projectCode}, parts count={parts?.Count ?? 0}, existingMaterials count={existingMaterials?.Count ?? 0}");
                InitializeComponent();
                LogService.Debug("[AddProductMaterialWindow] InitializeComponent 完成");
                DataContext = this;

                _productId = productId;
                _projectCode = projectCode ?? "";

                _parts = parts ?? new List<ProductPart>
                {
                    new ProductPart { PartName = "门厅", Quantity = 1 }
                };

                // 加载数据库中保存的自定义部件，合并到 _partComponents
                LoadCustomPartsFromDatabase();

                // 种子数据：3 个柜类模板（首次启动时插入）
                try { new DbService().SeedDefaultMaterialGroups(); } catch { /* 已 seed 或失败，忽略 */ }

                // 加载已有物料（新建产品暂存的 or 编辑时数据库的）
                if (existingMaterials != null && existingMaterials.Count > 0)
                {
                    LogService.Debug($"[AddProductMaterialWindow] 添加传入的已有物料，count={existingMaterials.Count}");
                    foreach (var m in existingMaterials)
                    {
                        SelectedMaterials.Add(m);
                    }
                    LogService.Debug($"[AddProductMaterialWindow] 添加后 SelectedMaterials.Count={SelectedMaterials.Count}");
                }
                else if (_productId > 0)
                {
                    // 从数据库加载（编辑已有产品）
                    LoadExistingMaterials();
                }

                LogService.Debug($"[AddProductMaterialWindow] 构造完成，SelectedMaterials.Count={SelectedMaterials.Count}");
                SelectedMaterialsPanel.ItemsSource = SelectedMaterials;
                LogService.Debug($"[AddProductMaterialWindow] SelectedMaterialsPanel.ItemsSource 已设置");
                SelectedMaterials.CollectionChanged += (s, e) => UpdateEmptyText();

                InitializeNavPanel();
                UpdatePartContent();

                StateChanged += AddProductMaterialWindow_StateChanged;

                WindowPositionService.AddPositionProtection(this);
                LogService.Debug("[AddProductMaterialWindow] 构造完成");
            }
            catch (Exception ex)
            {
                LogService.Error("[AddProductMaterialWindow] 构造异常", ex);
                throw;
            }
        }

        private void AddProductMaterialWindow_StateChanged(object? sender, System.EventArgs e)
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

        private void InitializeNavPanel()
        {
            NavPanel.Children.Clear();
            _navButtons.Clear();

            // 只显示"编辑部件"中传入的 _parts 里有的部件；未选中的不显示
            // _partComponents 里同时有默认部件和自定义部件（已 LoadCustomPartsFromDatabase 合并）
            // 用 _parts 的 PartName 顺序作为导航栏顺序
            var selectedPartNames = _parts
                .Where(p => !string.IsNullOrWhiteSpace(p.PartName))
                .Select(p => p.PartName)
                .Distinct()
                .ToList();

            // 如果 _parts 为空（异常兜底），至少显示"门厅"避免空白
            if (selectedPartNames.Count == 0)
            {
                selectedPartNames.Add("门厅");
            }

            string? firstPart = null;
            foreach (var partName in selectedPartNames)
            {
                // 防御：如果 _partComponents 里没有（DB 加载失败的极端情况），补一个空清单
                if (!_partComponents.ContainsKey(partName))
                {
                    _partComponents[partName] = new List<string>();
                }

                var navBorder = CreateNavButton(partName);
                _navButtons[partName] = navBorder;
                NavPanel.Children.Add(navBorder);
                firstPart ??= partName;
            }

            // 默认选中第一个
            if (firstPart != null && SelectedPartName != firstPart)
            {
                SelectedPartName = firstPart;
            }
            else
            {
                // SelectedPartName 已对，先把按钮高亮刷一次
                UpdateNavSelection();
            }

            // 底部添加"➕ 自定义部件"按钮（仅在编辑部件里允许的额外添加，导航栏内临时使用）
            var addCustomBtn = CreateAddCustomPartButton();
            NavPanel.Children.Add(addCustomBtn);
        }

        private Border CreateAddCustomPartButton()
        {
            var border = new Border
            {
                Tag = "__ADD_CUSTOM__",
                Background = UnselectedBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(5, 8, 5, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderBrush = (Brush)FindResource("ActionBorderBrush"),
                BorderThickness = (Thickness)FindResource("UnifiedBorderThickness")
            };

            var contentStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };

            contentStack.Children.Add(new TextBlock
            {
                Text = "➕ 自定义部件",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            });

            border.Child = contentStack;
            border.MouseLeftButtonUp += AddCustomPart_Click;
            return border;
        }

        private void AddCustomPart_Click(object sender, MouseButtonEventArgs e)
        {
            var dlg = new CustomPartEditorDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true && dlg.Result != null)
            {
                var newPart = dlg.Result;
                if (_partComponents.ContainsKey(newPart.PartName))
                {
                    MessageBox.Show($"\"{newPart.PartName}\" 已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _partComponents[newPart.PartName] = newPart.ComponentList;

                // 在导航栏末尾追加新按钮
                var navBorder = CreateNavButton(newPart.PartName);
                _navButtons[newPart.PartName] = navBorder;

                // 插入到"➕ 自定义部件"按钮之前
                var addIndex = NavPanel.Children.Count; // 当前末尾是添加按钮
                NavPanel.Children.Insert(addIndex - 1, navBorder);

                // 自动选中新部件
                SelectedPartName = newPart.PartName;
            }
        }

        private Border CreateNavButton(string partName)
        {
            var navBorder = new Border
            {
                Tag = partName,
                Background = UnselectedBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(5, 2, 5, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderBrush = (Brush)FindResource("ActionBorderBrush"),
                BorderThickness = (Thickness)FindResource("UnifiedBorderThickness")
            };

            var contentStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };

            contentStack.Children.Add(new TextBlock
            {
                Text = partName,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            });

            navBorder.Child = contentStack;
            navBorder.MouseLeftButtonUp += NavBorder_Click;

            return navBorder;
        }

        private void NavBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string partName)
            {
                SelectedPartName = partName;
            }
        }

        private void UpdateNavSelection()
        {
            LogService.Debug($"[AddProductMaterialWindow] UpdateNavSelection called, SelectedPartName={SelectedPartName}");
            foreach (var kvp in _navButtons)
            {
                bool isSelected = kvp.Key == SelectedPartName;
                kvp.Value.Background = isSelected
                    ? new SolidColorBrush(SelectedColor)
                    : UnselectedBrush;
                LogService.Debug($"[AddProductMaterialWindow] NavButton {kvp.Key} set to {(isSelected ? "Selected" : "Unselected")}");
            }
        }

        private void UpdatePartContent()
        {
            TitleText.Text = $"{SelectedPartName} - 添加物料";
            PartItemsPanel.Children.Clear();
            _partItemRows.Clear();
            _drawingNumberTextBoxes.Clear();

            if (!_partComponents.TryGetValue(SelectedPartName, out var components))
                return;

            if (components.Count == 0)
            {
                PartItemsPanel.Children.Add(new TextBlock
                {
                    Text = $"该部件「{SelectedPartName}」暂未配置部品清单，请等待后续补充。",
                    FontSize = 13,
                    Foreground = (Brush)FindResource("HintTextBrush"),
                    Margin = new Thickness(0, 30, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            foreach (var componentName in components)
            {
                var row = CreateComponentRow(componentName, SelectedPartName);
                PartItemsPanel.Children.Add(row);
                _partItemRows[componentName] = row;
            }
        }

        private Grid CreateComponentRow(string componentName, string partName)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // 图纸编号列

            // 部品标签
            var labelBorder = new Border
            {
                Width = 70,
                Height = 28,
                Background = (Brush)FindResource("WarmSurfaceBrush"),
                BorderBrush = (Brush)FindResource("ActionBorderBrush"),
                BorderThickness = new Thickness(0.8),
                CornerRadius = new CornerRadius(6)
            };
            var labelGrid = new Grid { Width = 70, Height = 28 };
            labelGrid.Children.Add(new Border
            {
                Width = 70,
                Height = 28,
                CornerRadius = new CornerRadius(6),
                Background = (Brush)FindResource("WarmSurfaceBrush"),
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 15 }
            });
            labelGrid.Children.Add(new TextBlock
            {
                Text = componentName,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            });
            labelBorder.Child = labelGrid;
            Grid.SetColumn(labelBorder, 0);
            grid.Children.Add(labelBorder);

            // 物料类型下拉框
            var comboBox = new ComboBox
            {
                Style = (Style)FindResource("PartComboBoxStyle"),
                Tag = componentName
            };

            var materials = GetMaterialTypes(partName, componentName);
            if (materials.Count > 0)
            {
                foreach (var material in materials)
                {
                    comboBox.Items.Add(material.Name);
                }
                if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                }
            }

            comboBox.SelectionChanged += MaterialTypeComboBox_SelectionChanged;
            Grid.SetColumn(comboBox, 1);
            grid.Children.Add(comboBox);

            // 物料详情按钮
            var addButton = new Border
            {
                Width = 80,
                Height = 28,
                Background = (Brush)FindResource("WarmActionBrush"),
                BorderBrush = (Brush)FindResource("ActionBorderBrush"),
                BorderThickness = new Thickness(0.8),
                CornerRadius = new CornerRadius(6),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = componentName
            };
            var buttonGrid = new Grid { Width = 80, Height = 28 };
            buttonGrid.Children.Add(new Border
            {
                Width = 80,
                Height = 28,
                CornerRadius = new CornerRadius(6),
                Background = (Brush)FindResource("WarmActionBrush"),
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 15 }
            });
            buttonGrid.Children.Add(new TextBlock
            {
                Text = "添加物料",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            });
            addButton.Child = buttonGrid;
            addButton.PreviewMouseLeftButtonDown += AddMaterial_Click;
            Grid.SetColumn(addButton, 2);
            grid.Children.Add(addButton);

            // 图纸编号输入框（带占位符效果）
            var drawingNumberPanel = new Grid { Width = 110, Height = 28 };

            // 输入框
            var drawingNumberTextBox = new TextBox
            {
                Style = (Style)FindResource("DrawingNumberTextBoxStyle"),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = componentName,
                Name = $"DrawingNumberTextBox_{componentName}_{Guid.NewGuid():N}"
            };

            // 占位符 TextBlock（放在 TextBox 上层）
            var placeholderText = new TextBlock
            {
                Text = "请输入图纸编号",
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Foreground = (Brush)FindResource("HintTextBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false,
                Margin = new Thickness(6, 0, 6, 0)
            };
            Panel.SetZIndex(placeholderText, 1); // 确保在上层

            // 占位符逻辑
            drawingNumberTextBox.GotFocus += (s, e) =>
            {
                placeholderText.Visibility = Visibility.Collapsed;
                drawingNumberTextBox.FontWeight = FontWeights.Bold;
                drawingNumberTextBox.Foreground = (Brush)FindResource("PrimaryTextBrush");
            };

            drawingNumberTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrEmpty(drawingNumberTextBox.Text))
                {
                    placeholderText.Visibility = Visibility.Visible;
                    drawingNumberTextBox.FontWeight = FontWeights.Normal;
                }
            };

            // 监听 TextBox 文本变化，动态控制占位符显示，自动转大写
            drawingNumberTextBox.TextChanged += (s, e) =>
            {
                // 自动转换为大写
                var text = drawingNumberTextBox.Text;
                if (text != text.ToUpper())
                {
                    var caretIndex = drawingNumberTextBox.CaretIndex;
                    drawingNumberTextBox.Text = text.ToUpper();
                    drawingNumberTextBox.CaretIndex = caretIndex;
                }
                placeholderText.Visibility = string.IsNullOrEmpty(drawingNumberTextBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };

            // 先添加 TextBox，再添加占位符（占位符在上层）
            drawingNumberPanel.Children.Add(drawingNumberTextBox);
            drawingNumberPanel.Children.Add(placeholderText);
            Grid.SetColumn(drawingNumberPanel, 3);
            grid.Children.Add(drawingNumberPanel);

            // 保存图纸编号 TextBox 引用到字典，以便在 AddMaterial_Click 中获取值
            if (!_drawingNumberTextBoxes.ContainsKey(componentName))
            {
                _drawingNumberTextBoxes[componentName] = new List<TextBox>();
            }
            _drawingNumberTextBoxes[componentName].Add(drawingNumberTextBox);

            return grid;
        }

        private void MaterialTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 部品类型选择变化时，清除子类型标记
            if (sender is ComboBox comboBox && comboBox.Tag is string tag && tag.Contains("|"))
            {
                comboBox.Tag = tag.Split('|')[0];
            }
        }

        private void AddMaterial_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string componentName)
            {
                if (_partItemRows.TryGetValue(componentName, out var row))
                {
                    var comboBox = row.Children.OfType<ComboBox>().FirstOrDefault();
                    var selectedText = comboBox?.SelectedItem?.ToString();

                    if (string.IsNullOrEmpty(selectedText)) return;

                    // 获取当前部件的图纸编号（转大写）
                    var drawingNumber = "";
                    if (_drawingNumberTextBoxes.TryGetValue(componentName, out var textBoxes) && textBoxes.Count > 0)
                    {
                        drawingNumber = textBoxes[0].Text?.Trim().ToUpper() ?? "";
                    }

                    // 如果没有输入图纸编号，弹出对话框让用户输入
                    if (string.IsNullOrEmpty(drawingNumber))
                    {
                        var inputDialog = new DrawingNumberInputDialog { Owner = this };
                        if (inputDialog.ShowDialog() == true)
                        {
                            drawingNumber = inputDialog.DrawingNumber;

                            // 将输入的图纸编号填回到文本框（确保大写）
                            if (_drawingNumberTextBoxes.TryGetValue(componentName, out var boxes) && boxes.Count > 0)
                            {
                                boxes[0].Text = drawingNumber.ToUpper();
                            }
                        }
                        else
                        {
                            // 用户取消
                            return;
                        }
                    }

                    // 检查图纸编号是否已存在（项目内唯一）
                    if (!string.IsNullOrEmpty(drawingNumber))
                    {
                        bool shouldRemove = false;
                        SelectedMaterial? existingToRemove = null;
                        string existingMaterialName = "";
                        string existingPartName = "";
                        string existingComponentName = "";

                        if (!string.IsNullOrEmpty(_projectCode))
                        {
                            // 有项目代码：在整个项目内检查
                            var db = new DbService();
                            var existing = db.CheckDrawingNumberExistsInProject(_projectCode, drawingNumber, _productId > 0 ? _productId : null);
                            if (existing.HasValue && existing.Value.exists)
                            {
                                var (_, partName, compName) = existing.Value;
                                existingPartName = partName;
                                existingComponentName = compName;

                                var result = MessageBox.Show(
                                    $"图纸编号「{drawingNumber}」在本项目内已存在。\n\n该物料：{existingMaterialName}\n所在位置：{existingPartName} - {existingComponentName}\n\n是否覆盖原有物料？",
                                    "图纸编号重复",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);

                                if (result != MessageBoxResult.Yes)
                                {
                                    return; // 用户取消
                                }

                                // 删除项目中已存在的该图纸编号物料
                                existingToRemove = SelectedMaterials.FirstOrDefault(m => m.DrawingNumber == drawingNumber);
                                shouldRemove = true;
                            }
                        }
                        else
                        {
                            // 无项目代码：仅检查当前产品的 SelectedMaterials
                            existingToRemove = SelectedMaterials.FirstOrDefault(m =>
                                m.DrawingNumber == drawingNumber);
                            if (existingToRemove != null)
                            {
                                existingMaterialName = existingToRemove.MaterialName;
                                existingPartName = existingToRemove.PartName;
                                existingComponentName = existingToRemove.ComponentName;
                                shouldRemove = true;

                                var result = MessageBox.Show(
                                    $"图纸编号「{drawingNumber}」已存在。\n\n该物料：{existingMaterialName}\n所在位置：{existingPartName} - {existingComponentName}\n\n是否覆盖原有物料？",
                                    "图纸编号重复",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);

                                if (result != MessageBoxResult.Yes)
                                {
                                    return; // 用户取消
                                }
                            }
                        }

                        if (shouldRemove && existingToRemove != null)
                        {
                            SelectedMaterials.Remove(existingToRemove);
                        }
                    }

                    // 在 _componentMaterials 找到当前选中项对应的 MaterialType
                    var materials = GetMaterialTypes(SelectedPartName, componentName);
                    var currentType = materials.FirstOrDefault(m => m.Name == selectedText);
                    if (currentType == null) return;

                    // 判断是否有子类型，如果有则获取所有子类型名称列表
                    var materialTypeNames = new List<string> { selectedText };
                    if (currentType.Children?.Count > 0)
                    {
                        materialTypeNames = currentType.Children.Select(c => c.Name).ToList();
                    }

                    LogService.Info($"[监控] selectedText={selectedText}, materialTypeNames={string.Join(",", materialTypeNames)}, IsComposite={currentType.IsComposite}");

                    var dbService = new DbService();

                    if (currentType.IsComposite && !string.IsNullOrEmpty(currentType.GroupCode))
                    {
                        // 复合物料：弹 MaterialGroupEditorDialog
                        try
                        {
                            var template = dbService.GetMaterialGroupByCode(currentType.GroupCode);
                            if (template == null || template.Items.Count == 0)
                            {
                                MessageBox.Show($"未找到组合模板 {currentType.GroupCode}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            var dlg = new MaterialGroupEditorDialog(template, dbService, selectedText) { Owner = this };
                            if (dlg.ShowDialog() == true && dlg.Result != null)
                            {
                                var main = dlg.Result;
                                main.PartName = SelectedPartName;
                                main.ComponentName = componentName;
                                main.DrawingNumber = drawingNumber;

                                LogService.Info($"[监控] 复合物料添加成功: MaterialName={main.MaterialName}, FullDisplayName={main.FullDisplayName}, PartName={main.PartName}, ComponentName={main.ComponentName}");
                                LogService.Info($"[监控] 子项数量={main.Children.Count}");
                                foreach (var child in main.Children)
                                {
                                    LogService.Info($"[监控] 子项: ItemName={child.ItemName}, MaterialName={child.MaterialName}, FullDisplayName={child.FullDisplayName}");
                                }
                            // 强制刷新总价通知
                            SelectedMaterials.Add(main);
                            MarkAsUnsaved();  // 标记有未保存的更改
                            UpdateEmptyText();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogService.Error("[AddProductMaterialWindow] 弹复合物料对话框失败", ex);
                            MessageBox.Show($"配置复合物料失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }

                    // 普通物料：走老的 MaterialSelectorDialog
                    var selectorDialog = new MaterialSelectorDialog(materialTypeNames, dbService)
                    {
                        Owner = this
                    };

                    // 预标记当前部品类型下已选的物料
                    var existingMaterialIds = SelectedMaterials
                        .Where(m => m.PartName == SelectedPartName && m.ComponentName == componentName && !m.IsComposite && materialTypeNames.Contains(m.MaterialTypeName))
                        .Select(m => m.FactoryMaterialId)
                        .ToHashSet();

                    // 将对话框中匹配的物料设为选中状态
                    foreach (var material in selectorDialog.AllMaterials)
                    {
                        if (existingMaterialIds.Contains(material.Id))
                        {
                            material.IsSelected = true;
                        }
                    }

                    if (selectorDialog.ShowDialog() == true && selectorDialog.SelectedMaterials.Count > 0)
                    {
                        foreach (var selectedDbMaterial in selectorDialog.SelectedMaterials)
                        {
                            // 已有物料 → 更新信息（数量保留原值）
                            var existing = SelectedMaterials.FirstOrDefault(m =>
                                m.FactoryMaterialId == selectedDbMaterial.Id &&
                                m.PartName == SelectedPartName &&
                                m.ComponentName == componentName &&
                                !m.IsComposite && materialTypeNames.Contains(m.MaterialTypeName));
                            if (existing != null)
                            {
                                existing.MaterialName = selectedDbMaterial.MaterialName;
                                existing.Specification = selectedDbMaterial.Specification;
                                existing.UnitPrice = selectedDbMaterial.CostPrice ?? 0;
                                existing.FactoryMaterialCode = selectedDbMaterial.FactoryMaterialCode;
                                existing.MyMaterialCode = selectedDbMaterial.MyMaterialCode;
                                existing.Brand = selectedDbMaterial.Brand;
                                existing.Unit = selectedDbMaterial.Unit;
                                existing.ImageUrl = selectedDbMaterial.ImageUrl ?? "";
                                existing.DrawingNumber = drawingNumber;
                                MarkAsUnsaved();  // 标记有未保存的更改
                            }
                            else
                            {
                                var newMaterial = new SelectedMaterial
                                {
                                    FactoryMaterialId = selectedDbMaterial.Id,
                                    PartName = SelectedPartName,
                                    MaterialName = selectedDbMaterial.MaterialName,
                                    Specification = selectedDbMaterial.Specification,
                                    UnitPrice = selectedDbMaterial.CostPrice ?? 0,
                                    Quantity = selectedDbMaterial.Quantity,
                                    ComponentName = componentName,
                                    MaterialTypeName = selectedDbMaterial.MaterialName,
                                    FactoryMaterialCode = selectedDbMaterial.FactoryMaterialCode,
                                    MyMaterialCode = selectedDbMaterial.MyMaterialCode,
                                    Brand = selectedDbMaterial.Brand,
                                    Unit = selectedDbMaterial.Unit,
                                    ImageUrl = selectedDbMaterial.ImageUrl ?? "",
                                    DrawingNumber = drawingNumber
                                };

                                SelectedMaterials.Add(newMaterial);
                                MarkAsUnsaved();  // 标记有未保存的更改

                                // 添加成功后清空图纸编号文本框
                                ClearDrawingNumberTextBox(componentName);
                            }
                            UpdateEmptyText();
                        }
                    }
                }
            }
        }

        // 清空指定部件的图纸编号文本框
        private void ClearDrawingNumberTextBox(string componentName)
        {
            if (_drawingNumberTextBoxes.TryGetValue(componentName, out var textBoxes) && textBoxes.Count > 0)
            {
                textBoxes[0].Text = "";
                // 触发 LostFocus 以恢复占位符
                textBoxes[0].RaiseEvent(new RoutedEventArgs(Control.LostFocusEvent));
            }
        }

        private void DeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SelectedMaterial material)
            {
                var countBefore = SelectedMaterials.Count;
                LogService.Info($"[DeleteMaterial_Click] 删除前: MaterialName={material.MaterialName}, IsComposite={material.IsComposite}, Count={countBefore}");
                SelectedMaterials.Remove(material);
                MarkAsUnsaved();  // 标记有未保存的更改
                LogService.Info($"[DeleteMaterial_Click] 删除后: Count={SelectedMaterials.Count}");
                UpdateEmptyText();
            }
        }

        // 复合物料 Border 加载时：设置缩略图
        private void CompositeBorder_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is SelectedMaterial material)
            {
                var grid = VisualTreeHelper.GetChild(border, 0) as Grid;
                var image = grid?.FindName("CompositeThumb") as Image;
                if (image != null)
                {
                    var firstChild = material.Children.FirstOrDefault();
                    if (firstChild != null && !string.IsNullOrEmpty(firstChild.ImageUrl))
                    {
                        image.Source = CreateBitmapImage(firstChild.ImageUrl);
                        var textBlock = grid?.Children.OfType<TextBlock>().FirstOrDefault();
                        if (textBlock != null) textBlock.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        // 已选物料行的缩略图点击 → 大图查看
        private void Thumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is SelectedMaterial material
                && !string.IsNullOrWhiteSpace(material.ImageUrl))
            {
                try
                {
                    var viewer = new ImageViewerWindow(material.ImageUrl, "物料图片")
                    {
                        Owner = this
                    };
                    viewer.ShowDialog();
                }
                catch (Exception ex)
                {
                    LogService.Error("[AddProductMaterialWindow] 打开大图失败", ex);
                }
            }
        }

        // 复合物料缩略图点击 → 查看第一个子项的大图
        private void CompositeThumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is SelectedMaterial material)
            {
                var firstChild = material.Children.FirstOrDefault();
                if (firstChild != null && !string.IsNullOrWhiteSpace(firstChild.ImageUrl))
                {
                    try
                    {
                        var viewer = new ImageViewerWindow(firstChild.ImageUrl, "物料图片")
                        {
                            Owner = this
                        };
                        viewer.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("[AddProductMaterialWindow] 打开复合物料大图失败", ex);
                    }
                }
            }
        }

        // 编辑单个物料 - 重新打开物料选择对话框
        private void EditSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SelectedMaterial material)
            {
                try
                {
                    // 保存原始值（这些在编辑后要保留）
                    var originalFactoryMaterialId = material.FactoryMaterialId;
                    var originalQuantity = material.Quantity;
                    var originalPartName = material.PartName;
                    var originalComponentName = material.ComponentName;
                    var originalItemName = material.ItemName;
                    var originalSubGroupName = material.SubGroupName;
                    var originalCabinetName = material.CabinetName;
                    var originalId = material.Id;
                    var originalIsComposite = material.IsComposite;
                    var originalGroupCode = material.GroupCode;
                    var originalParentRef = material.ParentRef;

                    var dbService = new DbService();
                    var selectorDialog = new MaterialSelectorDialog(material.MaterialTypeName ?? "", dbService)
                    {
                        Owner = this
                    };

                    // 预标记当前物料为选中，并预填图纸编号
                    foreach (var m in selectorDialog.AllMaterials)
                    {
                        if (m.Id == originalFactoryMaterialId)
                        {
                            m.IsSelected = true;
                            m.DrawingNumber = material.DrawingNumber ?? "";
                        }
                    }

                    if (selectorDialog.ShowDialog() == true && selectorDialog.SelectedMaterials.Count > 0)
                    {
                        var selectedDbMaterial = selectorDialog.SelectedMaterials.First();

                        // 复制选择的物料的所有属性
                        material.MaterialName = selectedDbMaterial.MaterialName;
                        material.Specification = selectedDbMaterial.Specification;
                        material.UnitPrice = selectedDbMaterial.CostPrice ?? 0;
                        material.MaterialTypeName = selectedDbMaterial.Category;
                        material.FactoryMaterialCode = selectedDbMaterial.FactoryMaterialCode;
                        material.MyMaterialCode = selectedDbMaterial.MyMaterialCode;
                        material.Brand = selectedDbMaterial.Brand;
                        material.ImageUrl = selectedDbMaterial.ImageUrl;
                        material.Unit = selectedDbMaterial.Unit ?? "";
                        material.FactoryMaterialId = selectedDbMaterial.Id;
                        material.DrawingNumber = selectedDbMaterial.DrawingNumber ?? "";

                        // 恢复原始值（这些不应该被覆盖）
                        material.PartName = originalPartName;
                        material.ComponentName = originalComponentName;
                        material.ItemName = originalItemName;
                        material.SubGroupName = originalSubGroupName;
                        material.CabinetName = originalCabinetName;
                        material.Id = originalId;
                        material.IsComposite = originalIsComposite;
                        material.GroupCode = originalGroupCode;
                        material.ParentRef = originalParentRef;

                        // 关键：使用编辑弹窗中修改的数量，而不是原始数量
                        material.Quantity = selectedDbMaterial.Quantity;

                        LogService.Info($"[监控] 单个物料编辑成功: MaterialName={material.MaterialName}, Specification={material.Specification}");

                        // 立即保存到产品物料库（保持原有 ID）
                        if (_productId > 0)
                        {
                            var savedCount = dbService.SaveProductMaterialsToLibrary(_productId, SelectedMaterials.ToList());
                            LogService.Info($"[监控] 编辑后立即保存: productId={_productId}, 记录数={savedCount}");
                        }

                        // 触发 UI 刷新 - 通知 SelectedMaterials 集合已更改
                        var index = SelectedMaterials.IndexOf(material);
                        if (index >= 0)
                        {
                            // 通知单项属性已更新
                            material.OnPropertyChanged(nameof(material.MaterialName));
                            material.OnPropertyChanged(nameof(material.Specification));
                            material.OnPropertyChanged(nameof(material.UnitPrice));
                            material.OnPropertyChanged(nameof(material.Unit));
                            material.OnPropertyChanged(nameof(material.TotalPrice));
                            material.OnPropertyChanged(nameof(material.ImageUrl));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error("[AddProductMaterialWindow] 编辑单个物料失败", ex);
                    MessageBox.Show($"编辑物料失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 编辑复合物料
        private void EditComposite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SelectedMaterial composite)
            {
                try
                {
                    var dbService = new DbService();
                    var template = dbService.GetMaterialGroupByCode(composite.GroupCode ?? "");
                    if (template == null || template.Items.Count == 0)
                    {
                        MessageBox.Show($"未找到组合模板 {composite.GroupCode}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var dlg = new MaterialGroupEditorDialog(template, dbService, composite.MaterialName ?? "", composite.Children.ToList())
                    {
                        Owner = this
                    };

                    if (dlg.ShowDialog() == true && dlg.Result != null)
                    {
                        var main = dlg.Result;
                        main.PartName = composite.PartName;
                        main.ComponentName = composite.ComponentName;
                        main.Id = composite.Id;

                        LogService.Info($"[监控] 复合物料编辑成功: MaterialName={main.MaterialName}, FullDisplayName={main.FullDisplayName}");
                        LogService.Info($"[监控] 子项数量={main.Children.Count}");

                        // 从列表中移除旧的，添加新的
                        var index = SelectedMaterials.IndexOf(composite);
                        SelectedMaterials.Remove(composite);
                        SelectedMaterials.Add(main);

                        // 标记有未保存的更改
                        MarkAsUnsaved();

                        LogService.Info($"[监控] 替换完成，新子项数量={main.Children.Count}");
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error("[AddProductMaterialWindow] 编辑复合物料失败", ex);
                    MessageBox.Show($"编辑复合物料失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateEmptyText()
        {
            EmptyMaterialsText.Visibility = SelectedMaterials.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // 保存物料的核心逻辑（不关闭窗口）
        private bool SaveMaterialsCore()
        {
            try
            {
                LogService.Info($"[SaveMaterialsCore] 开始，SelectedMaterials.Count={SelectedMaterials.Count}, productId={_productId}");

                // 清除未保存标记
                _hasUnsavedChanges = false;

                if (_productId > 0)
                {
                    var dbService = new DbService();
                    var savedCount = dbService.SaveProductMaterialsToLibrary(_productId, SelectedMaterials.ToList());
                    LogService.Info($"[SaveMaterialsCore] 保存完成: productId={_productId}, 记录数={savedCount}");
                    return true;
                }
                else if (SelectedMaterials.Count > 0)
                {
                    LogService.Info($"[SaveMaterialsCore] productId={_productId} 暂不落库（新建产品尚未持久化），SelectedMaterials.Count={SelectedMaterials.Count}");
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogService.Error("[AddProductMaterialWindow] 落库失败", ex);
                LogService.Error($"[SaveMaterialsCore][FULL] Message={ex.Message}");
                LogService.Error($"[SaveMaterialsCore][FULL] StackTrace={ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    LogService.Error($"[SaveMaterialsCore][FULL] InnerException={ex.InnerException.Message}");
                    LogService.Error($"[SaveMaterialsCore][FULL] InnerStackTrace={ex.InnerException.StackTrace}");
                }
                MessageBox.Show($"保存物料失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveMaterialsCore()) return;

            DialogResult = true;
            Close();
        }

        private List<ProductPartMaterial> FlattenForSave(
            IEnumerable<SelectedMaterial> materials,
            Dictionary<string, int> partMap)
        {
            var result = new List<ProductPartMaterial>();

            foreach (var sm in materials)
            {
                try
                {
                if (sm.IsComposite)
                {
                    // 主行：保留数据库主键，否则为新增行
                    var main = new ProductPartMaterial
                    {
                        Id = sm.Id,
                        ProductId = _productId,
                        PartId = partMap.TryGetValue(sm.PartName, out var pid) && pid > 0 ? (int?)pid : null,
                        PartName = sm.PartName,
                        ComponentName = sm.ComponentName,
                        MaterialTypeName = sm.MaterialTypeName,
                        MaterialId = null,
                        MaterialName = sm.MaterialName,
                        FactoryMaterialCode = "",
                        MyMaterialCode = "",
                        Brand = "",
                        Specification = "",
                        Unit = "",
                        UnitPrice = 0,
                        Quantity = 1m,
                        IsComposite = true,
                        GroupCode = sm.GroupCode,
                        ItemName = "",
                        ParentId = null,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    // 主行的 TotalPrice = 子项总价之和（需要手动算，因为是计算属性）
                    main.TotalPrice = sm.TotalPrice;
                    result.Add(main);
                    LogService.Info($"[FlattenForSave] 主行: MaterialName={main.MaterialName}, Id={main.Id}, Children={sm.Children.Count}");

                    // 子行
                    foreach (var child in sm.Children)
                    {
                        result.Add(new ProductPartMaterial
                        {
                            Id = child.Id,
                            ProductId = _productId,
                            PartId = partMap.TryGetValue(sm.PartName, out var pid2) && pid2 > 0 ? (int?)pid2 : null,
                            PartName = sm.PartName,
                            ComponentName = sm.ComponentName,
                            MaterialTypeName = sm.MaterialTypeName,
                            MaterialId = child.FactoryMaterialId > 0 ? (int?)child.FactoryMaterialId : null,
                            MaterialName = child.MaterialName,
                            FactoryMaterialCode = child.FactoryMaterialCode,
                            MyMaterialCode = child.MyMaterialCode,
                            Brand = child.Brand,
                            Specification = child.Specification,
                            Unit = child.Unit,
                            UnitPrice = child.UnitPrice,
                            Quantity = child.Quantity,
                            IsComposite = false,
                            GroupCode = sm.GroupCode,
                            ItemName = child.ItemName,
                            ParentId = sm.Id > 0 ? (int?)sm.Id : sm.Id,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                        LogService.Info($"[FlattenForSave] 子行: ItemName={child.ItemName}, MaterialName={child.MaterialName}, Quantity={child.Quantity}");
                    }
                }
                else
                {
                    result.Add(new ProductPartMaterial
                    {
                        Id = sm.Id,
                        ProductId = _productId,
                        PartId = partMap.TryGetValue(sm.PartName, out var pid3) && pid3 > 0 ? (int?)pid3 : null,
                        PartName = sm.PartName,
                        ComponentName = sm.ComponentName,
                        MaterialTypeName = sm.MaterialTypeName,
                        MaterialId = sm.FactoryMaterialId > 0 ? (int?)sm.FactoryMaterialId : null,
                        MaterialName = sm.MaterialName,
                        FactoryMaterialCode = sm.FactoryMaterialCode,
                        MyMaterialCode = sm.MyMaterialCode,
                        Brand = sm.Brand,
                        Specification = sm.Specification,
                        Unit = sm.Unit,
                        UnitPrice = sm.UnitPrice,
                        Quantity = sm.Quantity,
                        IsComposite = false,
                        GroupCode = "",
                        ItemName = "",
                        ParentId = null,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                    LogService.Info($"[FlattenForSave] 普通行: MaterialName={sm.MaterialName}, Quantity={sm.Quantity}");
                }
                }
                catch (Exception ex)
                {
                    LogService.Error($"[FlattenForSave] 异常: MaterialName={sm.MaterialName}, Error={ex.Message}, Stack={ex.StackTrace}");
                }
            }

            return result;
        }

        private static string MakeSaveKey(ProductPartMaterial m)
        {
            return MakeSaveKeyWithParentId(m, m.ParentId);
        }

        private static string MakeSaveKeyWithParentId(ProductPartMaterial m, int? parentIdOverride)
        {
            return string.Join("|",
                m.IsComposite ? "1" : "0",
                parentIdOverride.HasValue && parentIdOverride > 0 ? parentIdOverride.Value.ToString() : "",
                m.MaterialId.HasValue && m.MaterialId > 0 ? m.MaterialId.Value.ToString() : "",
                m.PartName ?? "",
                m.ComponentName ?? "",
                m.ItemName ?? "",
                m.GroupCode ?? "",
                m.MaterialTypeName ?? "",
                m.MaterialName ?? "");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private BitmapImage? CreateBitmapImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return null;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 120;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 记录是否有未保存的更改
        private bool _hasUnsavedChanges = false;

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // 如果是通过 OkButton_Click 关闭的（已保存），不需要再次提示
            // 如果有未保存的更改且用户还没保存，提示用户
            if (_hasUnsavedChanges && DialogResult != true)
            {
                var result = MessageBox.Show(
                    "有物料已编辑但尚未保存。\n\n是否保存更改？",
                    "提示",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 点击"是"：执行保存
                    if (SaveMaterialsCore())
                    {
                        DialogResult = true;
                        // 正常关闭流程
                    }
                    else
                    {
                        // 保存失败，阻止关闭
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // 点击"取消"：阻止关闭
                    e.Cancel = true;
                }
                // 点击"否"：继续关闭，不保存
            }
        }

        // 在编辑物料后调用此方法标记有未保存更改
        private void MarkAsUnsaved()
        {
            _hasUnsavedChanges = true;
        }
    }

    public class MaterialType
    {
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal DefaultPrice { get; set; }
        public List<MaterialType> Children { get; set; } = new();

        // 复合物料标记（方案 B）
        public bool IsComposite { get; set; }
        public string GroupCode { get; set; } = "";
    }
}
