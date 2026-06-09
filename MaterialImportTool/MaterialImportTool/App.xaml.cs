using MaterialImportTool.Services;
using System;
using System.Windows;

namespace MaterialImportTool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LogService.Info("========== 程序启动 ==========", "App");
            LogService.Info($"程序路径: {AppDomain.CurrentDomain.BaseDirectory}", "App");
            LogService.Info($"启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", "App");
            LogService.Info("日志系统初始化完成", "App");
            
            try
            {
                LogService.Info("开始初始化主窗口...", "App");
                var mainWindow = new MainWindow();
                LogService.Info("主窗口初始化完成", "App");
                mainWindow.Show();
                LogService.Info("主窗口已显示", "App");
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "App");
                MessageBox.Show($"程序启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogService.Info($"程序退出，退出码: {e.ApplicationExitCode}", "App");
            LogService.Info("========== 程序关闭 ==========", "App");
            base.OnExit(e);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogService.Error(e.Exception, "App");
            MessageBox.Show($"程序运行异常: {e.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}