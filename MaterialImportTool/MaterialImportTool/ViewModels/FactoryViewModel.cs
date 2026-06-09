using MaterialImportTool.Models;
using MaterialImportTool.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System;
using System.IO;
using OfficeOpenXml;

namespace MaterialImportTool.ViewModels
{
    public class FactoryViewModel : ViewModelBase
    {
        private readonly DbService _dbService;
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<Factory> _factories;
        public ObservableCollection<Factory> Factories
        {
            get => _factories;
            set => SetProperty(ref _factories, value);
        }

        private Factory _selectedFactory;
        public Factory SelectedFactory
        {
            get => _selectedFactory;
            set => SetProperty(ref _selectedFactory, value);
        }

        private Factory _editingFactory;
        public Factory EditingFactory
        {
            get => _editingFactory;
            set => SetProperty(ref _editingFactory, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                SearchFactories();
            }
        }

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set => SetProperty(ref _isDialogOpen, value);
        }

        public IRelayCommand AddFactoryCommand { get; }
        public IRelayCommand EditFactoryCommand { get; }
        public IRelayCommand DeleteFactoryCommand { get; }
        public IRelayCommand SaveFactoryCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand ExportCommand { get; }
        public IRelayCommand BackToHomeCommand { get; }

        public FactoryViewModel(DbService dbService, MainViewModel mainViewModel)
        {
            _dbService = dbService;
            _mainViewModel = mainViewModel;
            Factories = new ObservableCollection<Factory>();

            AddFactoryCommand = new RelayCommand(AddFactory);
            EditFactoryCommand = new RelayCommand(EditFactory);
            DeleteFactoryCommand = new RelayCommand(DeleteFactory);
            SaveFactoryCommand = new RelayCommand(SaveFactory);
            CancelCommand = new RelayCommand(Cancel);
            ExportCommand = new RelayCommand(ExportFactories);
            BackToHomeCommand = new RelayCommand(() => _mainViewModel.ShowHomeCommand.Execute(null));

            LoadFactories();
        }

        private void LoadFactories()
        {
            Factories.Clear();
            foreach (var factory in _dbService.GetFactories())
            {
                Factories.Add(factory);
            }
        }

