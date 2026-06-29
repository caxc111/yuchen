#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FactoryProductManager.Services;
using FactoryProductManager.Views;

namespace FactoryProductManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            RegisterGlobalExceptionHandlers();

            try
            {
                // 强制初始化日志服务并验证
                var logDir = LogService.LogDirectory;
                var logPath = LogService.CurrentLogFilePath;
                try
                {
                    System.IO.Directory.CreateDirectory(logDir); // 确保目录存在
                    System.IO.File.AppendAllText(logPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [BOOT] 日志目录={logDir}{Environment.NewLine}" +
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [BOOT] 日志文件={logPath}{Environment.NewLine}",
                        System.Text.Encoding.UTF8);
                }
                catch (Exception initEx)
                {
                    // 备用：写到项目根目录 debug.log
                    var fallbackLog = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(
                            System.Reflection.Assembly.GetExecutingAssembly().Location)!,
                        "..", "..", "..", "..", "..", "debug.log");
                    fallbackLog = System.IO.Path.GetFullPath(fallbackLog);
                    System.IO.File.AppendAllText(fallbackLog,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [BOOT-FALLBACK] 日志目录={logDir}, 错误={initEx.Message}{Environment.NewLine}",
                        System.Text.Encoding.UTF8);
                }

                LogService.Info("========== 应用程序启动 ==========");
                LogService.Info("开始初始化应用程序组件...");

                base.OnStartup(e);

                LogService.Info("创建主窗口...");
                var mainWindow = new MainWindow();

                LogService.Info("显示主窗口...");
                mainWindow.Show();

                // 初始化系统托盘
                TrayService.Instance.Initialize(mainWindow);
                TrayService.Instance.ShowMainWindowRequested += (s, e) =>
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                };

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
                TrayService.Instance.Dispose();
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

    public static class WindowDragBehavior
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(WindowDragBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element) return;

            if ((bool)e.NewValue)
            {
                element.PreviewMouseLeftButtonDown += Element_PreviewMouseLeftButtonDown;
            }
            else
            {
                element.PreviewMouseLeftButtonDown -= Element_PreviewMouseLeftButtonDown;
            }
        }

        private static void Element_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (e.ClickCount != 1 || sender is not UIElement element) return;

            var hit = e.OriginalSource as DependencyObject;
            if (hit != null && IsInteractiveElement(hit)) return;

            var window = GetWindow(element);
            window?.DragMove();
        }

        private static bool IsInteractiveElement(DependencyObject obj)
        {
            for (int i = 0; i < 30 && obj != null; i++, obj = VisualTreeHelper.GetParent(obj))
            {
                if (obj is System.Windows.Controls.Primitives.ButtonBase) return true;
                if (obj is System.Windows.Controls.ComboBox) return true;
                if (obj is System.Windows.Controls.ListBoxItem) return true;
                if (obj is System.Windows.Controls.CheckBox) return true;
                if (obj is System.Windows.Controls.RadioButton) return true;
                if (obj is System.Windows.Controls.TextBox) return true;
                if (obj is System.Windows.Controls.Primitives.TextBoxBase) return true;
                if (obj is System.Windows.Controls.PasswordBox) return true;
                if (obj is System.Windows.Controls.Primitives.ScrollBar) return true;
                if (obj is System.Windows.Controls.Slider) return true;
            }
            return false;
        }

        private static Window? GetWindow(DependencyObject obj)
        {
            for (int i = 0; i < 30 && obj != null; i++, obj = VisualTreeHelper.GetParent(obj))
            {
                if (obj is Window w) return w;
            }
            return null;
        }
    }
}
