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
    public class ProductManagementViewModel : ViewModelBase
    {
        private readonly DbService _dbService;
        private string _searchKeyword = string.Empty;

        public ObservableCollection<Product> Products { get; }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ProductManagementViewModel()
        {
            _dbService = new DbService();
            Products = new ObservableCollection<Product>();
            LoadProducts();
        }

        public void Refresh()
        {
            LoadProducts(SearchKeyword);
        }

        public void AddProduct(Product product)
        {
            product.CreatedAt = DateTime.Now;
            product.UpdatedAt = DateTime.Now;
            product.Id = _dbService.AddProduct(product);
            Products.Add(product);
        }

        public void UpdateProduct(Product product)
        {
            product.UpdatedAt = DateTime.Now;
            _dbService.UpdateProduct(product);

            var existing = Products.FirstOrDefault(item => item.Id == product.Id);
            if (existing != null)
            {
                var index = Products.IndexOf(existing);
                Products[index] = product;
            }
        }

        public void DeleteProduct(int id)
        {
            var existing = Products.FirstOrDefault(item => item.Id == id);
            _dbService.DeleteProduct(id);
            if (existing != null)
            {
                Products.Remove(existing);
            }
        }

        public void ExportToExcel()
        {
            if (Products.Count == 0)
            {
                MessageBox.Show("没有可导出的产品数据！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel文件 (*.xlsx)|*.xlsx",
                FileName = $"产品数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "导出产品数据",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("产品数据");
            string[] headers = { "业态", "产品编码", "户型", "面积（㎡）", "成本合价（元）", "销售合价（元）", "平面图", "状态", "创建时间", "更新时间" };

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
                worksheet.Cells[row, 1].Value = product.BusinessType;
                worksheet.Cells[row, 2].Value = product.ProductCode;
                worksheet.Cells[row, 3].Value = product.HouseType;
                worksheet.Cells[row, 4].Value = product.Area;
                worksheet.Cells[row, 5].Value = product.CostTotalPrice;
                worksheet.Cells[row, 6].Value = product.SellingTotalPrice;
                worksheet.Cells[row, 7].Value = product.FloorPlan;
                worksheet.Cells[row, 8].Value = product.IsActive ? "启用" : "停用";
                worksheet.Cells[row, 9].Value = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 10].Value = product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(saveDialog.FileName));
            MessageBox.Show("产品数据导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadProducts(string? keyword = null)
        {
            Products.Clear();
            foreach (var product in _dbService.GetProducts(keyword))
            {
                Products.Add(product);
            }
        }
    }
}