        private void SearchFactories()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                LoadFactories();
            }
            else
            {
                Factories.Clear();
                foreach (var factory in _dbService.GetFactories())
                {
                    if (factory.FactoryName.Contains(SearchText) || 
                        factory.FactoryCode.Contains(SearchText))
                    {
                        Factories.Add(factory);
                    }
                }
            }
        }

        private void AddFactory()
        {
            try
            {
                LogService.Info("开始添加新工厂", "FactoryViewModel");
                EditingFactory = new Factory();
                LogService.Info("EditingFactory 初始化完成", "FactoryViewModel");
                IsDialogOpen = true;
                LogService.Info("对话框已打开", "FactoryViewModel");
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "FactoryViewModel");
                MessageBox.Show($"添加工厂失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditFactory()
        {
            if (SelectedFactory != null)
            {
                EditingFactory = new Factory
                {
                    Id = SelectedFactory.Id,
                    FactoryCode = SelectedFactory.FactoryCode,
                    FactoryName = SelectedFactory.FactoryName,
                    FactoryType = SelectedFactory.FactoryType,
                    Address = SelectedFactory.Address,
                    Certifications = SelectedFactory.Certifications,
                    Description = SelectedFactory.Description,
                    Scale = SelectedFactory.Scale,
                    EmployeeCount = SelectedFactory.EmployeeCount,
                    ProductionCapacity = SelectedFactory.ProductionCapacity,
                    ControllingPerson = SelectedFactory.ControllingPerson,
                    ContactPerson = SelectedFactory.ContactPerson,
                    ContactInfo = SelectedFactory.ContactInfo,
                    ContactMethod = SelectedFactory.ContactMethod
                };
                IsDialogOpen = true;
            }
        }

        private void DeleteFactory()
        {
            if (SelectedFactory != null)
            {
                if (MessageBox.Show($"确定要删除工厂 {SelectedFactory.FactoryName} 吗？", "确认删除", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _dbService.DeleteFactory(SelectedFactory.Id);
                    LoadFactories();
                    MessageBox.Show("删除成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void SaveFactory()
        {
            if (string.IsNullOrWhiteSpace(EditingFactory.FactoryCode))
            {
                MessageBox.Show("请输入工厂编码！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(EditingFactory.FactoryName))
            {
                MessageBox.Show("请输入工厂名称！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _dbService.SaveFactory(EditingFactory);
            LoadFactories();
            IsDialogOpen = false;
            MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel()
        {
            IsDialogOpen = false;
            EditingFactory = null;
        }

        private void ExportFactories()
        {
            LogService.LogMethodEntry("ExportFactories", "FactoryViewModel");
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv";
            dialog.FileName = "工厂数据_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LogService.Info($"开始导出工厂数据到: {dialog.FileName}", "FactoryViewModel");
                    if (dialog.FileName.EndsWith(".xlsx"))
                    {
                        ExportToExcel(dialog.FileName);
                    }
                    else
                    {
                        ExportToCsv(dialog.FileName);
                    }
                    LogService.LogExportOperation("工厂数据", dialog.FileName, Factories.Count, "FactoryViewModel");
                    MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogService.Error(ex, "FactoryViewModel");
                    MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            LogService.LogMethodExit("ExportFactories", "FactoryViewModel");
        }

        private void ExportToExcel(string filePath)
        {
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("工厂数据");
                
                worksheet.Cells["A1"].Value = "工厂编码";
                worksheet.Cells["B1"].Value = "工厂名称";
                worksheet.Cells["C1"].Value = "工厂类型";
                worksheet.Cells["D1"].Value = "地址";
                worksheet.Cells["E1"].Value = "认证情况";
                worksheet.Cells["F1"].Value = "备注";
                worksheet.Cells["G1"].Value = "工厂规模";
                worksheet.Cells["H1"].Value = "员工人数";
                worksheet.Cells["I1"].Value = "生产能力";
                worksheet.Cells["J1"].Value = "负责人";
                worksheet.Cells["K1"].Value = "联系人";
                worksheet.Cells["L1"].Value = "联系信息";
                worksheet.Cells["M1"].Value = "联系方式";

                int row = 2;
                foreach (var factory in Factories)
                {
                    worksheet.Cells[$"A{row}"].Value = factory.FactoryCode;
                    worksheet.Cells[$"B{row}"].Value = factory.FactoryName;
                    worksheet.Cells[$"C{row}"].Value = factory.FactoryType;
                    worksheet.Cells[$"D{row}"].Value = factory.Address;
                    worksheet.Cells[$"E{row}"].Value = factory.Certifications;
                    worksheet.Cells[$"F{row}"].Value = factory.Description;
                    worksheet.Cells[$"G{row}"].Value = factory.Scale;
                    worksheet.Cells[$"H{row}"].Value = factory.EmployeeCount;
                    worksheet.Cells[$"I{row}"].Value = factory.ProductionCapacity;
                    worksheet.Cells[$"J{row}"].Value = factory.ControllingPerson;
                    worksheet.Cells[$"K{row}"].Value = factory.ContactPerson;
                    worksheet.Cells[$"L{row}"].Value = factory.ContactInfo;
                    worksheet.Cells[$"M{row}"].Value = factory.ContactMethod;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void ExportToCsv(string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("工厂编码,工厂名称,工厂类型,地址,认证情况,备注,工厂规模,员工人数,生产能力,负责人,联系人,联系信息,联系方式");
                foreach (var factory in Factories)
                {
                    writer.WriteLine($"{factory.FactoryCode},{factory.FactoryName},{factory.FactoryType},{factory.Address},{factory.Certifications},{factory.Description},{factory.Scale},{factory.EmployeeCount},{factory.ProductionCapacity},{factory.ControllingPerson},{factory.ContactPerson},{factory.ContactInfo},{factory.ContactMethod}");
                }
            }
        }
    }
}