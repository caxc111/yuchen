using System;
using System.Windows;
using FactoryProductManager.Services;

namespace FactoryProductManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                LogService.LogApplicationStart();
                
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                DispatcherUnhandledException += App_DispatcherUnhandledException;
                
                base.OnStartup(e);
                
                LogService.LogInfo("应用程序启动成功");
            }
            catch (Exception ex)
            {
                LogService.LogError("应用程序启动失败", ex);
                MessageBox.Show($"应用程序启动失败: {ex.Message}\n\n请查看日志文件获取详细信息", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void ImportFactoryDataFromExcel()
        {
            try
            {
                string excelPath = @"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\工厂信息.xls";
                
                if (!System.IO.File.Exists(excelPath))
                {
                    LogService.LogInfo("Excel文件不存在，跳过导入");
                    return;
                }

                var dbService = new DbService();
                var existingFactories = dbService.GetFactories();
                
                if (existingFactories.Count > 0)
                {
                    LogService.LogInfo($"数据库中已有 {existingFactories.Count} 条工厂数据，跳过导入");
                    return;
                }

                var excelService = new ExcelImportService(dbService);
                int importedCount = excelService.ImportFactoriesFromExcel(excelPath);
                
                LogService.LogInfo($"成功从Excel导入 {importedCount} 条工厂数据");
            }
            catch (Exception ex)
            {
                LogService.LogError("导入Excel工厂数据失败", ex);
                MessageBox.Show($"导入工厂数据失败: {ex.Message}\n\n请查看日志文件获取详细信息", "导入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                LogService.LogInfo($"应用程序退出，退出码: {e.ApplicationExitCode}");
                LogService.LogApplicationExit();
            }
            catch
            {
            }
            base.OnExit(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogService.LogError("未处理的异常", ex);
            
            if (!e.IsTerminating)
            {
                MessageBox.Show($"发生未处理的异常: {ex?.Message}\n\n请查看日志文件获取详细信息", "运行时错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogService.LogError("UI线程未处理异常", e.Exception);
            MessageBox.Show($"UI线程发生异常: {e.Exception.Message}\n\n请查看日志文件获取详细信息", "UI错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}