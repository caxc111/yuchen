using System;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace FactoryProductManager.Views
{
    public partial class ImageViewerWindow : Window
    {
        private readonly string _imagePath;

        public ImageViewerWindow(string imagePath, string title)
        {
            _imagePath = imagePath;
            Title = title;
            InitializeComponent();
            TitleTextBlock.Text = title;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_imagePath, UriKind.Absolute);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                ViewerImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载图片：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            UpdateMaximizeIcon();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UpdateMaximizeIcon()
        {
            if (MaximizeButton.Content is PackIcon icon)
            {
                icon.Kind = WindowState == WindowState.Maximized
                    ? PackIconKind.WindowRestore
                    : PackIconKind.WindowMaximize;
            }
        }
    }
}


