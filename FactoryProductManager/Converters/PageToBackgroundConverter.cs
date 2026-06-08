using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FactoryProductManager.Converters
{
    public class PageToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return DependencyProperty.UnsetValue;
            bool isSelected = value.ToString() == parameter.ToString();
            return isSelected ? "White" : "Transparent";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
