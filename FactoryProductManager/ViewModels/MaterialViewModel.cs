using FactoryProductManager.Models;
using FactoryProductManager.Services;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;

namespace FactoryProductManager.ViewModels
{
    public class MaterialViewModel : ViewModelBase
    {
        private readonly DbService _dbService;
        private string _currentSearchKeyword = string.Empty;

        public ObservableCollection<FactoryMaterial> Materials { get; set; }

        public MaterialViewModel()
        {
            LogService.LogViewModelCreation(nameof(MaterialViewModel));
            try
            {
                LogService.Info("初始化MaterialViewModel...");
                _dbService = new DbService();
                Materials = new ObservableCollection<FactoryMaterial>();
                LogService.Info("开始加载物料数据...");
                LoadMaterials();
                LogService.Info($"MaterialViewModel初始化完成，共加载 {Materials.Count} 条物料数据");
            }
            catch (Exception ex)
            {
                LogService.Error("MaterialViewModel初始化失败", ex);
                throw;
            }
        }

        private void LoadMaterials()
        {
            LoadMaterials(null);
        }

        private void LoadMaterials(string? searchKeyword)
        {
            try
            {
                LogService.Debug("进入LoadMaterials方法");
                Materials.Clear();
                var materials = _dbService.GetFactoryMaterials();

                if (!string.IsNullOrWhiteSpace(searchKeyword))
                {
                    var keyword = searchKeyword.ToLower().Trim();
                    materials = materials.Where(m =>
                        (m.FactoryMaterialCode?.ToLower().Contains(keyword) ?? false) ||
                        (m.MyMaterialCode?.ToLower().Contains(keyword) ?? false) ||
                        (m.MaterialName?.ToLower().Contains(keyword) ?? false) ||
                        (m.Category?.ToLower().Contains(keyword) ?? false) ||
                        (m.CategoryDisplay?.ToLower().Contains(keyword) ?? false) ||
                        (m.FactoryName?.ToLower().Contains(keyword) ?? false) ||
                        (m.Brand?.ToLower().Contains(keyword) ?? false) ||
                        (m.Specification?.ToLower().Contains(keyword) ?? false) ||
                        (m.Texture?.ToLower().Contains(keyword) ?? false) ||
                        (m.Process?.ToLower().Contains(keyword) ?? false) ||
                        (m.Unit?.ToLower().Contains(keyword) ?? false)
                    ).ToList();
                    LogService.Debug($"搜索关键词: {searchKeyword}，筛选出 {materials.Count} 条记录");
                }

                foreach (var material in materials)
                {
                    Materials.Add(material);
                }
                LogService.Debug($"LoadMaterials方法完成，加载了 {Materials.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.Error("加载物料数据失败", ex);
                throw;
            }
        }

        public void Search(string? keyword)
        {
            LogService.Info($"执行物料搜索: {keyword}");
            _currentSearchKeyword = keyword ?? string.Empty;
            LoadMaterials(keyword);
        }

        public void AddMaterial(FactoryMaterial material)
        {
            try
            {
                LogService.Info("开始添加物料: " + material.MaterialName);
                material.CreatedAt = DateTime.Now;
                material.UpdatedAt = DateTime.Now;
                var id = _dbService.AddFactoryMaterial(material);
                material.Id = id;
                LoadMaterials(_currentSearchKeyword);
                LogService.Info("物料添加成功，ID: " + id);
            }
            catch (Exception ex)
            {
                LogService.Error("添加物料失败: " + material.MaterialName, ex);
                throw;
            }
        }

        public void UpdateMaterial(FactoryMaterial material)
        {
            try
            {
                LogService.Info("开始更新物料: " + material.MaterialName);
                material.UpdatedAt = DateTime.Now;
                _dbService.UpdateFactoryMaterial(material);
                var index = Materials.IndexOf(Materials.First(m => m.Id == material.Id));
                if (index >= 0)
                {
                    Materials[index] = material;
                }
                LogService.Info("物料更新成功，ID: " + material.Id);
            }
            catch (Exception ex)
            {
                LogService.Error("更新物料失败: " + material.MaterialName, ex);
                throw;
            }
        }

        public void DeleteMaterial(int id)
        {
            try
            {
                var material = Materials.FirstOrDefault(m => m.Id == id);
                LogService.Info("开始删除物料: " + (material?.MaterialName ?? "未知"));
                _dbService.DeleteFactoryMaterial(id);
                if (material != null)
                {
                    Materials.Remove(material);
                }
                LogService.Info("物料删除成功，ID: " + id);
            }
            catch (Exception ex)
            {
                LogService.Error("删除物料失败，ID: " + id, ex);
                throw;
            }
        }

        public void Refresh()
        {
            try
            {
                LogService.Info("刷新物料数据...");
                LoadMaterials(_currentSearchKeyword);
                LogService.Info("物料数据刷新完成");
            }
            catch (Exception ex)
            {
                LogService.Error("刷新物料数据失败", ex);
                throw;
            }
        }

        public void ExportToExcel()
        {
            try
            {
                LogService.Info("开始导出物料数据到Excel...");

                if (Materials == null || Materials.Count == 0)
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
                            "工厂物料编码", "宇辰物料编码", "物料名称", "类别", "所属工厂",
                            "品牌", "规格", "纹理", "工艺", "适用场景",
                            "认证情况", "图片", "创建时间", "更新时间"
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
                        foreach (var material in Materials)
                        {
                            worksheet.Cells[row, 1].Value = material.FactoryMaterialCode;
                            worksheet.Cells[row, 2].Value = material.MyMaterialCode;
                            worksheet.Cells[row, 3].Value = material.MaterialName;
                            worksheet.Cells[row, 4].Value = material.CategoryDisplay;
                            worksheet.Cells[row, 5].Value = material.FactoryName;
                            worksheet.Cells[row, 6].Value = material.Brand;
                            worksheet.Cells[row, 7].Value = material.Specification;
                            worksheet.Cells[row, 8].Value = material.Texture;
                            worksheet.Cells[row, 9].Value = material.Process;
                            worksheet.Cells[row, 10].Value = material.UsageScenario;
                            worksheet.Cells[row, 11].Value = material.Certifications;
                            worksheet.Cells[row, 13].Value = material.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                            worksheet.Cells[row, 14].Value = material.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                            worksheet.Row(row).Height = 72;
                            AddMaterialImageToWorksheet(worksheet, row, 12, material.ImageUrl);
                            row++;
                        }

                        for (int col = 1; col <= headers.Length; col++)
                        {
                            worksheet.Cells[2, col, row - 1, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            worksheet.Cells[2, col, row - 1, col].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        }

                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                        worksheet.Column(12).Width = 14;
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
                            LogService.Info($"物料数据导出成功，共导出 {Materials.Count} 条记录，文件: {finalPath}");
                            if (!saveDialog.FileName.Equals(finalPath))
                            {
                                MessageBox.Show($"成功导出 {Materials.Count} 条物料数据！\n文件位置：{finalPath}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private static void AddMaterialImageToWorksheet(ExcelWorksheet worksheet, int row, int column, string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                worksheet.Cells[row, column].Value = "无图";
                return;
            }

            using (var sourceImage = Image.FromFile(imagePath))
            {
                var picture = worksheet.Drawings.AddPicture($"material_{row}_{column}", imagePath);
                var targetSize = GetScaledSize(sourceImage.Width, sourceImage.Height, 72, 72);

                picture.SetSize(targetSize.width, targetSize.height);
                picture.SetPosition(
                    row - 1,
                    Math.Max(0, (int)Math.Round((72 - targetSize.height) / 2d)),
                    column - 1,
                    Math.Max(0, (int)Math.Round((96 - targetSize.width) / 2d)));
            }
        }

        private static (int width, int height) GetScaledSize(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            if (originalWidth <= 0 || originalHeight <= 0)
            {
                return (maxWidth, maxHeight);
            }

            double ratio = Math.Min((double)maxWidth / originalWidth, (double)maxHeight / originalHeight);
            ratio = Math.Min(ratio, 1d);

            return ((int)Math.Round(originalWidth * ratio), (int)Math.Round(originalHeight * ratio));
        }
    }
}
