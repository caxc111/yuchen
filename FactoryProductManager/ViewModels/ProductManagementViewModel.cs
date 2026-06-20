using FactoryProductManager.Models;
using FactoryProductManager.Services;
using FactoryProductManager.Views;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace FactoryProductManager.ViewModels
{
    public class ProductManagementViewModel : ViewModelBase
    {
        private readonly DbService _dbService;
        private string _searchKeyword = string.Empty;
        private bool _showInactiveProducts = false;

        public ObservableCollection<Product> Products { get; }
        public ObservableCollection<Product> InactiveProducts { get; }
        public ObservableCollection<Product> DisplayProducts { get; }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    Refresh();
                }
            }
        }

        public bool ShowInactiveProducts
        {
            get => _showInactiveProducts;
            set
            {
                if (SetProperty(ref _showInactiveProducts, value))
                {
                    RefreshDisplayProducts();
                    OnPropertyChanged(nameof(ShowInactiveButtonText));
                    OnPropertyChanged(nameof(HasInactiveProducts));
                    OnPropertyChanged(nameof(HasInactiveProductsVisibility));
                }
            }
        }

        public string ShowInactiveButtonText
        {
            get
            {
                int count = Products.Count(p => !p.IsActive);
                return ShowInactiveProducts ? "收起停用" : $"显示停用 ({count})";
            }
        }

        public bool HasInactiveProducts => Products.Any(p => !p.IsActive);

        public Visibility HasInactiveProductsVisibility => HasInactiveProducts ? Visibility.Visible : Visibility.Collapsed;

        public ProductManagementViewModel()
        {
            _dbService = new DbService();
            Products = new ObservableCollection<Product>();
            DisplayProducts = new ObservableCollection<Product>();
            LoadProducts();
        }

        private void RefreshDisplayProducts()
        {
            DisplayProducts.Clear();

            // 先显示启用的产品
            foreach (var product in Products.Where(p => p.IsActive))
            {
                DisplayProducts.Add(product);
            }

            // 如果展开停用产品，再显示停用的产品
            if (ShowInactiveProducts)
            {
                foreach (var product in Products.Where(p => !p.IsActive))
                {
                    DisplayProducts.Add(product);
                }
            }
        }

        public void Refresh()
        {
            LoadProducts(SearchKeyword);
        }

        public void AddProduct(Product product, IReadOnlyList<ProductPart>? parts = null, IReadOnlyList<SelectedMaterial>? materials = null)
        {
            product.CreatedAt = DateTime.Now;
            product.UpdatedAt = DateTime.Now;
            product.Id = _dbService.AddProduct(product, parts);
            Products.Add(product);
            RefreshDisplayProducts();

            if (materials != null && materials.Count > 0)
            {
                PersistSelectedMaterials(product.Id, parts, materials);
            }
        }

        public void UpdateProduct(Product product, IReadOnlyList<ProductPart>? parts = null, IReadOnlyList<SelectedMaterial>? materials = null)
        {
            product.UpdatedAt = DateTime.Now;
            _dbService.UpdateProduct(product, parts);

            var existing = Products.FirstOrDefault(item => item.Id == product.Id);
            if (existing != null)
            {
                var index = Products.IndexOf(existing);
                Products[index] = product;
            }

            RefreshDisplayProducts();

            if (materials != null && materials.Count > 0)
            {
                _dbService.DeleteProductPartMaterialsByProduct(product.Id);
                PersistSelectedMaterials(product.Id, parts, materials);
            }
        }

        public void EnableProduct(int productId)
        {
            var product = Products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
            {
                product.IsActive = true;
                _dbService.UpdateProduct(product);
                RefreshDisplayProducts();
                OnPropertyChanged(nameof(ShowInactiveButtonText));
                OnPropertyChanged(nameof(HasInactiveProducts));
                    OnPropertyChanged(nameof(HasInactiveProductsVisibility));
            }
        }

        public void DisableProduct(int productId)
        {
            var product = Products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
            {
                product.IsActive = false;
                _dbService.UpdateProduct(product);
                RefreshDisplayProducts();
                OnPropertyChanged(nameof(ShowInactiveButtonText));
                OnPropertyChanged(nameof(HasInactiveProducts));
                    OnPropertyChanged(nameof(HasInactiveProductsVisibility));
            }
        }

        private void PersistSelectedMaterials(int productId, IReadOnlyList<ProductPart>? parts, IReadOnlyList<SelectedMaterial> materials)
        {
            var partMap = (parts ?? _dbService.GetProductParts(productId))
                .ToDictionary(p => p.PartName, p => p.Id);

            var entities = materials.Select(sm => new ProductPartMaterial
            {
                ProductId = productId,
                PartId = partMap.TryGetValue(sm.ComponentName, out var pid) && pid > 0
                    ? (int?)pid
                    : null,
                PartName = sm.ComponentName,
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
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }).ToList();

            _dbService.AddProductPartMaterials(productId, entities);
        }

        public void DeleteProduct(int id)
        {
            var existing = Products.FirstOrDefault(item => item.Id == id);
            _dbService.DeleteProduct(id);
            if (existing != null)
            {
                Products.Remove(existing);
                RefreshDisplayProducts();
            }
        }

        public void ExportToExcel()
        {
            LogService.Info("[ExportToExcel] 方法开始执行");
            if (Products.Count == 0)
            {
                LogService.Warning("[ExportToExcel] 没有可导出的产品数据，显示警告弹窗");
                MessageBox.Show("没有可导出的产品数据！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel文件 (*.xlsx)|*.xlsx",
                FileName = $"产品数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "导出产品数据",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            LogService.Info("[ExportToExcel] 显示保存文件对话框");
            if (saveDialog.ShowDialog() != true)
            {
                LogService.Info("[ExportToExcel] 用户取消保存对话框");
                return;
            }
            LogService.Info($"[ExportToExcel] 用户选择保存路径: {saveDialog.FileName}");

            using var package = new ExcelPackage();

            foreach (var product in Products)
            {
                LogService.Info($"[ExportToExcel] 开始处理产品: {product.ProductName} (ID={product.Id})");
                // 为每个产品创建一个工作表，使用产品名称作为工作表名
                var sheetName = string.IsNullOrWhiteSpace(product.ProductName)
                    ? $"户型_{product.Id}"
                    : SanitizeSheetName(product.ProductName);

                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                // 获取该产品的所有物料明细
                var materials = _dbService.GetProductPartMaterials(product.Id);
                var parts = _dbService.GetProductParts(product.Id);

                // 列标题
                string[] headers = {
                    "图纸 (Drawing)",           // A
                    "部件 (Space)",             // B
                    "部品 (Components)",        // C
                    "物料 (Material)",          // D
                    "单位 (Unit)",              // E
                    "品牌",                     // F
                    "工厂名称",                 // G
                    "工厂物料编码",             // H
                    "宇辰物料编码",             // I
                    "规格",                     // J
                    "数量 (Quantity)",          // K
                    "单价 (Unit price)",        // L
                    "成本总价",                 // M
                    "供货周期 (Supply cycle)"   // N
                };

                // 设置表头（第1行）
                worksheet.Cells[1, 1].Value = $"硬装一级产品明细 - {product.ProductName}";
                worksheet.Cells[1, 1, 1, 14].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                // 设置列标题（第2行）
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[2, i + 1].Value = headers[i];
                    worksheet.Cells[2, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[2, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[2, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[2, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 第3行开始：输出数据
                int dataRow = 3;

                if (materials.Count == 0)
                {
                    // 如果没有物料明细，输出产品基本信息
                    worksheet.Cells[dataRow, 2].Value = "";                                         // B: 部件（无物料时留空）
                    worksheet.Cells[dataRow, 3].Value = "";                                         // C: 部品
                    worksheet.Cells[dataRow, 4].Value = "（无物料明细）";                           // D: 物料
                    worksheet.Cells[dataRow, 5].Value = "";                                         // E: 单位
                    worksheet.Cells[dataRow, 6].Value = "";                                         // F: 品牌
                    worksheet.Cells[dataRow, 7].Value = "";                                         // G: 工厂名称
                    worksheet.Cells[dataRow, 8].Value = product.ProductCode;                         // H: 工厂物料编码
                    worksheet.Cells[dataRow, 9].Value = "";                                         // I: 宇辰物料编码
                    worksheet.Cells[dataRow, 10].Value = "";                                        // J: 规格
                    worksheet.Cells[dataRow, 11].Value = product.Area;                               // K: 数量
                    worksheet.Cells[dataRow, 12].Value = "";                                         // L: 单价
                    worksheet.Cells[dataRow, 13].Value = product.CostTotalPrice;                    // M: 成本总价
                    worksheet.Cells[dataRow, 14].Value = "";                                        // N: 供货周期
                }
                else
                {
                    // 输出所有物料明细
                    foreach (var mat in materials)
                    {
                        worksheet.Cells[dataRow, 2].Value = mat.PartName;                            // B: 部件（空间）
                        worksheet.Cells[dataRow, 3].Value = mat.ComponentName;                      // C: 部品（物料分类）
                        worksheet.Cells[dataRow, 4].Value = mat.MaterialName;                        // D: 物料（具体材料）
                        worksheet.Cells[dataRow, 5].Value = mat.Unit;                               // E: 单位
                        worksheet.Cells[dataRow, 6].Value = mat.Brand;                              // F: 品牌
                        worksheet.Cells[dataRow, 7].Value = mat.FactoryName;                       // G: 工厂名称
                        worksheet.Cells[dataRow, 8].Value = mat.FactoryMaterialCode;                 // H: 工厂物料编码
                        worksheet.Cells[dataRow, 9].Value = mat.MyMaterialCode;                     // I: 宇辰物料编码
                        worksheet.Cells[dataRow, 10].Value = mat.Specification;                     // J: 规格
                        worksheet.Cells[dataRow, 11].Value = mat.Quantity;                          // K: 数量
                        worksheet.Cells[dataRow, 12].Value = mat.UnitPrice;                         // L: 单价
                        worksheet.Cells[dataRow, 13].Value = mat.TotalPrice;                        // M: 成本总价
                        worksheet.Cells[dataRow, 14].Value = mat.Remarks;                           // N: 供货周期
                        dataRow++;
                    }
                }

                // 合并A列（第3行到数据最后一行），用于放置图片
                int startRow = 3;
                int endRow = dataRow - 1;
                if (endRow >= startRow)
                {
                    // 合并A列的起始行到结束行
                    worksheet.Cells[startRow, 1, endRow, 1].Merge = true;

                    // 在合并的单元格中放置图片
                    EmbedFloorPlanImage(worksheet, product.FloorPlan, startRow, endRow);
                }

                // 设置所有列宽（固定值，确保文字完整显示）
                worksheet.Column(1).Width = 18;    // A: 图纸
                worksheet.Column(2).Width = 20;    // B: 部件
                worksheet.Column(3).Width = 20;    // C: 部品
                worksheet.Column(4).Width = 25;    // D: 物料
                worksheet.Column(5).Width = 8;     // E: 单位
                worksheet.Column(6).Width = 18;    // F: 品牌
                worksheet.Column(7).Width = 22;    // G: 工厂名称
                worksheet.Column(8).Width = 22;    // H: 工厂物料编码
                worksheet.Column(9).Width = 18;    // I: 宇辰物料编码
                worksheet.Column(10).Width = 25;   // J: 规格
                worksheet.Column(11).Width = 20;   // K: 数量
                worksheet.Column(12).Width = 20;   // L: 单价
                worksheet.Column(13).Width = 20;   // M: 成本总价
                worksheet.Column(14).Width = 25;   // N: 供货周期
            }

            package.SaveAs(new FileInfo(saveDialog.FileName));
            LogService.Info($"产品数据导出成功，共导出 {Products.Count} 个产品，文件: {saveDialog.FileName}");
            LogService.Info($"[ExportToExcel] 显示导出成功弹窗: MessageBox.Show(\"成功导出 {Products.Count} 个户型的工作表！\", \"提示\", MessageBoxButton.OK, MessageBoxImage.Information)");
            MessageBox.Show($"成功导出 {Products.Count} 个户型的工作表！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            LogService.Info("[ExportToExcel] 方法执行完成");
        }

        /// <summary>
        /// 清理工作表名称中的非法字符
        /// </summary>
        private string SanitizeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Sheet";

            // Excel 工作表名称不能包含 : \ / ? * [ ]
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            var sanitized = name;
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // 工作表名称最长31个字符
            if (sanitized.Length > 31)
                sanitized = sanitized.Substring(0, 31);

            return string.IsNullOrWhiteSpace(sanitized) ? "Sheet" : sanitized;
        }

        private void LoadProducts(string? keyword = null)
        {
            Products.Clear();

            var allProducts = _dbService.GetProducts(keyword);

            foreach (var product in allProducts)
            {
                Products.Add(product);
            }

            RefreshDisplayProducts();
            OnPropertyChanged(nameof(ShowInactiveButtonText));
            OnPropertyChanged(nameof(HasInactiveProducts));
            OnPropertyChanged(nameof(HasInactiveProductsVisibility));
        }

        /// <summary>
        /// 将平面图图片嵌入到Excel指定单元格的A列（支持合并单元格），返回图片的像素尺寸
        /// </summary>
        /// <param name="startRow">合并单元格的起始行</param>
        /// <param name="endRow">合并单元格的结束行</param>
        private (int width, int height) EmbedFloorPlanImage(ExcelWorksheet worksheet, string? imagePath, int startRow, int endRow)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                LogService.Debug($"[EmbedFloorPlanImage] 图片路径为空，跳过");
                return (0, 0);
            }
            LogService.Debug($"[EmbedFloorPlanImage] 开始处理图片: {imagePath}, 行范围: {startRow}-{endRow}");

            try
            {
                var fullPath = imagePath;
                // 如果是相对路径，转换为绝对路径
                if (!Path.IsPathRooted(imagePath))
                {
                    fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                }

                if (!File.Exists(fullPath))
                    return (0, 0);

                // 获取图片原始尺寸
                using var image = System.Drawing.Image.FromFile(fullPath);
                var originalWidth = image.Width;
                var originalHeight = image.Height;

                // 根据合并单元格的总高度计算图片最大显示尺寸
                // 行高约等于 0.75 像素/单位
                int mergedRows = endRow - startRow + 1;
                int totalHeight = mergedRows * 15;  // 估算合并后的总像素高度

                const int maxDisplayWidth = 120;    // 图片最大显示宽度（像素）

                var displayWidth = originalWidth;
                var displayHeight = originalHeight;

                // 宽度限制
                if (displayWidth > maxDisplayWidth)
                {
                    var ratio = (double)maxDisplayWidth / displayWidth;
                    displayWidth = maxDisplayWidth;
                    displayHeight = (int)(displayHeight * ratio);
                }

                // 高度限制（不超过合并单元格的总高度）
                if (displayHeight > totalHeight)
                {
                    var ratio = (double)totalHeight / displayHeight;
                    displayHeight = totalHeight;
                    displayWidth = (int)(displayWidth * ratio);
                }

                // EPPlus 7+ 使用 FileInfo 方式添加图片
                var fileInfo = new FileInfo(fullPath);
                var pic = worksheet.Drawings.AddPicture($"FloorPlan_{startRow}_{endRow}", fileInfo);

                // 设置图片缩放后的尺寸
                pic.SetSize(displayWidth, displayHeight);

                // 设置图片位置到合并单元格的起始位置
                pic.SetPosition(startRow - 1, 0, 0, 0);

                return (displayWidth, displayHeight);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"嵌入图片失败: {ex.Message}");
                return (0, 0);
            }
        }
    }
}
