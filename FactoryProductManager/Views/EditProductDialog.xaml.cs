using FactoryProductManager.Helpers;
using FactoryProductManager.Models;
using FactoryProductManager.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FactoryProductManager.Views
{
    public partial class EditProductDialog : Window, INotifyPropertyChanged
    {
        private readonly DbService _dbService = new DbService(DatabaseType.Project);
        private static readonly HashSet<string> ResidentialBusinessTypes = new(StringComparer.Ordinal)
        {
            "公寓",
            "House"
        };

        private string _businessType = string.Empty;

        public Product Product { get; set; }
        public IReadOnlyList<string> BusinessTypeOptions { get; } = new[] { "公寓", "House", "公区", "酒店", "商业" };
        public string BusinessType
        {
            get => _businessType;
            set
            {
                if (_businessType == value) return;
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
        public bool IsDeleted { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public decimal AveragePrice => Product.Area > 0 ? Product.CostTotalPrice / Product.Area : 0;

        public string HouseTypeDisplay
        {
            get => Product.HouseType;
            set
            {
                if (Product.HouseType != value)
                {
                    Product.HouseType = value;
                    OnPropertyChanged();
                }
            }
        }

        public EditProductDialog(Product product)
        {
            LogService.Debug("[EditProductDialog] 构造开始");
            InitializeComponent();
            LogService.Debug("[EditProductDialog] InitializeComponent 完成");

            Product = new Product
            {
                Id = product.Id,
                BusinessType = product.BusinessType,
                ProductCode = product.ProductCode,
                ProjectCode = product.ProjectCode,
                HouseType = product.HouseType,
                Area = product.Area,
                CostTotalPrice = product.CostTotalPrice,
                SellingTotalPrice = product.SellingTotalPrice,
                FloorPlan = product.FloorPlan,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            BusinessType = string.IsNullOrWhiteSpace(Product.BusinessType) ? BusinessTypeOptions[0] : Product.BusinessType;
            DataContext = this;

            // 加载已有的平面图
            if (!string.IsNullOrEmpty(Product.FloorPlan) && File.Exists(Product.FloorPlan))
            {
                SetFloorPlanPreviewImage(Product.FloorPlan);
            }

            // 窗口加载后加载部件和物料
            ContentRendered += EditProductDialog_ContentRendered;

            WindowPositionService.AddPositionProtection(this);
            this.EnableTrayMinimize();
            LogService.Debug("[EditProductDialog] 构造完成");
        }

        private void EditProductDialog_ContentRendered(object? sender, EventArgs e)
        {
            ContentRendered -= EditProductDialog_ContentRendered;
            LogService.Debug($"[EditProductDialog] ContentRendered, Product.Id={Product.Id}");

            try
            {
                // 加载部件
                var existingParts = _dbService.GetProductParts(Product.Id);
                if (existingParts.Count > 0)
                {
                    var summary = string.Join("，", existingParts.Select(p => $"{p.PartName}*{p.Quantity}"));
                    PartsSummaryTextBox.Text = summary;

                    if (RequiresResidentialHouseType && string.IsNullOrWhiteSpace(Product.HouseType))
                    {
                        Product.HouseType = CalculateHouseType(existingParts);
                        OnPropertyChanged(nameof(HouseTypeDisplay));
                    }
                }

                // 加载已有物料
                var existingMaterials = _dbService.LoadProductMaterialsFromLibrary(Product.Id);
                if (existingMaterials.Count > 0)
                {
                    _loadedMaterials = existingMaterials;
                    Product.CostTotalPrice = existingMaterials.Sum(m => m.TotalPrice);
                    OnPropertyChanged(nameof(Product));
                    OnPropertyChanged(nameof(AveragePrice));
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"[EditProductDialog] 加载数据失败: {ex.Message}");
            }
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

            try
            {
                _dbService.UpdateProduct(Product);
                LogService.Info($"[EditProductDialog] 已更新产品，ProductId={Product.Id}");

                IsSaved = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LogService.Error("[EditProductDialog] 更新产品失败", ex);
                MessageBox.Show($"更新产品失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditSequenceNumber_Click(object sender, RoutedEventArgs e)
        {
            string currentCode = Product.ProductCode;
            if (string.IsNullOrWhiteSpace(currentCode))
            {
                MessageBox.Show("当前产品编码无效", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parts = currentCode.Split('-');
            if (parts.Length < 4)
            {
                MessageBox.Show("产品编码格式不正确，无法编辑序号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string codePrefix = string.Join("-", parts.Take(3)); // 项目-业态-户型
            string currentSequence = parts[3]; // 当前序号

            string? newSequence = PromptForSequenceNumber(codePrefix, currentSequence);
            if (string.IsNullOrWhiteSpace(newSequence))
                return; // 用户取消

            string newCode = $"{codePrefix}-{newSequence}";

            // 检查编码是否重复（排除当前产品）
            if (_dbService.CheckProductCodeExistsForEdit(Product.Id, newCode))
            {
                MessageBox.Show($"产品编码「{newCode}」已存在，请重新输入序号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 更新产品编码
            try
            {
                _dbService.UpdateProductCode(Product.Id, newCode);
                Product.ProductCode = newCode;
                OnPropertyChanged(nameof(Product));
                LogService.Info($"[EditProductDialog] 已更新产品序号：{currentCode} -> {newCode}");
                MessageBox.Show($"产品编码已更新为「{newCode}」", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogService.Error("[EditProductDialog] 更新产品序号失败", ex);
                MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string? PromptForSequenceNumber(string codePrefix, string currentSequence)
        {
            string? result = null;
            var inputWindow = new Window
            {
                Title = "编辑户型序号",
                Width = 450,
                Height = 240,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label1 = new TextBlock
            {
                Text = $"当前序号：{currentSequence}",
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 13,
                Foreground = System.Windows.Media.Brushes.Gray
            };
            Grid.SetRow(label1, 0);

            var label2 = new TextBlock
            {
                Text = $"新的序号（将生成为：{codePrefix}-XX）：",
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 13
            };
            Grid.SetRow(label2, 1);

            var textBox = new TextBox
            {
                Height = 36,
                FontSize = 15,
                Padding = new Thickness(8, 4, 8, 4),
                VerticalContentAlignment = VerticalAlignment.Center,
                Text = currentSequence
            };
            textBox.PreviewTextInput += (s, args) =>
            {
                args.Handled = !System.Text.RegularExpressions.Regex.IsMatch(args.Text, @"^[a-zA-Z0-9]+$");
            };
            Grid.SetRow(textBox, 2);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var okButton = new Button
            {
                Content = "确定",
                Width = 80,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, args) =>
            {
                result = textBox.Text.Trim();
                inputWindow.DialogResult = true;
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 32,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 3);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.Children.Add(label1);
            grid.Children.Add(label2);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            inputWindow.Content = grid;

            textBox.Focus();
            textBox.SelectAll();

            if (inputWindow.ShowDialog() == true)
            {
                return result;
            }
            return null;
        }

        private void EditPartsButton_Click(object sender, RoutedEventArgs e)
        {
            int productId = Product.Id > 0 ? Product.Id : 0;

            var partsDialog = new PartManagementDialog(productId, false, _currentParts);
            partsDialog.Owner = this;

            if (partsDialog.ShowDialog() == true)
            {
                _currentParts = new List<ProductPart>(partsDialog.Parts);
                _currentParts.AddRange(partsDialog.CustomParts);

                // 保存部件到数据库（通过 UpdateProduct 方法）
                if (Product.Id > 0)
                {
                    _dbService.UpdateProduct(Product, _currentParts);
                    LogService.Info($"[EditProductDialog] 已保存 {_currentParts.Count} 个部件");
                }

                UpdatePartsSummary();
            }
        }

        private void UpdatePartsSummary()
        {
            if (_currentParts == null || _currentParts.Count == 0)
            {
                PartsSummaryTextBox.Text = string.Empty;
                Product.HouseType = string.Empty;
                Product.ProductCode = UpdateProductCodeHouseType(Product.ProductCode, string.Empty);
                OnPropertyChanged(nameof(Product));
                OnPropertyChanged(nameof(HouseTypeDisplay));
                return;
            }

            var summary = string.Join("，", _currentParts.Select(p => $"{p.PartName}*{p.Quantity}"));
            PartsSummaryTextBox.Text = summary;
            Product.HouseType = CalculateHouseType(_currentParts);

            // 户型变化后自动更新产品编码
            var newHouseTypeCode = GetHouseTypeCode(Product.HouseType);
            Product.ProductCode = UpdateProductCodeHouseType(Product.ProductCode, newHouseTypeCode);

            OnPropertyChanged(nameof(Product));
            OnPropertyChanged(nameof(HouseTypeDisplay));
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

        private static string UpdateProductCodeHouseType(string originalCode, string newHouseTypeCode)
        {
            if (string.IsNullOrEmpty(originalCode)) return originalCode;

            var parts = originalCode.Split('-');
            if (parts.Length >= 3)
            {
                // 格式: 项目-业态-户型-序号，直接替换第3段（户型部分）
                parts[2] = newHouseTypeCode;
                return string.Join("-", parts);
            }
            return originalCode;
        }

        private static string CalculateHouseType(List<ProductPart> allParts)
        {
            int bedroomCount = allParts
                .Where(p => p.PartName == "主卧室" || p.PartName == "次卧室")
                .Sum(p => (int)p.Quantity);

            int bathroomCount = allParts
                .Where(p => p.PartName == "主卫生间" || p.PartName == "次卫生间")
                .Sum(p => (int)p.Quantity);

            return $"{bedroomCount}R{bathroomCount}B";
        }

        private List<ProductPart>? _currentParts;
        private List<SelectedMaterial>? _loadedMaterials;

        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Debug("[EditProductDialog] AddMaterialButton_Click 开始");

            if (_currentParts == null || _currentParts.Count == 0)
            {
                var existingParts = _dbService.GetProductParts(Product.Id);
                if (existingParts.Count == 0)
                {
                    MessageBox.Show("请先选择部件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                _currentParts = existingParts;
            }

            var dialog = new AddProductMaterialWindow(Product.Id, Product.ProjectCode ?? "", _currentParts, _loadedMaterials);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                var materials = dialog.SelectedMaterials.ToList();

                try
                {
                    // 保存物料到数据库
                    _dbService.SaveProductMaterialsToLibrary(Product.Id, materials);
                    LogService.Info($"[EditProductDialog] 已保存 {materials.Count} 个物料");

                    // 重新计算成本合价
                    Product.CostTotalPrice = materials.Sum(m => m.TotalPrice);
                    OnPropertyChanged(nameof(Product));
                    OnPropertyChanged(nameof(AveragePrice));

                    _loadedMaterials = materials;
                }
                catch (Exception ex)
                {
                    LogService.Error("[EditProductDialog] 保存物料失败", ex);
                    MessageBox.Show($"保存物料失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ===== 平面图图片处理 =====
        private void FloorPlanDropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void FloorPlanDropZone_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;

            string filePath = files[0];
            if (IsValidImageFile(filePath))
            {
                LoadFloorPlanImage(filePath);
            }
            else
            {
                MessageBox.Show("请选择有效的图片文件（支持：jpg, jpeg, png, gif, bmp）");
            }
        }

        private void FloorPlanDropZone_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件 (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                Title = "选择平面图"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadFloorPlanImage(openFileDialog.FileName);
            }
        }

        private bool IsValidImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp";
        }

        private void LoadFloorPlanImage(string filePath)
        {
            try
            {
                string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (!Directory.Exists(imagesDir))
                {
                    Directory.CreateDirectory(imagesDir);
                }

                string fileName = Guid.NewGuid() + Path.GetExtension(filePath);
                string destPath = Path.Combine(imagesDir, fileName);

                File.Copy(filePath, destPath, true);
                SetFloorPlanPreviewImage(destPath);
                Product.FloorPlan = destPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图片加载失败: {ex.Message}");
            }
        }

        private void SetFloorPlanPreviewImage(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            FloorPlanPreviewImage.Source = bitmap;
            FloorPlanHintPanel.Visibility = Visibility.Collapsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
