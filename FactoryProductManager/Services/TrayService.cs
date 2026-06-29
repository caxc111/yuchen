using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Forms;
using FactoryProductManager.Services;

namespace FactoryProductManager.Services
{
    /// <summary>
    /// 系统托盘图标服务 - 管理托盘图标和上下文菜单
    /// </summary>
    public class TrayService : IDisposable
    {
        private static TrayService? _instance;
        public static TrayService Instance => _instance ??= new TrayService();

        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private readonly Dictionary<Window, ToolStripMenuItem> _windowMenuItems = new();
        private bool _disposed;

        public event EventHandler? ShowMainWindowRequested;

        private TrayService()
        {
        }

        public void Initialize(Window mainWindow)
        {
            if (_notifyIcon != null) return;

            LogService.Info("初始化系统托盘图标...");

            // 创建托盘图标
            _notifyIcon = new NotifyIcon
            {
                Text = "宇程科技信息系统",
                Visible = true,
                Icon = CreateAppIcon(),
            };

            // 创建上下文菜单
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add(CreateShowMainWindowItem());
            _contextMenu.Items.Add(new ToolStripSeparator());

            var showAllItem = new ToolStripMenuItem("显示所有窗口");
            showAllItem.Click += (s, e) => ShowAllWindows();
            _contextMenu.Items.Add(showAllItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitApplication();
            _contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);

            LogService.Info("系统托盘图标初始化完成");
        }

        private ToolStripMenuItem CreateShowMainWindowItem()
        {
            var item = new ToolStripMenuItem("显示主窗口");
            item.Click += (s, e) => ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
            return item;
        }

        private Icon CreateAppIcon()
        {
            // 创建一个 16x16 的图标
            int size = 16;
            using var bitmap = new Bitmap(size, size);
            using var graphics = Graphics.FromImage(bitmap);

            // 设置高质量渲染
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // 绘制背景（蓝色圆角矩形）
            using var bgBrush = new SolidBrush(Color.FromArgb(0x2B, 0x57, 0x9C)); // Material Blue
            graphics.FillEllipse(bgBrush, 0, 0, size - 1, size - 1);

            // 绘制字母"Y"（代表宇程）
            using var font = new Font("Arial", 8, System.Drawing.FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString("Y", font, textBrush, new RectangleF(0, 0, size, size), format);

            return Icon.FromHandle(bitmap.GetHicon());
        }

        /// <summary>
        /// 注册窗口到托盘
        /// </summary>
        public void RegisterWindow(Window window, string title)
        {
            if (_notifyIcon == null || _contextMenu == null) return;
            if (_windowMenuItems.ContainsKey(window)) return;

            LogService.Info($"注册窗口到托盘: {title}");

            var menuItem = new ToolStripMenuItem(title);
            menuItem.Click += (s, e) =>
            {
                ShowWindow(window);
                window.Activate();
            };

            // 在"显示所有窗口"之前插入
            int insertIndex = _contextMenu.Items.Count - 3; // 减3是因为有separator和exit
            if (insertIndex < 2) insertIndex = 2;
            _contextMenu.Items.Insert(insertIndex, menuItem);

            _windowMenuItems[window] = menuItem;

            // 监听窗口状态变化
            window.StateChanged += Window_StateChanged;
            window.Closing += Window_Closing;
        }

        /// <summary>
        /// 更新窗口标题
        /// </summary>
        public void UpdateWindowTitle(Window window, string newTitle)
        {
            if (_windowMenuItems.TryGetValue(window, out var menuItem))
            {
                menuItem.Text = newTitle;
            }
        }

        /// <summary>
        /// 注销窗口
        /// </summary>
        public void UnregisterWindow(Window window)
        {
            if (!_windowMenuItems.TryGetValue(window, out var menuItem)) return;

            LogService.Info($"注销托盘窗口");

            _contextMenu?.Items.Remove(menuItem);
            _windowMenuItems.Remove(window);

            window.StateChanged -= Window_StateChanged;
            window.Closing -= Window_Closing;
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (sender is not Window window) return;

            if (window.WindowState == WindowState.Minimized)
            {
                // 最小化时隐藏窗口，但托盘图标保持
                window.Hide();
                LogService.Info($"窗口最小化到托盘");
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is Window window)
            {
                UnregisterWindow(window);
            }
        }

        private void ShowWindow(Window window)
        {
            // 先设置状态再显示，确保窗口正确恢复
            window.WindowState = WindowState.Normal;
            window.Show();
            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();
        }

        private void ShowAllWindows()
        {
            foreach (var window in _windowMenuItems.Keys)
            {
                if (window.IsVisible == false)
                {
                    ShowWindow(window);
                }
            }
        }

        public void SetTrayIcon(Icon icon)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Icon = icon;
            }
        }

        public void SetTrayTooltip(string tooltip)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = tooltip.Length > 63 ? tooltip.Substring(0, 63) : tooltip;
            }
        }

        private void ExitApplication()
        {
            LogService.Info("用户从托盘菜单退出应用");
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            _contextMenu?.Dispose();
            _contextMenu = null;
            _windowMenuItems.Clear();
        }
    }
}
