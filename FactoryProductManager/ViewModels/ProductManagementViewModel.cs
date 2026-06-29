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
            _dbService = new DbService(DatabaseType.Project);
            Products = new ObservableCollection<Product>();
            InactiveProducts = new ObservableCollection<Product>();
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
                existing.BusinessType = product.BusinessType;
                existing.ProductCode = product.ProductCode;
                existing.ProductName = product.ProductName;
                existing.ProjectCode = product.ProjectCode;
                existing.HouseType = product.HouseType;
                existing.Area = product.Area;
                existing.CostTotalPrice = product.CostTotalPrice;
                existing.SellingTotalPrice = product.SellingTotalPrice;
                existing.FloorPlan = product.FloorPlan;
                existing.IsActive = product.IsActive;
                existing.UpdatedAt = product.UpdatedAt;
            }

            RefreshDisplayProducts();

            if (materials != null && materials.Count > 0)
            {
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
            // 直接保存 SelectedMaterial 到 ProductMaterialLibrary 表
            _dbService.SaveProductMaterialsToLibrary(productId, materials.ToList());
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
                var materials = _dbService.LoadProductMaterialsFromLibrary(product.Id);
                var parts = _dbService.GetProductParts(product.Id);

                // 列标题
                string[] headers = {
                    "图纸",                      // A
                    "物料名称",                  // B
                    "物料缩略图",                // C
                    "部件",                      // D
                    "部品",                      // E
                    "单位",                      // F
                    "品牌",                      // G
                    "工厂名称",                  // H
                    "工厂物料编码",              // I
                    "宇辰物料编码",              // J
                    "规格",                      // K
                    "数量",                      // L
                    "单价",                      // M
                    "成本总价",                  // N
                    "供货周期"                   // O
                };

                // 设置表头（第1行）
                worksheet.Cells[1, 1].Value = $"硬装一级产品明细 - {product.ProductName}";
                worksheet.Cells[1, 1, 1, 15].Merge = true;
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
                    worksheet.Cells[dataRow, 2].Value = "";                                         // B: 物料名称
                    worksheet.Cells[dataRow, 3].Value = "";                                         // C: 物料缩略图
                    worksheet.Cells[dataRow, 4].Value = "";                                         // D: 部件
                    worksheet.Cells[dataRow, 5].Value = "";                                         // E: 部品
                    worksheet.Cells[dataRow, 6].Value = "（无物料明细）";                           // F: 物料
                    worksheet.Cells[dataRow, 7].Value = "";                                         // G: 单位
                    worksheet.Cells[dataRow, 8].Value = "";                                         // H: 品牌
                    worksheet.Cells[dataRow, 9].Value = "";                                         // I: 工厂名称
                    worksheet.Cells[dataRow, 10].Value = product.ProductCode;                        // J: 工厂物料编码
                    worksheet.Cells[dataRow, 11].Value = "";                                        // K: 宇辰物料编码
                    worksheet.Cells[dataRow, 12].Value = "";                                         // L: 规格
                    worksheet.Cells[dataRow, 13].Value = product.Area;                              // M: 数量
                    worksheet.Cells[dataRow, 14].Value = "";                                        // N: 单价
                    worksheet.Cells[dataRow, 15].Value = product.CostTotalPrice;                   // O: 成本总价
                }
                else
                {
                    // 输出所有物料明细
                    foreach (var mat in materials)
                    {
                        // 如果是复合物料，输出子项；否则输出主行
                        if (mat.IsComposite && mat.Children.Count > 0)
                        {
                            // 复合物料：输出所有子项
                            foreach (var child in mat.Children)
                            {
                                worksheet.Cells[dataRow, 2].Value = child.MaterialName;                        // B: 物料名称
                                worksheet.Cells[dataRow, 4].Value = child.PartName;                            // D: 部件
                                worksheet.Cells[dataRow, 5].Value = child.ComponentName;                      // E: 部品
                                worksheet.Cells[dataRow, 6].Value = child.Unit;                              // F: 单位
                                worksheet.Cells[dataRow, 7].Value = child.Brand;                              // G: 品牌
                                worksheet.Cells[dataRow, 8].Value = child.FactoryName;                        // H: 工厂名称
                                worksheet.Cells[dataRow, 9].Value = child.FactoryMaterialCode;                 // I: 工厂物料编码
                                worksheet.Cells[dataRow, 10].Value = child.MyMaterialCode;                    // J: 宇辰物料编码
                                worksheet.Cells[dataRow, 11].Value = child.Specification;                     // K: 规格
                                worksheet.Cells[dataRow, 12].Value = child.Quantity;                         // L: 数量
                                worksheet.Cells[dataRow, 13].Value = child.UnitPrice;                         // M: 单价
                                worksheet.Cells[dataRow, 14].Value = child.TotalPrice;                       // N: 成本总价
                                worksheet.Cells[dataRow, 15].Value = child.Remarks;                          // O: 供货周期
                                dataRow++;
                            }
                        }
                        else
                        {
                            // 普通物料：输出主行
                            worksheet.Cells[dataRow, 2].Value = mat.MaterialName;                            // B: 物料名称
                            worksheet.Cells[dataRow, 4].Value = mat.PartName;                                // D: 部件
                            worksheet.Cells[dataRow, 5].Value = mat.ComponentName;                           // E: 部品
                            worksheet.Cells[dataRow, 6].Value = mat.Unit;                                   // F: 单位
                            worksheet.Cells[dataRow, 7].Value = mat.Brand;                                  // G: 品牌
                            worksheet.Cells[dataRow, 8].Value = mat.FactoryName;                             // H: 工厂名称
                            worksheet.Cells[dataRow, 9].Value = mat.FactoryMaterialCode;                     // I: 工厂物料编码
                            worksheet.Cells[dataRow, 10].Value = mat.MyMaterialCode;                         // J: 宇辰物料编码
                            worksheet.Cells[dataRow, 11].Value = mat.Specification;                          // K: 规格
                            worksheet.Cells[dataRow, 12].Value = mat.Quantity;                               // L: 数量
                            worksheet.Cells[dataRow, 13].Value = mat.UnitPrice;                             // M: 单价
                            worksheet.Cells[dataRow, 14].Value = mat.TotalPrice;                           // N: 成本总价
                            worksheet.Cells[dataRow, 15].Value = mat.SupplyCycle;                           // O: 供货周期
                            dataRow++;
                        }
                    }
                }

                // 合并A列（第3行到数据最后一行），用于放置图纸图片
                int startRow = 3;
                int endRow = dataRow - 1;
                if (endRow >= startRow)
                {
                    // 合并A列的起始行到结束行
                    worksheet.Cells[startRow, 1, endRow, 1].Merge = true;

                    // 在合并的单元格中放置图纸图片
                    EmbedFloorPlanImage(worksheet, product.FloorPlan, startRow, endRow);

                    // 设置行高（统一高度，确保图纸和缩略图显示一致）
                    for (int r = startRow; r <= endRow; r++)
                    {
                        worksheet.Row(r).Height = 60;
                    }

                    // 为每行添加物料缩略图（C列，每行单独一个图片）
                    int thumbnailRow = startRow;
                    foreach (var mat in materials)
                    {
                        if (mat.IsComposite && mat.Children.Count > 0)
                        {
                            foreach (var child in mat.Children)
                            {
                                EmbedMaterialThumbnail(worksheet, child.ImageUrl, thumbnailRow, 3);
                                thumbnailRow++;
                            }
                        }
                        else
                        {
                            EmbedMaterialThumbnail(worksheet, mat.ImageUrl, thumbnailRow, 3);
                            thumbnailRow++;
                        }
                    }
                }

                // 设置所有列宽（固定值，确保文字完整显示）
                worksheet.Column(1).Width = 18;    // A: 图纸
                worksheet.Column(2).Width = 22;    // B: 物料名称
                worksheet.Column(3).Width = 10;    // C: 物料缩略图
                worksheet.Column(4).Width = 18;    // D: 部件
                worksheet.Column(5).Width = 18;    // E: 部品
                worksheet.Column(6).Width = 8;     // F: 单位
                worksheet.Column(7).Width = 18;   // G: 品牌
                worksheet.Column(8).Width = 22;    // H: 工厂名称
                worksheet.Column(9).Width = 22;    // I: 工厂物料编码
                worksheet.Column(10).Width = 18;   // J: 宇辰物料编码
                worksheet.Column(11).Width = 25;   // K: 规格
                worksheet.Column(12).Width = 10;  // L: 数量
                worksheet.Column(13).Width = 12;  // M: 单价
                worksheet.Column(14).Width = 15;   // N: 成本总价
                worksheet.Column(15).Width = 15;  // O: 供货周期
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

        /// <summary>
        /// 将物料缩略图嵌入到指定单元格的B列（不合并单元格，每行独立图片）
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <param name="row">行号</param>
        /// <param name="column">列号（默认2，即B列）</param>
        private void EmbedMaterialThumbnail(ExcelWorksheet worksheet, string? imagePath, int row, int column = 2)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return;
            }

            try
            {
                var fullPath = imagePath;
                // 如果是相对路径，转换为绝对路径
                if (!Path.IsPathRooted(imagePath))
                {
                    fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                }

                if (!File.Exists(fullPath))
                    return;

                // 获取图片原始尺寸
                using var image = System.Drawing.Image.FromFile(fullPath);
                var originalWidth = image.Width;
                var originalHeight = image.Height;

                // 列宽约等于 7 像素/单位，列宽12 = 84像素宽度
                int cellWidth = 12 * 7;   // 84像素
                // 行高约等于 0.75 像素/单位，行高60 = 45像素高度
                int cellHeight = 60;       // 像素（行高60）

                // 计算缩放比例，使图片适应单元格
                var displayWidth = originalWidth;
                var displayHeight = originalHeight;

                // 宽度限制
                if (displayWidth > cellWidth)
                {
                    var ratio = (double)cellWidth / displayWidth;
                    displayWidth = cellWidth;
                    displayHeight = (int)(displayHeight * ratio);
                }

                // 高度限制
                if (displayHeight > cellHeight)
                {
                    var ratio = (double)cellHeight / displayHeight;
                    displayHeight = cellHeight;
                    displayWidth = (int)(displayWidth * ratio);
                }

                // EPPlus 7+ 使用 FileInfo 方式添加图片
                var fileInfo = new FileInfo(fullPath);
                var pic = worksheet.Drawings.AddPicture($"Thumb_{row}_{column}_{Guid.NewGuid():N}", fileInfo);

                // 设置图片缩放后的尺寸
                pic.SetSize(displayWidth, displayHeight);

                // 设置图片位置：居中于单元格
                // 行偏移 = (单元格高度 - 图片高度) / 2
                int rowOffset = Math.Max(0, (cellHeight - displayHeight) / 2);
                // 列偏移 = (单元格宽度 - 图片宽度) / 2
                int colOffset = Math.Max(0, (cellWidth - displayWidth) / 2);

                pic.SetPosition(row - 1, rowOffset, column - 1, colOffset);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"嵌入物料缩略图失败: {ex.Message}");
            }
        }
    }
}
