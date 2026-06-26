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
        private readonly DbService _dbService = new();
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

        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"确定要删除产品「{Product.ProductCode}」吗？\n\n此操作将同时删除该产品的部件和物料数据，且无法恢复。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbService.DeleteProduct(Product.Id);
                    LogService.Info($"[EditProductDialog] 已删除产品，ProductId={Product.Id}");

                    IsDeleted = true;
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    LogService.Error("[EditProductDialog] 删除产品失败", ex);
                    MessageBox.Show($"删除产品失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
                OnPropertyChanged(nameof(HouseTypeDisplay));
                return;
            }

            var summary = string.Join("，", _currentParts.Select(p => $"{p.PartName}*{p.Quantity}"));
            PartsSummaryTextBox.Text = summary;
            Product.HouseType = CalculateHouseType(_currentParts);
            OnPropertyChanged(nameof(HouseTypeDisplay));
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
