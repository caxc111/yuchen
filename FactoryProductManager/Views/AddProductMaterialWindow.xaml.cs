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

namespace FactoryProductManager.Views
{
    public partial class AddProductMaterialWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<SelectedMaterial> SelectedMaterials { get; } = new();

        // 传入的部位列表
        private readonly List<ProductPart> _parts;

        // 当前选中的部位
        private string _selectedPartName = "门厅";
        public string SelectedPartName
        {
            get => _selectedPartName;
            set
            {
                _selectedPartName = value;
                OnPropertyChanged();
                UpdatePartContent();
                UpdateNavSelection();
            }
        }

        // 门厅部位的部品定义
        private readonly Dictionary<string, List<string>> _partComponents = new()
        {
            ["门厅"] = new List<string> { "地面", "固装", "灯具" }
        };

        // 部品对应的物料类型（从Excel中获取的数据）
        private readonly Dictionary<string, List<MaterialType>> _componentMaterials = new()
        {
            ["地面"] = new List<MaterialType>
            {
                new MaterialType { Name = "瓷砖", Unit = "元/片", DefaultPrice = 45 },
                new MaterialType { Name = "木地板", Unit = "元/m²", DefaultPrice = 300 },
                new MaterialType { Name = "石材", Unit = "元/m²", DefaultPrice = 600 },
                new MaterialType { Name = "地毯", Unit = "元/m²", DefaultPrice = 100 }
            },
            ["固装"] = new List<MaterialType>
            {
                new MaterialType { Name = "玄关柜", Unit = "元/套", DefaultPrice = 1680 },
                new MaterialType { Name = "鞋柜", Unit = "元/套", DefaultPrice = 1680 }
            },
            ["灯具"] = new List<MaterialType>
            {
                new MaterialType { Name = "筒灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "射灯", Unit = "元/盏", DefaultPrice = 78 },
                new MaterialType { Name = "灯带", Unit = "元/m", DefaultPrice = 8 }
            }
        };

        // 导航按钮字典
        private readonly Dictionary<string, Border> _navButtons = new();

        // 部品行字典
        private readonly Dictionary<string, Grid> _partItemRows = new();

        // 选中和未选中颜色
        private static readonly Brush SelectedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAE3DA"));
        private static readonly Brush UnselectedBrush = Brushes.Transparent;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddProductMaterialWindow(List<ProductPart>? parts = null)
        {
            try
            {
                LogService.Debug($"[AddProductMaterialWindow] 开始构造，parts count={parts?.Count ?? 0}");
                InitializeComponent();
                LogService.Debug("[AddProductMaterialWindow] InitializeComponent 完成");
                DataContext = this;

                _parts = parts ?? new List<ProductPart>
                {
                    new ProductPart { PartName = "门厅", Quantity = 1 }
                };

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

            foreach (var partName in _partComponents.Keys)
            {
                var navBorder = CreateNavButton(partName);

                if (partName == "门厅")
                {
                    navBorder.Background = SelectedBrush;
                }

                _navButtons[partName] = navBorder;
                NavPanel.Children.Add(navBorder);
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
            foreach (var kvp in _navButtons)
            {
                kvp.Value.Background = kvp.Key == SelectedPartName ? SelectedBrush : UnselectedBrush;
            }
        }

        private void UpdatePartContent()
        {
            TitleText.Text = $"{SelectedPartName} - 添加物料";
            PartItemsPanel.Children.Clear();
            _partItemRows.Clear();

            if (!_partComponents.TryGetValue(SelectedPartName, out var components))
                return;

            foreach (var componentName in components)
            {
                var row = CreateComponentRow(componentName);
                PartItemsPanel.Children.Add(row);
                _partItemRows[componentName] = row;
            }
        }

        private Grid CreateComponentRow(string componentName)
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

            if (_componentMaterials.TryGetValue(componentName, out var materials))
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
                Text = "+ 添加物料",
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
                    var selectedMaterialType = comboBox?.SelectedItem?.ToString();

                    if (!string.IsNullOrEmpty(selectedMaterialType))
                    {
                        var dbService = new DbService();
                        var selectorDialog = new MaterialSelectorDialog(selectedMaterialType, dbService)
                        {
                            Owner = this
                        };

                        if (selectorDialog.ShowDialog() == true && selectorDialog.SelectedMaterial != null)
                        {
                            var selectedDbMaterial = selectorDialog.SelectedMaterial;

                            var newMaterial = new SelectedMaterial
                            {
                                MaterialName = $"{componentName}-{selectedDbMaterial.MaterialName}",
                                Specification = selectedDbMaterial.Specification,
                                UnitPrice = selectedDbMaterial.CostPrice ?? 0,
                                Quantity = 1,
                                ComponentName = componentName,
                                MaterialTypeName = selectedMaterialType,
                                FactoryMaterialCode = selectedDbMaterial.FactoryMaterialCode,
                                Brand = selectedDbMaterial.Brand,
                                Unit = selectedDbMaterial.Unit
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

        private void UpdateEmptyText()
        {
            EmptyMaterialsText.Visibility = SelectedMaterials.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
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

    public class MaterialType
    {
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal DefaultPrice { get; set; }
    }

    public class SelectedMaterial : INotifyPropertyChanged
    {
        private int _quantity = 1;

        public string MaterialName { get; set; } = "";
        public string Specification { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public string ComponentName { get; set; } = "";
        public string MaterialTypeName { get; set; } = "";
        public string FactoryMaterialCode { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Unit { get; set; } = "";

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalPrice => UnitPrice * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(Quantity))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPrice)));
            }
        }
    }
}
