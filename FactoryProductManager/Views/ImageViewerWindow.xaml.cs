using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FactoryProductManager.Services;

namespace FactoryProductManager.Views
{
    public partial class ImageViewerWindow : Window
    {
        private readonly string _imagePath;
        private bool _isOriginalSize = false;
        private double _originalWidth;
        private double _originalHeight;

        public ImageViewerWindow(string imagePath, string materialName = null, string materialCode = null, string specification = null)
        {
            InitializeComponent();
            _imagePath = imagePath;
            TitleText.Text = System.IO.Path.GetFileName(imagePath);

            if (!string.IsNullOrWhiteSpace(materialName) || !string.IsNullOrWhiteSpace(materialCode) || !string.IsNullOrWhiteSpace(specification))
            {
                if (!string.IsNullOrWhiteSpace(materialName))
                    MaterialNameText.Text = materialName;
                else
                    MaterialNameText.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrWhiteSpace(materialCode))
                    MaterialCodeText.Text = $"宇辰编码: {materialCode}";
                else
                    MaterialCodeText.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrWhiteSpace(specification))
                    SpecificationText.Text = $"规格: {specification}";
                else
                    SpecificationText.Visibility = Visibility.Collapsed;

                InfoPanel.Visibility = Visibility.Visible;
            }

            LoadImage();

            WindowPositionService.AddPositionProtection(this);
        }

        private void LoadImage()
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                DisplayImage.Source = bitmap;
                _originalWidth = bitmap.PixelWidth;
                _originalHeight = bitmap.PixelHeight;

                if (_originalWidth > 800 || _originalHeight > 600)
                {
                    OriginalSizeButton.Visibility = Visibility.Visible;
                }

                FitToWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载图片: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void FitToWindow()
        {
            DisplayImage.Width = double.NaN;
            DisplayImage.Height = double.NaN;
            DisplayImage.Stretch = System.Windows.Media.Stretch.Uniform;
            _isOriginalSize = false;
        }

        private void ShowOriginalSize()
        {
            DisplayImage.Width = _originalWidth;
            DisplayImage.Height = _originalHeight;
            DisplayImage.Stretch = System.Windows.Media.Stretch.None;
            _isOriginalSize = true;
        }

        private void OriginalSizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isOriginalSize)
            {
                FitToWindow();
                OriginalSizeButton.Content = "显示原图";
            }
            else
            {
                ShowOriginalSize();
                OriginalSizeButton.Content = "适应窗口";
            }
        }

        private void DisplayImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (_isOriginalSize)
                {
                    FitToWindow();
                    OriginalSizeButton.Content = "显示原图";
                }
                else
                {
                    ShowOriginalSize();
                    OriginalSizeButton.Content = "适应窗口";
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Space)
            {
                OriginalSizeButton_Click(sender, e);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
