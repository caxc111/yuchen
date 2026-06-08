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
                LogService.Info("开始初始化应用程序组件...");
                
                base.OnStartup(e);
                
                LogService.Info("创建主窗口...");
                var mainWindow = new MainWindow();
                
                LogService.Info("显示主窗口...");
                mainWindow.Show();
                
                LogService.Info("应用程序启动完成");
            }
            catch (Exception ex)
            {
                LogService.Error("应用程序启动失败", ex);
                MessageBox.Show($"应用程序启动失败: {ex.Message}\n\n请查看日志文件获取详细信息。", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                LogService.Info("应用程序开始退出...");
                base.OnExit(e);
                LogService.LogApplicationExit();
            }
            catch (Exception ex)
            {
                LogService.Error("应用程序退出时发生错误", ex);
            }
        }
    }
}