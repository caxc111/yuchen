using System;
using System.Windows;
using FactoryProductManager.Services;

namespace FactoryProductManager.Helpers
{
    /// <summary>
    /// 窗口托盘帮助类 - 简化窗口与托盘服务的集成
    /// </summary>
    public static class WindowTrayHelper
    {
        /// <summary>
        /// 为窗口启用托盘最小化功能
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="title">托盘菜单中显示的标题（可选，默认使用窗口Title）</param>
        public static void EnableTrayMinimize(this Window window, string? title = null)
        {
            var displayTitle = string.IsNullOrEmpty(title) ? window.Title : title;
            TrayService.Instance.RegisterWindow(window, displayTitle);
        }

        /// <summary>
        /// 更新窗口在托盘中的显示标题
        /// </summary>
        public static void UpdateTrayTitle(this Window window, string newTitle)
        {
            TrayService.Instance.UpdateWindowTitle(window, newTitle);
        }

        /// <summary>
        /// 禁用窗口的托盘最小化功能
        /// </summary>
        public static void DisableTrayMinimize(this Window window)
        {
            TrayService.Instance.UnregisterWindow(window);
        }
    }
}
