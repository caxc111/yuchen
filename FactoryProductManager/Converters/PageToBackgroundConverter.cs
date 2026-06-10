using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FactoryProductManager.Converters
{
    public class PageToBackgroundConverter : IValueConverter
    {
        private static readonly Brush SelectedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAE3DA"));
        private static readonly Brush UnselectedBrush = Brushes.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return DependencyProperty.UnsetValue;
            bool isSelected = value.ToString() == parameter.ToString();
            return isSelected ? SelectedBrush : UnselectedBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
