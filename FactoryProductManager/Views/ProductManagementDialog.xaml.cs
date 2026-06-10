using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FactoryProductManager.Views
{
    public partial class ProductManagementDialog : Window, INotifyPropertyChanged
    {
        private readonly DbService _dbService = new();
        private static readonly HashSet<string> ResidentialBusinessTypes = new(StringComparer.Ordinal)
        {
            "公寓",
            "House"
        };

        private string _businessType = string.Empty;

        public Product Product { get; set; }
        public IReadOnlyList<string> BusinessTypeOptions { get; } = new[] { "公寓", "House", "公区", "酒店", "商业" };
        public IReadOnlyList<string> ResidentialHouseTypeOptions { get; } = new[] { "一房一卫", "两房一卫", "两房两卫", "三房两卫", "四房三卫", "添加" };
        public string BusinessType
        {
            get => _businessType;
            set
            {
                if (_businessType == value)
                {
                    return;
                }

                _businessType = value;
                Product.BusinessType = value;

                if (!RequiresResidentialHouseType)
                {
                    Product.HouseType = string.Empty;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsResidential));
                OnPropertyChanged(nameof(RequiresResidentialHouseType));
            }
        }
        public bool IsResidential => RequiresResidentialHouseType;
        public bool RequiresResidentialHouseType => ResidentialBusinessTypes.Contains(BusinessType);
        public bool IsSaved { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public ProductManagementDialog(Product? product = null)
        {
            InitializeComponent();
            if (product == null)
            {
                Product = new Product
                {
                    IsActive = true
                };
                Title = "添加产品";
            }
            else
            {
                Product = new Product
                {
                    Id = product.Id,
                    BusinessType = product.BusinessType,
                    ProductCode = product.ProductCode,
                    HouseType = product.HouseType,
                    Area = product.Area,
                    CostTotalPrice = product.CostTotalPrice,
                    SellingTotalPrice = product.SellingTotalPrice,
                    FloorPlan = product.FloorPlan,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                };
                Title = "编辑产品";
            }

            BusinessType = string.IsNullOrWhiteSpace(Product.BusinessType) ? BusinessTypeOptions[0] : Product.BusinessType;
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Product.ProductCode))
            {
                MessageBox.Show("请输入产品编码");
                return;
            }

            if (RequiresResidentialHouseType && string.IsNullOrWhiteSpace(Product.HouseType))
            {
                MessageBox.Show("请输入或选择户型");
                return;
            }

            IsSaved = true;
            DialogResult = true;
            Close();
        }

        private void HouseTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedHouseType = ShowHouseTypeSelectionDialog();
            if (string.IsNullOrWhiteSpace(selectedHouseType))
            {
                return;
            }

            if (selectedHouseType == "添加")
            {
                var customHouseType = ShowCustomHouseTypeDialog();
                if (string.IsNullOrWhiteSpace(customHouseType))
                {
                    return;
                }

                Product.HouseType = customHouseType;
            }
            else
            {
                Product.HouseType = selectedHouseType;
            }

            OnPropertyChanged(nameof(Product));
        }

        private string? ShowHouseTypeSelectionDialog()
        {
            var dialog = new Window
            {
                Title = "选择户型",
                Owner = this,
                Width = 360,
                Height = 360,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                Background = Brushes.Transparent,
                AllowsTransparency = true,
                ShowInTaskbar = false
            };

            string? selectedHouseType = Product.HouseType;
            var optionButtons = new List<Button>();
            var optionsPanel = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Left,
                ItemWidth = 150
            };

            foreach (var houseType in ResidentialHouseTypeOptions)
            {
                var optionButton = CreateHouseTypeOptionButton(houseType, selectedHouseType);
                optionButton.Click += (_, _) =>
                {
                    selectedHouseType = houseType;
                    UpdateHouseTypeOptionStyles(optionButtons, selectedHouseType);
                };
                optionButtons.Add(optionButton);
                optionsPanel.Children.Add(optionButton);
            }

            var confirmButton = CreateDialogButton("确定", true, new Thickness(0, 0, 10, 0));
            var cancelButton = CreateDialogButton("取消", false, new Thickness(0));
            cancelButton.IsCancel = true;

            confirmButton.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(selectedHouseType))
                {
                    dialog.DialogResult = true;
                }
            };
            cancelButton.Click += (_, _) => dialog.Close();

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 0)
            };
            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            var contentPanel = new StackPanel();
            contentPanel.Children.Add(new TextBlock
            {
                Text = "请选择公寓 / House 户型",
                FontSize = 14,
                Foreground = (Brush)FindResource("PrimaryTextBrush"),
                Margin = new Thickness(0, 0, 0, 12)
            });
            contentPanel.Children.Add(optionsPanel);
            contentPanel.Children.Add(buttonPanel);

            dialog.Content = CreateDialogPanel(contentPanel, new Thickness(16));

            return dialog.ShowDialog() == true ? selectedHouseType : null;
        }

        private string? ShowCustomHouseTypeDialog()
        {
            var dialog = new Window
            {
                Title = "添加户型",
                Owner = this,
                Width = 360,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                Background = Brushes.Transparent,
                AllowsTransparency = true,
                ShowInTaskbar = false
            };

            var textBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 12),
                Height = 36,
                FontSize = 14,
                Text = Product.HouseType ?? string.Empty,
                Style = (Style)FindResource("WarmInputTextBoxStyle")
            };

            var confirmButton = CreateDialogButton("确定", true, new Thickness(0, 0, 10, 0));
            var cancelButton = CreateDialogButton("取消", false, new Thickness(0));
            cancelButton.IsCancel = true;

            confirmButton.Click += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    dialog.DialogResult = true;
                }
            };
            cancelButton.Click += (_, _) => dialog.Close();

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 0)
            };
            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            var contentPanel = new StackPanel();
            contentPanel.Children.Add(new TextBlock
            {
                Text = "请输入自定义户型名称",
                FontSize = 14,
                Foreground = (Brush)FindResource("PrimaryTextBrush"),
                Margin = new Thickness(0, 0, 0, 12)
            });
            contentPanel.Children.Add(textBox);
            contentPanel.Children.Add(buttonPanel);

            dialog.Content = CreateDialogPanel(contentPanel, new Thickness(16));

            return dialog.ShowDialog() == true ? textBox.Text.Trim() : null;
        }

        private static string GetBusinessTypeCode(string businessType)
        {
            return businessType switch
            {
                "公寓" => "A",
                "House" => "H",
                "公区" => "P",
                "酒店" => "HT",
                "商业" => "C",
                _ => string.Empty
            };
        }

        private static string GetHouseTypeCode(string houseType)
        {
            return houseType.Trim() switch
            {
                "一房一卫" => "1R1B",
                "两房一卫" => "2R1B",
                "两房两卫" => "2R2B",
                "三房两卫" => "3R2B",
                "四房三卫" => "4R3B",
                _ => houseType.Trim().ToUpperInvariant().Replace(" ", string.Empty)
            };
        }

        private string BuildProductCode()
        {
            var businessTypeCode = GetBusinessTypeCode(BusinessType);
            if (string.IsNullOrWhiteSpace(businessTypeCode))
            {
                throw new InvalidOperationException("请先选择有效业态");
            }

            var houseTypeCode = RequiresResidentialHouseType
                ? GetHouseTypeCode(Product.HouseType)
                : "NA";

            if (RequiresResidentialHouseType && string.IsNullOrWhiteSpace(houseTypeCode))
            {
                throw new InvalidOperationException("请先选择或输入户型");
            }

            string codePrefix = $"IH-{businessTypeCode}-{houseTypeCode}";
            int nextSequence = _dbService.GetNextProductCodeSequence(codePrefix, Product.Id > 0 ? Product.Id : null);
            return $"{codePrefix}-{nextSequence:D3}";
        }

        private void GenerateProductCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Product.ProductCode = BuildProductCode();
                OnPropertyChanged(nameof(Product));
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Border CreateDialogPanel(UIElement content, Thickness padding)
        {
            return new Border
            {
                Style = (Style)FindResource("WarmPanelBorderStyle"),
                Padding = padding,
                Child = content
            };
        }

        private Button CreateHouseTypeOptionButton(string houseType, string? selectedHouseType)
        {
            var optionButton = new Button
            {
                Content = houseType,
                Height = 42,
                Margin = new Thickness(0, 0, 10, 10),
                Padding = new Thickness(12, 0, 12, 0),
                Style = (Style)FindResource("UnifiedDialogActionButtonStyle")
            };

            ApplyHouseTypeOptionStyle(optionButton, houseType == selectedHouseType);
            return optionButton;
        }

        private void UpdateHouseTypeOptionStyles(IEnumerable<Button> optionButtons, string? selectedHouseType)
        {
            foreach (var optionButton in optionButtons)
            {
                var optionText = optionButton.Content?.ToString();
                ApplyHouseTypeOptionStyle(optionButton, optionText == selectedHouseType);
            }
        }

        private void ApplyHouseTypeOptionStyle(Button optionButton, bool isSelected)
        {
            optionButton.Foreground = (Brush)FindResource("PrimaryTextBrush");
            optionButton.Background = isSelected
                ? (Brush)FindResource("WarmButtonBrush")
                : Brushes.Transparent;
            optionButton.BorderBrush = (Brush)FindResource("ActionBorderBrush");
        }

        private Button CreateDialogButton(string content, bool isPrimary, Thickness margin)
        {
            return new Button
            {
                Content = content,
                Width = 80,
                Height = 34,
                Margin = margin,
                IsDefault = isPrimary,
                Style = (Style)FindResource("UnifiedDialogActionButtonStyle"),
                Foreground = (Brush)FindResource("PrimaryTextBrush")
            };
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            Close();
        }
    }
}
