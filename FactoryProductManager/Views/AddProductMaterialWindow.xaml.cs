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
            ["主卧室"] = new List<string> { "地面", "固装", "灯具", "房门" },
            ["主卫生间"] = new List<string> { "地面", "墙面", "固装", "五金洁具", "灯具", "房门" },
            ["次卧室"] = new List<string> { "地面", "固装", "灯具", "房门" },
            ["次卫生间"] = new List<string> { "地面", "墙面", "固装", "五金洁具", "灯具", "房门" },
            ["洗衣房"] = new List<string> { "地面", "固装", "五金洁具", "灯具", "房门" },
            ["书房"]   = new List<string> { "地面", "固装", "灯具", "房门" },
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
                new MaterialType { Name = "水龙头", Unit = "元/只", DefaultPrice = 350 }
            },
            ["洗衣房-灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 }
            },
            ["洗衣房-房门"] = new List<MaterialType>
            {
                new MaterialType { Name = "房门", Unit = "元/樘", DefaultPrice = 1500 }
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
                new MaterialType { Name = "水龙头", Unit = "元/个", DefaultPrice = 350 }
            },
            ["客餐厨-电器"] = new List<MaterialType>
            {
                new MaterialType { Name = "油烟机", Unit = "元/个", DefaultPrice = 3500 },
                new MaterialType { Name = "灶具", Unit = "元/个", DefaultPrice = 2000 },
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
            ["主卧室-房门"] = new List<MaterialType>
            {
                new MaterialType { Name = "房门", Unit = "元/樘", DefaultPrice = 1500 }
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
                new MaterialType { Name = "水龙头", Unit = "元/个", DefaultPrice = 350 },
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
            ["主卫生间-房门"] = new List<MaterialType>
            {
                new MaterialType { Name = "房门", Unit = "元/樘", DefaultPrice = 1500 }
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
            ["次卧室-房门"] = new List<MaterialType>
            {
                new MaterialType { Name = "房门", Unit = "元/樘", DefaultPrice = 1500 }
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
                new MaterialType { Name = "水龙头", Unit = "元/个", DefaultPrice = 350 },
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
            ["次卫生间-房门"] = new List<MaterialType>
            {
                new MaterialType { Name = "房门", Unit = "元/樘", DefaultPrice = 1500 }
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
            ["书房-房门"] = new List<MaterialType>
            {
                new MaterialType { Name = "房门", Unit = "元/樘", DefaultPrice = 1500 }
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

        // 选中和未选中颜色
        private static readonly Brush SelectedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAE3DA"));
        private static readonly Brush UnselectedBrush = Brushes.Transparent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddProductMaterialWindow(int productId, List<ProductPart>? parts = null)
        {
            try
            {
                LogService.Debug($"[AddProductMaterialWindow] 开始构造，productId={productId}, parts count={parts?.Count ?? 0}");
                InitializeComponent();
                LogService.Debug("[AddProductMaterialWindow] InitializeComponent 完成");
                DataContext = this;

                _productId = productId;

                _parts = parts ?? new List<ProductPart>
                {
                    new ProductPart { PartName = "门厅", Quantity = 1 }
                };

                // 加载数据库中保存的自定义部件，合并到 _partComponents
                LoadCustomPartsFromDatabase();

                // 种子数据：3 个柜类模板（首次启动时插入）
                try { new DbService().SeedDefaultMaterialGroups(); } catch { /* 已 seed 或失败，忽略 */ }

                SelectedMaterialsPanel.ItemsSource = SelectedMaterials;
                SelectedMaterials.CollectionChanged += (s, e) => UpdateEmptyText();

                InitializeNavPanel();
                UpdatePartContent();

                StateChanged += AddProductMaterialWindow_StateChanged;
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
                Style = (Style)FindResource("NavButtonStyle"),
                Tag = "__ADD_CUSTOM__",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7EFE2")),
                Margin = new Thickness(0, 8, 0, 0),
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
            // 创建导航按钮，与首页一致的样式
            var navBorder = new Border
            {
                Style = (Style)FindResource("NavButtonStyle"),
                Tag = partName,
                Background = UnselectedBrush
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
            // 与同页面的 SelectedMaterialItemStyle 保持一致：常态 0.8 细灰线框
            // 选中/未选中仅用背景色区分（未选中透明、选中 #FFEAE3DA 与 WarmHeaderBrush 同色）
            var defaultThickness = (Thickness)FindResource("UnifiedBorderThickness");
            foreach (var kvp in _navButtons)
            {
                bool isSelected = kvp.Key == SelectedPartName;
                kvp.Value.Background = isSelected ? SelectedBrush : UnselectedBrush;
                kvp.Value.BorderThickness = defaultThickness;
            }
        }

        private void UpdatePartContent()
        {
            TitleText.Text = $"{SelectedPartName} - 添加物料";
            PartItemsPanel.Children.Clear();
            _partItemRows.Clear();

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
            addButton.MouseLeftButtonUp += AddMaterial_Click;
            Grid.SetColumn(addButton, 2);
            grid.Children.Add(addButton);

            return grid;
        }

        private void MaterialTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 部品类型选择变化时的处理
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

                    // 在 _componentMaterials 找到当前选中项对应的 MaterialType，判断是否是复合物料
                    var materials = GetMaterialTypes(SelectedPartName, componentName);
                    var currentType = materials.FirstOrDefault(m => m.Name == selectedText);
                    if (currentType == null) return;

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
                            var dlg = new MaterialGroupEditorDialog(template, dbService) { Owner = this };
                            if (dlg.ShowDialog() == true && dlg.Result != null)
                            {
                                var main = dlg.Result;
                                main.PartName = SelectedPartName;
                                main.ComponentName = componentName;
                                // 强制刷新总价通知
                                SelectedMaterials.Add(main);
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
                    var selectorDialog = new MaterialSelectorDialog(selectedText, dbService)
                    {
                        Owner = this
                    };

                    if (selectorDialog.ShowDialog() == true && selectorDialog.SelectedMaterials.Count > 0)
                    {
                        foreach (var selectedDbMaterial in selectorDialog.SelectedMaterials)
                        {

                            var newMaterial = new SelectedMaterial
                            {
                                FactoryMaterialId = selectedDbMaterial.Id,
                                PartName = SelectedPartName,
                                MaterialName = $"{componentName}-{selectedDbMaterial.MaterialName}",
                                Specification = selectedDbMaterial.Specification,
                                UnitPrice = selectedDbMaterial.CostPrice ?? 0,
                                Quantity = 1,
                                ComponentName = componentName,
                                MaterialTypeName = selectedText,
                                FactoryMaterialCode = selectedDbMaterial.FactoryMaterialCode,
                                MyMaterialCode = selectedDbMaterial.MyMaterialCode,
                                Brand = selectedDbMaterial.Brand,
                                Unit = selectedDbMaterial.Unit,
                                ImageUrl = selectedDbMaterial.ImageUrl ?? ""
                            };

                            SelectedMaterials.Add(newMaterial);
                            UpdateEmptyText();
                        }
                    }
                }
            }
        }

        private void DeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SelectedMaterial material)
            {
                SelectedMaterials.Remove(material);
                UpdateEmptyText();
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
                    var viewer = new ImageViewerWindow(material.ImageUrl)
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

        private void UpdateEmptyText()
        {
            EmptyMaterialsText.Visibility = SelectedMaterials.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_productId > 0 && SelectedMaterials.Count > 0)
                {
                    var partMap = _parts.ToDictionary(p => p.PartName, p => p.Id);

                    // 展平 SelectedMaterials 为 ProductPartMaterial 列表（组合 → 主行+子行）
                    var flatMaterials = FlattenForSave(SelectedMaterials, partMap);

                    var dbService = new DbService();
                    dbService.DeleteProductPartMaterialsByProduct(_productId);
                    dbService.AddProductPartMaterials(_productId, flatMaterials);
                }
                else if (SelectedMaterials.Count > 0)
                {
                    LogService.Info($"[AddProductMaterialWindow] productId={_productId} 暂不落库（新建产品尚未持久化），SelectedMaterials.Count={SelectedMaterials.Count} 由调用方处理");
                }
            }
            catch (Exception ex)
            {
                LogService.Error("[AddProductMaterialWindow] 落库失败", ex);
                MessageBox.Show($"保存物料失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private List<ProductPartMaterial> FlattenForSave(
            IEnumerable<SelectedMaterial> materials,
            Dictionary<string, int> partMap)
        {
            var result = new List<ProductPartMaterial>();
            int seq = 0; // 内存里的临时 id 分配（用于子行指向主行）

            foreach (var sm in materials)
            {
                if (sm.IsComposite)
                {
                    // 主行
                    var mainId = ++seq; // 内存 id（落库时不占位）
                    var main = new ProductPartMaterial
                    {
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
                        Quantity = 1,
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

                    // 子行
                    foreach (var child in sm.Children)
                    {
                        result.Add(new ProductPartMaterial
                        {
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
                            ParentId = null,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                }
                else
                {
                    result.Add(new ProductPartMaterial
                    {
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
                }
            }

            return result;
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

    public class MaterialType
    {
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal DefaultPrice { get; set; }

        // 复合物料标记（方案 B）
        public bool IsComposite { get; set; }
        public string GroupCode { get; set; } = "";
    }

    public class SelectedMaterial : INotifyPropertyChanged
    {
        private int _quantity = 1;

        public int FactoryMaterialId { get; set; }
        public string PartName { get; set; } = "";
        public string MaterialName { get; set; } = "";
        public string Specification { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public string ComponentName { get; set; } = "";
        public string MaterialTypeName { get; set; } = "";
        public string FactoryMaterialCode { get; set; } = "";
        public string MyMaterialCode { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Unit { get; set; } = "";
        public string ImageUrl { get; set; } = "";

        // ===== 复合物料相关字段（方案 B） =====
        // 1=这是一个组合的主行（如"定制橱柜"），含 Children
        public bool IsComposite { get; set; }
        public string GroupCode { get; set; } = "";
        public string ItemName { get; set; } = "";  // 主行 = ""；子行 = "台面" 等
        public int? ParentRef { get; set; }        // 内存里的父子引用（落库前用，落库后用 ParentId）
        public ObservableCollection<SelectedMaterial> Children { get; } = new();

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                    _parentForNotify?.OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public SelectedMaterial()
        {
            Children.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (SelectedMaterial item in e.NewItems) item.AttachParent(this);
                if (e.OldItems != null)
                    foreach (SelectedMaterial item in e.OldItems) item.DetachParent(this);
                OnPropertyChanged(nameof(TotalPrice));
            };
        }

        private void AttachParent(SelectedMaterial parent) { ParentRef = null; _parentForNotify = parent; }
        private void DetachParent(SelectedMaterial parent) { _parentForNotify = null; }
        private SelectedMaterial? _parentForNotify;

        // 复合物料：显示总价 = 主行 0 + 子项之和；普通物料：单价 × 数量
        public decimal TotalPrice
        {
            get
            {
                if (IsComposite)
                {
                    decimal sum = 0;
                    foreach (var c in Children) sum += c.TotalPrice;
                    return sum;
                }
                return UnitPrice * Quantity;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
