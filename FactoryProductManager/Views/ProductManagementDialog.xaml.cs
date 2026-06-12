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

            // 只取前两个字符（英文或数字）
            string result = trimmed.Length >= 2 
                ? trimmed.Substring(0, 2).ToUpperInvariant() 
                : trimmed.ToUpperInvariant();

            // 提取数字
            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(digits))
            {
                result += digits;
            }

            return string.IsNullOrEmpty(result) ? "XX" : result;
        }

        private string BuildProductCode()
        {
            var businessTypeCode = GetBusinessTypeCode(BusinessType);
            if (string.IsNullOrWhiteSpace(businessTypeCode))
            {
                throw new InvalidOperationException("请先选择有效业态");
            }

            var projectCodeValue = GetProjectNameCode(Product.ProjectName);
            if (projectCodeValue == null)
            {
                throw new InvalidOperationException("项目代号只能输入英文或数字，请重新输入");
            }

            var houseTypeCode = RequiresResidentialHouseType
                ? GetHouseTypeCode(Product.HouseType)
                : "NA";

            if (RequiresResidentialHouseType && string.IsNullOrWhiteSpace(houseTypeCode))
            {
                throw new InvalidOperationException("请先选择或输入户型");
            }

            // 新编码格式: {项目代号}-{业态码}{户型码}-{流水号}
            // 例如: XN62-A1R1B-001
            string codePrefix = $"{projectCodeValue}-{businessTypeCode}{houseTypeCode}";
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

            var partsDialog = new PartManagementDialog(productId, isNewProduct);
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
                    _pendingParts = null;
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
    }
}
