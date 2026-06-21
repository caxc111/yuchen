using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;

namespace FactoryProductManager.Services
{
    /// <summary>
    /// 窗口位置保护服务，确保所有弹窗始终显示在屏幕可见区域内
    /// </summary>
    public static class WindowPositionService
    {
        /// <summary>
        /// 确保窗口始终在屏幕可见区域内
        /// </summary>
        public static void EnsureWindowOnScreen(Window window)
        {
            if (window == null) return;

            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                var screen = Screen.FromHandle(hwnd);
                var workingArea = screen.WorkingArea;

                bool moved = false;
                double left = window.Left;
                double top = window.Top;
                double width = window.ActualWidth;
                double height = window.ActualHeight;

                if (left < workingArea.Left)
                {
                    left = workingArea.Left;
                    moved = true;
                }
                if (top < workingArea.Top)
                {
                    top = workingArea.Top;
                    moved = true;
                }
                if (left + width > workingArea.Right)
                {
                    left = workingArea.Right - width;
                    moved = true;
                }
                if (top + height > workingArea.Bottom)
                {
                    top = workingArea.Bottom - height;
                    moved = true;
                }

                if (moved)
                {
                    window.Left = left;
                    window.Top = top;
                    LogService.Debug($"[WindowPositionService] 窗口 '{window.Title}' 位置已调整到屏幕内: Left={left:F0}, Top={top:F0}");
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"[WindowPositionService] EnsureWindowOnScreen 出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 为窗口添加位置保护（自动在 Loaded 和 LocationChanged 时检查）
        /// </summary>
        public static void AddPositionProtection(Window window)
        {
            if (window == null) return;

            bool isFirstLoad = true;

            window.Loaded += (s, e) =>
            {
                if (isFirstLoad)
                {
                    isFirstLoad = false;
                    EnsureWindowOnScreen(window);
                }
            };

            window.LocationChanged += (s, e) =>
            {
                EnsureWindowOnScreen(window);
            };
        }
    }
}
