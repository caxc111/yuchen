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
        public bool IsAdding { get; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public ProductManagementDialog(Product? product = null)
        {
            LogService.Debug("[ProductManagementDialog] 构造开始");
            InitializeComponent();
            LogService.Debug("[ProductManagementDialog] InitializeComponent 完成");
            if (product == null)
            {
                Product = new Product
                {
                    IsActive = true
                };
                Title = "添加产品";
                IsAdding = true;
            }
            else
            {
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
                Title = "编辑产品";
                IsAdding = false;
            }

            BusinessType = string.IsNullOrWhiteSpace(Product.BusinessType) ? BusinessTypeOptions[0] : Product.BusinessType;
            DataContext = this;

            // 加载已有的平面图
            if (!string.IsNullOrEmpty(Product.FloorPlan) && File.Exists(Product.FloorPlan))
            {
                SetFloorPlanPreviewImage(Product.FloorPlan);
            }

            if (product != null && product.Id > 0)
            {
                ContentRendered += ProductManagementDialog_ContentRendered;
            }
        }

        private void ProductManagementDialog_ContentRendered(object? sender, EventArgs e)
        {
            ContentRendered -= ProductManagementDialog_ContentRendered;
            LogService.Debug($"[ProductManagementDialog] ContentRendered, Product.Id={Product.Id}, PartsSummaryTextBox.IsLoaded={PartsSummaryTextBox.IsLoaded}");
            try
            {
                var existingParts = _dbService.GetProductParts(Product.Id);
                LogService.Debug($"[ProductManagementDialog] GetProductParts 返回 {existingParts.Count} 条");
                if (existingParts.Count > 0)
                {
                    var summary = string.Join("，", existingParts.Select(p => $"{p.PartName}*{p.Quantity}"));
                    PartsSummaryTextBox.Text = summary;

                    if (RequiresResidentialHouseType && string.IsNullOrWhiteSpace(Product.HouseType))
                    {
                        Product.HouseType = CalculateHouseType(existingParts);
                        OnPropertyChanged(nameof(Product));
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"[ProductManagementDialog] 加载部件失败: {ex.Message}");
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

            IsSaved = true;
            DialogResult = true;
            Close();
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

        private static string? GetProjectNameCode(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return "XX";
            }

            var trimmed = projectName.Trim();

            // 检测是否包含中文
            bool hasChinese = trimmed.Any(c => c >= 0x4E00 && c <= 0x9FFF);
            if (hasChinese)
            {
                return null; // 返回null表示有中文，需要弹窗提示
            }

            // 直接返回全部内容（只转大写）
            return trimmed.ToUpperInvariant();
        }

        private string BuildProductCode()
        {
            var businessTypeCode = GetBusinessTypeCode(BusinessType);
            if (string.IsNullOrWhiteSpace(businessTypeCode))
            {
                throw new InvalidOperationException("请先选择有效业态");
            }

            var projectCodeValue = GetProjectNameCode(Product.ProjectCode);
            if (projectCodeValue == null)
            {
                throw new InvalidOperationException("项目代码只能输入英文或数字，请重新输入");
            }

            var houseTypeCode = RequiresResidentialHouseType
                ? GetHouseTypeCode(Product.HouseType)
                : "NA";

            if (RequiresResidentialHouseType && string.IsNullOrWhiteSpace(houseTypeCode))
            {
                throw new InvalidOperationException("请先选择或输入户型");
            }

            // 新编码格式: {项目代号}-{业态码}-{户型码}-{流水号}
            // 例如: XN62-A-1R1B-001
            string codePrefix = $"{projectCodeValue}-{businessTypeCode}-{houseTypeCode}";
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

        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Debug("[ProductManagementDialog] AddMaterialButton_Click 开始");
            var selectedParts = new List<ProductPart>();
            if (_pendingParts != null)
            {
                selectedParts.AddRange(_pendingParts);
            }
            else if (Product.Id > 0)
            {
                try
                {
                    selectedParts = _dbService.GetProductParts(Product.Id);
                }
                catch { }
            }

            LogService.Debug($"[ProductManagementDialog] selectedParts count={selectedParts.Count}");
            if (selectedParts.Count == 0)
            {
                MessageBox.Show("请先选择部件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LogService.Debug("[ProductManagementDialog] 开始 new AddProductMaterialWindow");
            var dialog = new AddProductMaterialWindow(Product.Id, selectedParts);
            LogService.Debug("[ProductManagementDialog] AddProductMaterialWindow 构造完成，开始 ShowDialog");
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var materials = dialog.SelectedMaterials;
                if (Product.Id > 0)
                {
                    LogService.Info($"[ProductManagementDialog] 已为 productId={Product.Id} 写入 {materials.Count} 个物料");
                }
                else
                {
                    _pendingMaterials = materials.ToList();
                    MessageBox.Show($"已暂存 {materials.Count} 个物料（保存产品后落库）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
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

        private void EditPartsButton_Click(object sender, RoutedEventArgs e)
        {
            int productId = Product.Id > 0 ? Product.Id : 0;
            bool isNewProduct = Product.Id <= 0;

            // 把上次已选部件传回窗口，避免再次打开时被清空
            var partsDialog = new PartManagementDialog(productId, isNewProduct, _pendingParts);
            partsDialog.Owner = this;

            if (partsDialog.ShowDialog() == true)
            {
                if (isNewProduct)
                {
                    _pendingParts = new List<ProductPart>(partsDialog.Parts);
                    _pendingParts.AddRange(partsDialog.CustomParts);
                }
                else
                {
                    _pendingParts = partsDialog.Parts.Concat(partsDialog.CustomParts).ToList();
                }
                UpdatePartsSummary(partsDialog);
            }
        }

        private void UpdatePartsSummary(PartManagementDialog partsDialog)
        {
            var allParts = partsDialog.Parts.Concat(partsDialog.CustomParts).ToList();
            if (allParts.Count == 0)
            {
                PartsSummaryTextBox.Text = string.Empty;
                Product.HouseType = string.Empty;
                OnPropertyChanged(nameof(Product));
                return;
            }
            var summary = string.Join("，", allParts.Select(p => $"{p.PartName}*{p.Quantity}"));
            PartsSummaryTextBox.Text = summary;

            Product.HouseType = CalculateHouseType(allParts);
            OnPropertyChanged(nameof(Product));
        }

        private string CalculateHouseType(List<ProductPart> allParts)
        {
            int bedroomCount = allParts
                .Where(p => p.PartName == "主卧室" || p.PartName == "次卧室")
                .Sum(p => (int)p.Quantity);

            int bathroomCount = allParts
                .Where(p => p.PartName == "主卫生间" || p.PartName == "次卫生间")
                .Sum(p => (int)p.Quantity);

            return $"{bedroomCount}R{bathroomCount}B";
        }

        private List<ProductPart>? _pendingParts;
        public IReadOnlyList<ProductPart>? PendingParts => _pendingParts;

        private List<SelectedMaterial>? _pendingMaterials;
        public IReadOnlyList<SelectedMaterial>? PendingMaterials => _pendingMaterials;

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
    }
}
