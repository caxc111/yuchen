using MaterialImportTool.Models;
using MaterialImportTool.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MaterialImportTool.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private readonly DbService _dbService;
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<Product> _products = new();
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        private ObservableCollection<Factory> _factories = new();
        public ObservableCollection<Factory> Factories
        {
            get => _factories;
            set => SetProperty(ref _factories, value);
        }

        private Product? _selectedProduct;
        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        private Product? _editingProduct;
        public Product? EditingProduct
        {
            get => _editingProduct;
            set => SetProperty(ref _editingProduct, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                SearchProducts();
            }
        }

        private ObservableCollection<OcrResultItem> _ocrResult = new();
        public ObservableCollection<OcrResultItem> OcrResult
        {
            get => _ocrResult;
            set => SetProperty(ref _ocrResult, value);
        }

        private string _selectedFactoryCode = string.Empty;
        public string SelectedFactoryCode
        {
            get => _selectedFactoryCode;
            set => SetProperty(ref _selectedFactoryCode, value);
        }

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        private bool _isExcelImportOpen;
        public bool IsExcelImportOpen
        {
            get => _isExcelImportOpen;
            set => SetProperty(ref _isExcelImportOpen, value);
        }

        public ObservableCollection<string> CategoryCodes { get; } = new();
        public ObservableCollection<string> SubCategoryCodes { get; } = new();

        public IRelayCommand AddProductCommand { get; }
        public IRelayCommand EditProductCommand { get; }
        public IRelayCommand DeleteProductCommand { get; }
        public IRelayCommand SaveProductCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand ExportCommand { get; }
        public IRelayCommand BackToHomeCommand { get; }
        public IRelayCommand ScreenshotCommand { get; }
        public IRelayCommand ExcelImportCommand { get; }
        public IRelayCommand GenerateCodeCommand { get; }
        public IRelayCommand BatchImportCommand { get; }
        public IRelayCommand CategoryChangedCommand { get; }

        public ProductViewModel(DbService dbService, MainViewModel mainViewModel)
        {
            _dbService = dbService;
            _mainViewModel = mainViewModel;

            AddProductCommand = new RelayCommand(AddProduct);
            EditProductCommand = new RelayCommand(EditProduct);
            DeleteProductCommand = new RelayCommand(DeleteProduct);
            SaveProductCommand = new RelayCommand(SaveProduct);
            CancelCommand = new RelayCommand(Cancel);
            ExportCommand = new RelayCommand(ExportProducts);
            BackToHomeCommand = new RelayCommand(() => _mainViewModel.ShowHomeCommand.Execute(null));
            ScreenshotCommand = new RelayCommand(TakeScreenshot);
            ExcelImportCommand = new RelayCommand(OpenExcelImport);
            GenerateCodeCommand = new RelayCommand(GenerateCode);
            BatchImportCommand = new RelayCommand(BatchImportProducts);
            CategoryChangedCommand = new RelayCommand(OnCategoryChanged);

            LoadProducts();
            LoadFactories();
            LoadCategories();
        }

        private void LoadProducts()
        {
            Products.Clear();
            foreach (var product in _dbService.GetProducts())
            {
                Products.Add(product);
            }
        }

        private void LoadFactories()
        {
            Factories.Clear();
            Factories.Add(new Factory { FactoryCode = string.Empty, FactoryName = "请选择工厂" });
            foreach (var factory in _dbService.GetFactories())
            {
                Factories.Add(factory);
            }
        }

        private void LoadCategories()
        {
            CategoryCodes.Clear();
            CategoryCodes.Add("请选择");
            CategoryCodes.Add("SM-柜体木饰面");
            CategoryCodes.Add("MD-木地板");
            CategoryCodes.Add("DT-地毯");
            CategoryCodes.Add("CZ-瓷砖");
            CategoryCodes.Add("SC-石材");
            CategoryCodes.Add("CW-厨卫陶瓷");
            CategoryCodes.Add("WJ-厨卫五金");
            CategoryCodes.Add("HM-户内门");
            CategoryCodes.Add("DJ-灯具开关");
            CategoryCodes.Add("DQ-电器");
        }

        private void OnCategoryChanged()
        {
            SubCategoryCodes.Clear();
            SubCategoryCodes.Add("请选择");

            if (EditingProduct == null || string.IsNullOrWhiteSpace(EditingProduct.Category))
            {
                return;
            }

            var categoryCode = EditingProduct.Category.Split('-')[0];
            switch (categoryCode)
            {
                case "SM":
                    SubCategoryCodes.Add("SP-实木木饰面面板");
                    SubCategoryCodes.Add("KJ-科技木皮饰面面板");
                    SubCategoryCodes.Add("HY-混油饰面面板");
                    SubCategoryCodes.Add("SJ-三聚氰胺饰面面板");
                    SubCategoryCodes.Add("PT-PET膜饰面面板");
                    SubCategoryCodes.Add("QT-其他饰面");
                    break;
                case "MD":
                    SubCategoryCodes.Add("SM-实木地板");
                    SubCategoryCodes.Add("SF-实木复合地板");
                    SubCategoryCodes.Add("QH-强化复合地板");
                    SubCategoryCodes.Add("SP-SPC(石塑)");
                    SubCategoryCodes.Add("LT-LVT");
                    SubCategoryCodes.Add("WP-WPC(木塑)");
                    SubCategoryCodes.Add("QT-塑胶地板及其他");
                    break;
                case "DT":
                    SubCategoryCodes.Add("BL-丙纶满铺毯");
                    SubCategoryCodes.Add("QL-晴纶满铺毯");
                    SubCategoryCodes.Add("DL-涤纶满铺毯");
                    SubCategoryCodes.Add("YM-羊毛/羊毛混纺满铺毯");
                    SubCategoryCodes.Add("ZQ-植物纤维(剑麻/黄麻)");
                    break;
                case "CZ":
                    SubCategoryCodes.Add("LM-亮面砖");
                    SubCategoryCodes.Add("YG-亚光砖");
                    SubCategoryCodes.Add("JL-肌理砖/手工砖/仿古砖");
                    SubCategoryCodes.Add("MK-马赛克/小砖");
                    SubCategoryCodes.Add("YB-岩板/大规格瓷砖");
                    break;
                case "SC":
                    SubCategoryCodes.Add("DL-大理石");
                    SubCategoryCodes.Add("HY-花岗岩");
                    SubCategoryCodes.Add("SY-砂岩");
                    SubCategoryCodes.Add("BY-板岩");
                    break;
            }
        }

        private void SearchProducts()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                LoadProducts();
                return;
            }

            Products.Clear();
            foreach (var product in _dbService.GetProducts())
            {
                var matchesMyCode = !string.IsNullOrWhiteSpace(product.MyProductCode) &&
                    product.MyProductCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

                if (product.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    product.FactoryProductCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    matchesMyCode)
                {
                    Products.Add(product);
                }
            }
        }

        private void AddProduct()
        {
            EditingProduct = new Product();
            SelectedFactoryCode = string.Empty;
            SubCategoryCodes.Clear();
            SubCategoryCodes.Add("请选择");
            IsDialogOpen = true;
        }

        private void EditProduct()
        {
            if (SelectedProduct == null)
            {
                return;
            }

            EditingProduct = new Product
            {
                Id = SelectedProduct.Id,
                FactoryProductCode = SelectedProduct.FactoryProductCode,
                MyProductCode = SelectedProduct.MyProductCode,
                ProductName = SelectedProduct.ProductName,
                Brand = SelectedProduct.Brand,
                Specification = SelectedProduct.Specification,
                Texture = SelectedProduct.Texture,
                Process = SelectedProduct.Process,
                UsageScenario = SelectedProduct.UsageScenario,
                Certifications = SelectedProduct.Certifications,
                Category = SelectedProduct.Category,
                SubCategory = SelectedProduct.SubCategory,
                ImageUrl = SelectedProduct.ImageUrl,
                FactoryId = SelectedProduct.FactoryId,
                FactoryCode = SelectedProduct.FactoryCode,
                FactoryName = SelectedProduct.FactoryName
            };

            SelectedFactoryCode = SelectedProduct.FactoryCode ?? string.Empty;
            OnCategoryChanged();
            IsDialogOpen = true;
        }

        private void DeleteProduct()
        {
            if (SelectedProduct == null)
            {
                return;
            }

            if (MessageBox.Show($"确定要删除产品 {SelectedProduct.ProductName} 吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _dbService.DeleteProduct(SelectedProduct.Id);
                LoadProducts();
                MessageBox.Show("删除成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveProduct()
        {
            if (EditingProduct == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingProduct.FactoryProductCode))
            {
                MessageBox.Show("请输入工厂产品编码！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingProduct.ProductName))
            {
                MessageBox.Show("请输入产品名称！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingProduct.Category) || EditingProduct.Category == "请选择")
            {
                MessageBox.Show("请选择产品分类！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(SelectedFactoryCode) && SelectedFactoryCode != "请选择工厂")
            {
                var factory = _dbService.GetFactoryByCode(SelectedFactoryCode);
                if (factory != null)
                {
                    EditingProduct.FactoryId = factory.Id;
                    EditingProduct.FactoryCode = factory.FactoryCode;
                    EditingProduct.FactoryName = factory.FactoryName;
                }
            }

            _dbService.SaveProduct(EditingProduct);
            LoadProducts();
            IsDialogOpen = false;
            MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel()
        {
            IsDialogOpen = false;
            EditingProduct = null;
        }

        private void GenerateCode()
        {
            if (EditingProduct == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingProduct.Category) || EditingProduct.Category == "请选择")
            {
                MessageBox.Show("请先选择产品分类！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var categoryCode = EditingProduct.Category.Split('-')[0];
            var subCode = "001";

            if (!string.IsNullOrWhiteSpace(EditingProduct.SubCategory) && EditingProduct.SubCategory != "请选择")
            {
                subCode = EditingProduct.SubCategory.Split('-')[0];
            }

            var sequence = _dbService.GetNextProductSequence();
            var code = $"S{sequence:D3}-{categoryCode}-{subCode}-{sequence:D3}";

            while (_dbService.IsProductCodeExists(code))
            {
                sequence++;
                code = $"S{sequence:D3}-{categoryCode}-{subCode}-{sequence:D3}";
            }

            EditingProduct.MyProductCode = code;
        }

        private void TakeScreenshot()
        {
            MessageBox.Show("截图功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenExcelImport()
        {
            IsExcelImportOpen = true;
        }

        private void BatchImportProducts()
        {
            LogService.LogMethodEntry("BatchImportProducts", "ProductViewModel");
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel文件 (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LogService.Info($"开始从Excel导入产品数据: {dialog.FileName}", "ProductViewModel");
                    ImportFromExcel(dialog.FileName);
                    LoadProducts();
                    IsExcelImportOpen = false;
                    LogService.LogImportOperation("产品数据", dialog.FileName, Products.Count, "ProductViewModel");
                    MessageBox.Show("批量导入成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogService.Error(ex, "ProductViewModel");
                    MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            LogService.LogMethodExit("BatchImportProducts", "ProductViewModel");
        }

        private void ImportFromExcel(string filePath)
        {
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            if (worksheet.Dimension == null)
            {
                return;
            }

            var rowCount = worksheet.Dimension.End.Row;
            for (var row = 2; row <= rowCount; row++)
            {
                var product = new Product
                {
                    FactoryProductCode = worksheet.Cells[$"A{row}"].Text,
                    ProductName = worksheet.Cells[$"C{row}"].Text,
                    Brand = worksheet.Cells[$"D{row}"].Text,
                    Specification = worksheet.Cells[$"E{row}"].Text,
                    Texture = worksheet.Cells[$"F{row}"].Text,
                    Process = worksheet.Cells[$"G{row}"].Text,
                    UsageScenario = worksheet.Cells[$"H{row}"].Text,
                    Certifications = worksheet.Cells[$"I{row}"].Text,
                    Category = worksheet.Cells[$"J{row}"].Text
                };

                var factoryCode = worksheet.Cells[$"A{row}"].Text;
                if (!string.IsNullOrWhiteSpace(factoryCode))
                {
                    var factory = _dbService.GetFactoryByCode(factoryCode);
                    if (factory != null)
                    {
                        product.FactoryId = factory.Id;
                        product.FactoryCode = factory.FactoryCode;
                        product.FactoryName = factory.FactoryName;
                    }
                }

                var categoryText = product.Category;
                if (!string.IsNullOrWhiteSpace(categoryText) && !categoryText.Contains('-'))
                {
                    product.Category = GetCategoryByName(categoryText);
                }

                var normalizedCategory = product.Category ?? string.Empty;
                var sequence = _dbService.GetNextProductSequence();
                var catCode = normalizedCategory.Contains('-') ? normalizedCategory.Split('-')[0] : normalizedCategory;
                product.MyProductCode = $"S{sequence:D3}-{catCode}-001-{sequence:D3}";

                _dbService.SaveProduct(product);
            }
        }

        private string GetCategoryByName(string name)
        {
            var mapping = new Dictionary<string, string>
            {
                { "柜体木饰面", "SM-柜体木饰面" },
                { "木地板", "MD-木地板" },
                { "地毯", "DT-地毯" },
                { "瓷砖", "CZ-瓷砖" },
                { "石材", "SC-石材" },
                { "厨卫陶瓷", "CW-厨卫陶瓷" },
                { "厨卫五金", "WJ-厨卫五金" },
                { "户内门", "HM-户内门" },
                { "灯具开关", "DJ-灯具开关" },
                { "电器", "DQ-电器" }
            };

            return mapping.TryGetValue(name, out var code) ? code : name;
        }

        private void ExportProducts()
        {
            LogService.LogMethodEntry("ExportProducts", "ProductViewModel");
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv",
                FileName = "产品数据_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LogService.Info($"开始导出产品数据到: {dialog.FileName}", "ProductViewModel");
                    if (dialog.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportToExcel(dialog.FileName);
                    }
                    else
                    {
                        ExportToCsv(dialog.FileName);
                    }

                    LogService.LogExportOperation("产品数据", dialog.FileName, Products.Count, "ProductViewModel");
                    MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogService.Error(ex, "ProductViewModel");
                    MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            LogService.LogMethodExit("ExportProducts", "ProductViewModel");
        }

        private void ExportToExcel(string filePath)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("产品数据");

            worksheet.Cells["A1"].Value = "工厂产品编码";
            worksheet.Cells["B1"].Value = "宇辰产品编码";
            worksheet.Cells["C1"].Value = "产品名称";
            worksheet.Cells["D1"].Value = "品牌";
            worksheet.Cells["E1"].Value = "规格";
            worksheet.Cells["F1"].Value = "材质";
            worksheet.Cells["G1"].Value = "工艺";
            worksheet.Cells["H1"].Value = "使用场景";
            worksheet.Cells["I1"].Value = "认证情况";
            worksheet.Cells["J1"].Value = "分类";
            worksheet.Cells["K1"].Value = "二级分类";
            worksheet.Cells["L1"].Value = "工厂编码";
            worksheet.Cells["M1"].Value = "工厂名称";

            var row = 2;
            foreach (var product in Products)
            {
                worksheet.Cells[$"A{row}"].Value = product.FactoryProductCode;
                worksheet.Cells[$"B{row}"].Value = product.MyProductCode;
                worksheet.Cells[$"C{row}"].Value = product.ProductName;
                worksheet.Cells[$"D{row}"].Value = product.Brand;
                worksheet.Cells[$"E{row}"].Value = product.Specification;
                worksheet.Cells[$"F{row}"].Value = product.Texture;
                worksheet.Cells[$"G{row}"].Value = product.Process;
                worksheet.Cells[$"H{row}"].Value = product.UsageScenario;
                worksheet.Cells[$"I{row}"].Value = product.Certifications;
                worksheet.Cells[$"J{row}"].Value = product.Category;
                worksheet.Cells[$"K{row}"].Value = product.SubCategory;
                worksheet.Cells[$"L{row}"].Value = product.FactoryCode;
                worksheet.Cells[$"M{row}"].Value = product.FactoryName;
                row++;
            }

            worksheet.Cells.AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        private void ExportToCsv(string filePath)
        {
            using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("工厂产品编码,宇辰产品编码,产品名称,品牌,规格,材质,工艺,使用场景,认证情况,分类,二级分类,工厂编码,工厂名称");
            foreach (var product in Products)
            {
                writer.WriteLine($"{product.FactoryProductCode},{product.MyProductCode},{product.ProductName},{product.Brand},{product.Specification},{product.Texture},{product.Process},{product.UsageScenario},{product.Certifications},{product.Category},{product.SubCategory},{product.FactoryCode},{product.FactoryName}");
            }
        }
    }
}
