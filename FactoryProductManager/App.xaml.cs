#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FactoryProductManager.Services;

namespace FactoryProductManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            RegisterGlobalExceptionHandlers();

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
                ShowFatalError("应用程序启动失败", ex, true);
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

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            LogService.Info("全局异常处理器注册完成");
        }

        private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogService.Error("UI线程未处理异常", e.Exception);
            ShowFatalError("程序发生未处理异常", e.Exception, true);
            e.Handled = true;
            Shutdown(-1);
        }

        private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("发生非 Exception 类型的未处理异常");
            LogService.Error("AppDomain 未处理异常", exception);
            ShowFatalError("程序发生严重异常", exception, true);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogService.Error("Task 未观察异常", e.Exception);
            e.SetObserved();
        }

        private static void ShowFatalError(string title, Exception ex, bool openLogDirectory)
        {
            try
            {
                if (openLogDirectory)
                {
                    LogService.OpenLogDirectory();
                }

                MessageBox.Show(
                    $"{title}: {ex.Message}\n\n会话标识: {LogService.CurrentSessionId}\n日志文件: {LogService.CurrentLogFilePath}\n日志目录已自动打开。",
                    "程序错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
            }
        }
    }
}
