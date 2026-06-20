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
    public class FactoryViewModel : ViewModelBase
    {
        private readonly DbService _dbService;
        private string _currentSearchKeyword = string.Empty;

        public ObservableCollection<Factory> Factories { get; set; }

        public FactoryViewModel()
        {
            LogService.LogViewModelCreation(nameof(FactoryViewModel));
            try
            {
                LogService.Info("初始化FactoryViewModel...");
                _dbService = new DbService();
                Factories = new ObservableCollection<Factory>();
                LogService.Info("开始加载工厂数据...");
                LoadFactories();
                LogService.Info($"FactoryViewModel初始化完成，共加载 {Factories.Count} 条工厂数据");
            }
            catch (Exception ex)
            {
                LogService.Error("FactoryViewModel初始化失败", ex);
                throw;
            }
        }

        private void LoadFactories(string? searchKeyword = null)
        {
            try
            {
                LogService.Debug("进入LoadFactories方法");
                Factories.Clear();
                var factories = _dbService.GetFactories();
                
                // 如果有搜索关键词，进行过滤
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    var keyword = searchKeyword.ToLower().Trim();
                    factories = factories.Where(f => 
                        (f.FactoryCode?.ToLower().Contains(keyword) ?? false) ||
                        (f.FactoryName?.ToLower().Contains(keyword) ?? false) ||
                        (f.FactoryType?.ToLower().Contains(keyword) ?? false) ||
                        (f.Address?.ToLower().Contains(keyword) ?? false) ||
                        (f.Certifications?.ToLower().Contains(keyword) ?? false) ||
                        (f.Description?.ToLower().Contains(keyword) ?? false) ||
                        (f.Scale?.ToLower().Contains(keyword) ?? false) ||
                        (f.ProductionCapacity?.ToLower().Contains(keyword) ?? false) ||
                        (f.ControllingPerson?.ToLower().Contains(keyword) ?? false) ||
                        (f.ContactPerson?.ToLower().Contains(keyword) ?? false) ||
                        (f.ContactInfo?.ToLower().Contains(keyword) ?? false) ||
                        (f.EmployeeCount?.ToString().Contains(keyword) ?? false)
                    ).ToList();
                    LogService.Debug($"搜索关键词: {searchKeyword}，筛选出 {factories.Count} 条记录");
                }
                
                foreach (var factory in factories)
                {
                    Factories.Add(factory);
                }
                LogService.Debug($"LoadFactories方法完成，加载了 {Factories.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.Error("加载工厂数据失败", ex);
                throw;
            }
        }
        
        public void Search(string? keyword)
        {
            LogService.Info($"执行搜索: {keyword}");
            _currentSearchKeyword = keyword ?? string.Empty;
            LoadFactories(keyword);
        }

        public void AddFactory(Factory factory)
        {
            try
            {
                LogService.Info("开始添加工厂: " + factory.FactoryName);
                
                // 检查工厂编码是否已存在
                var existingFactory = Factories.FirstOrDefault(f => f.FactoryCode == factory.FactoryCode);
                if (existingFactory != null)
                {
                    LogService.Warning($"工厂编码 '{factory.FactoryCode}' 已存在");
                    System.Windows.MessageBox.Show($"工厂编码 '{factory.FactoryCode}' 已存在，请使用其他编码！", "添加失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                factory.CreatedAt = DateTime.Now;
                factory.UpdatedAt = DateTime.Now;
                var id = _dbService.AddFactory(factory);
                factory.Id = id;
                
                // 根据当前搜索关键词刷新列表
                LoadFactories(_currentSearchKeyword);
                
                LogService.Info("工厂添加成功，ID: " + id);
                System.Windows.MessageBox.Show("工厂添加成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogService.Error("添加工厂失败: " + factory.FactoryName, ex);
                System.Windows.MessageBox.Show($"添加工厂失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateFactory(Factory factory)
        {
            try
            {
                LogService.Info("开始更新工厂: " + factory.FactoryName);
                factory.UpdatedAt = DateTime.Now;
                _dbService.UpdateFactory(factory);
                var index = Factories.IndexOf(Factories.First(f => f.Id == factory.Id));
                if (index >= 0)
                {
                    Factories[index] = factory;
                }
                LogService.Info("工厂更新成功，ID: " + factory.Id);
            }
            catch (Exception ex)
            {
                LogService.Error("更新工厂失败: " + factory.FactoryName, ex);
                throw;
            }
        }

        public void DeleteFactory(int id)
        {
            try
            {
                var factory = Factories.FirstOrDefault(f => f.Id == id);
                LogService.Info("开始删除工厂: " + (factory?.FactoryName ?? "未知"));
                _dbService.DeleteFactory(id);
                if (factory != null)
                {
                    Factories.Remove(factory);
                }
                LogService.Info("工厂删除成功，ID: " + id);
            }
            catch (Exception ex)
            {
                LogService.Error("删除工厂失败，ID: " + id, ex);
                throw;
            }
        }

        public void Refresh()
        {
            try
            {
                LogService.Info("刷新工厂数据...");
                LoadFactories();
                LogService.Info("工厂数据刷新完成");
            }
            catch (Exception ex)
            {
                LogService.Error("刷新工厂数据失败", ex);
                throw;
            }
        }

        public void ExportToExcel()
        {
            try
            {
                LogService.Info("开始导出工厂数据到Excel...");

                if (Factories == null || Factories.Count == 0)
                {
                    LogService.Warning("没有可导出的工厂数据");
                    MessageBox.Show("没有可导出的工厂数据！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx",
                    FileName = $"工厂数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "导出工厂数据",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("工厂数据");

                        string[] headers = new string[]
                        {
                            "工厂编码", "工厂名称", "品牌", "工厂类型", "地址", "联系人",
                            "联系信息", "认证情况", "工厂规模", "员工人数", "生产能力", "备注"
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
                        foreach (var factory in Factories)
                        {
                            worksheet.Cells[row, 1].Value = factory.FactoryCode;
                            worksheet.Cells[row, 2].Value = factory.FactoryName;
                            worksheet.Cells[row, 3].Value = factory.Brand;
                            worksheet.Cells[row, 4].Value = factory.FactoryType;
                            worksheet.Cells[row, 5].Value = factory.Address;
                            worksheet.Cells[row, 6].Value = factory.ContactPerson;
                            worksheet.Cells[row, 7].Value = factory.ContactInfo;
                            worksheet.Cells[row, 8].Value = factory.Certifications;
                            worksheet.Cells[row, 9].Value = factory.Scale;
                            worksheet.Cells[row, 10].Value = factory.EmployeeCount;
                            worksheet.Cells[row, 11].Value = factory.ProductionCapacity;
                            worksheet.Cells[row, 12].Value = factory.Description;
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
                            LogService.Info($"工厂数据导出成功，共导出 {Factories.Count} 条记录，文件: {finalPath}");
                            MessageBox.Show($"成功导出 {Factories.Count} 个工厂的工作表！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("导出工厂数据到Excel失败", ex);
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}