using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FactoryProductManager.Views
{
    public partial class CustomPartEditorDialog : Window
    {
        private readonly DbService _dbService = new();
        public CustomPart? Result { get; private set; }

        // 7 个默认部品（去重自 _partComponents）
        private static readonly string[] DefaultComponents =
        {
            "地面", "墙面", "固装", "五金洁具", "电器", "灯具", "房门"
        };

        public CustomPartEditorDialog()
        {
            InitializeComponent();
            BuildComponentList();
        }

        private void BuildComponentList()
        {
            ComponentListPanel.Children.Clear();

            foreach (var name in DefaultComponents)
            {
                var row = CreateComponentRow(name, isCustom: false);
                ComponentListPanel.Children.Add(row);
            }
        }

        private UIElement CreateComponentRow(string name, bool isCustom)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var checkBox = new CheckBox
            {
                Content = name,
                IsChecked = false,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Tag = name
            };

            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            if (isCustom)
            {
                var delBtn = new Button
                {
                    Content = "删除",
                    Width = 56,
                    Height = 24,
                    FontSize = 11,
                    Margin = new Thickness(8, 0, 0, 0),
                    Style = (Style)FindResource("AddMaterialButtonStyle"),
                    Tag = name
                };
                delBtn.Click += DeleteCustomComponent_Click;
                Grid.SetColumn(delBtn, 2);
                grid.Children.Add(delBtn);
            }

            return grid;
        }

        private void AddCustomComponent_Click(object sender, RoutedEventArgs e)
        {
            var existing = ComponentListPanel.Children
                .OfType<Grid>()
                .SelectMany(g => g.Children.OfType<CheckBox>())
                .Select(cb => cb.Tag?.ToString() ?? string.Empty)
                .ToHashSet();

            string newName = "新部品" + (existing.Count(c => c.StartsWith("新部品")) + 1);
            var row = CreateComponentRow(newName, isCustom: true);
            ComponentListPanel.Children.Add(row);
        }

        private void DeleteCustomComponent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string name)
            {
                // 找到包含此 Tag 的 CheckBox 行
                foreach (var child in ComponentListPanel.Children.OfType<Grid>().ToList())
                {
                    var cb = child.Children.OfType<CheckBox>().FirstOrDefault();
                    if (cb != null && cb.Tag?.ToString() == name)
                    {
                        ComponentListPanel.Children.Remove(child);
                        break;
                    }
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var partName = PartNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(partName))
            {
                MessageBox.Show("请输入部件名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                PartNameTextBox.Focus();
                return;
            }

            // 默认部件重名校验
            var defaultPartNames = new[] { "门厅", "客餐厨", "主卧室", "主卫生间", "次卧室", "次卫生间", "洗衣房", "书房", "阳台" };
            if (defaultPartNames.Contains(partName))
            {
                MessageBox.Show($"\"{partName}\" 是默认部件，请直接使用默认按钮", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 数据库重名校验
            if (_dbService.CustomPartExists(partName))
            {
                MessageBox.Show($"自定义部件\"{partName}\"已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 收集选中的部品
            var selected = ComponentListPanel.Children
                .OfType<Grid>()
                .SelectMany(g => g.Children.OfType<CheckBox>())
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag?.ToString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();

            if (selected.Count == 0)
            {
                var result = MessageBox.Show("未选择任何部品，保存后该部件将不挂载物料。是否继续？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }

            try
            {
                var part = new CustomPart
                {
                    PartName = partName,
                    ComponentList = selected
                };

                _dbService.AddCustomPart(part);
                Result = part;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
