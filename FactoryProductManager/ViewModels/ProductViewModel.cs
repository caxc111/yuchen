using FactoryProductManager.Models;
using FactoryProductManager.Services;
using OfficeOpenXml;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace FactoryProductManager.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private readonly DbService _dbService;

        public ObservableCollection<FactoryProduct> Products { get; set; }

        public ProductViewModel()
        {
            LogService.LogViewModelCreation(nameof(ProductViewModel));
            try
            {
                LogService.Info("初始化ProductViewModel...");
                _dbService = new DbService();
                Products = new ObservableCollection<FactoryProduct>();
                LogService.Info("开始加载产品数据...");
                LoadProducts();
                LogService.Info($"ProductViewModel初始化完成，共加载 {Products.Count} 条产品数据");
            }
            catch (Exception ex)
            {
                LogService.Error("ProductViewModel初始化失败", ex);
                throw;
            }
        }

        private void LoadProducts()
        {
            try
            {
                LogService.Debug("进入LoadProducts方法");
                Products.Clear();
                var products = _dbService.GetFactoryProducts();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
                LogService.Debug($"LoadProducts方法完成，加载了 {Products.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.Error("加载产品数据失败", ex);
                throw;
            }
        }

        public void AddProduct(FactoryProduct product)
        {
            try
            {
                LogService.Info("开始添加产品: " + product.ProductName);
                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;
                var id = _dbService.AddFactoryProduct(product);
                product.Id = id;
                Products.Add(product);
                LogService.Info("产品添加成功，ID: " + id);
            }
            catch (Exception ex)
            {
                LogService.Error("添加产品失败: " + product.ProductName, ex);
                throw;
            }
        }

        public void UpdateProduct(FactoryProduct product)
        {
            try
            {
                LogService.Info("开始更新产品: " + product.ProductName);
                product.UpdatedAt = DateTime.Now;
                _dbService.UpdateFactoryProduct(product);
                var index = Products.IndexOf(Products.First(p => p.Id == product.Id));
                if (index >= 0)
                {
                    Products[index] = product;
                }
                LogService.Info("产品更新成功，ID: " + product.Id);
            }
            catch (Exception ex)
            {
                LogService.Error("更新产品失败: " + product.ProductName, ex);
                throw;
            }
        }

        public void DeleteProduct(int id)
        {
            try
            {
                var product = Products.FirstOrDefault(p => p.Id == id);
                LogService.Info("开始删除产品: " + (product?.ProductName ?? "未知"));
                _dbService.DeleteFactoryProduct(id);
                if (product != null)
                {
                    Products.Remove(product);
                }
                LogService.Info("产品删除成功，ID: " + id);
            }
            catch (Exception ex)
            {
                LogService.Error("删除产品失败，ID: " + id, ex);
                throw;
            }
        }

        public void Refresh()
        {
            try
            {
                LogService.Info("刷新产品数据...");
                LoadProducts();
                LogService.Info("产品数据刷新完成");
            }
            catch (Exception ex)
            {
                LogService.Error("刷新产品数据失败", ex);
                throw;
            }
        }

        public void ExportToExcel()
        {
            try
            {
                LogService.Info("开始导出物料数据到Excel...");

                if (Products == null || Products.Count == 0)
                {
                    LogService.Warning("没有可导出的物料数据");
                    MessageBox.Show("没有可导出的物料数据！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx",
                    FileName = $"物料数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "导出物料数据",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("物料数据");

                        string[] headers = new string[]
                        {
                            "工厂物料编码", "宇辰物料编码", "产品名称", "品牌", "规格",
                            "纹理", "工艺", "适用场景", "认证情况", "类别",
                            "所属工厂", "图片路径", "创建时间", "更新时间"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                            worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        }

                        int row = 2;
                        foreach (var product in Products)
                        {
                            worksheet.Cells[row, 1].Value = product.FactoryProductCode;
                            worksheet.Cells[row, 2].Value = product.MyProductCode;
                            worksheet.Cells[row, 3].Value = product.ProductName;
                            worksheet.Cells[row, 4].Value = product.Brand;
                            worksheet.Cells[row, 5].Value = product.Specification;
                            worksheet.Cells[row, 6].Value = product.Texture;
                            worksheet.Cells[row, 7].Value = product.Process;
                            worksheet.Cells[row, 8].Value = product.UsageScenario;
                            worksheet.Cells[row, 9].Value = product.Certifications;
                            worksheet.Cells[row, 10].Value = product.Category;
                            worksheet.Cells[row, 11].Value = product.FactoryName;
                            worksheet.Cells[row, 12].Value = product.ImageUrl;
                            worksheet.Cells[row, 13].Value = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                            worksheet.Cells[row, 14].Value = product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                            row++;
                        }

                        for (int col = 1; col <= headers.Length; col++)
                        {
                            worksheet.Cells[2, col, row - 1, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            worksheet.Cells[2, col, row - 1, col].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                        worksheet.Cells[worksheet.Dimension.Address].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                        string finalPath = saveDialog.FileName;
                        bool saveSuccess = false;

                        try
                        {
                            var fileInfo = new FileInfo(finalPath);
                            package.SaveAs(fileInfo);
                            saveSuccess = true;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            LogService.Warning($"无法保存到 {finalPath}，尝试保存到文档文件夹");

                            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                            var fileName = Path.GetFileName(finalPath);
                            finalPath = Path.Combine(docPath, fileName);

                            var fileInfo = new FileInfo(finalPath);
                            package.SaveAs(fileInfo);
                            saveSuccess = true;

                            MessageBox.Show($"无法保存到原位置，已自动保存到文档文件夹：\n{finalPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        if (saveSuccess)
                        {
                            LogService.Info($"物料数据导出成功，共导出 {Products.Count} 条记录，文件: {finalPath}");
                            if (!saveDialog.FileName.Equals(finalPath))
                            {
                                MessageBox.Show($"成功导出 {Products.Count} 条物料数据！\n文件位置：{finalPath}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("导出物料数据到Excel失败", ex);
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}