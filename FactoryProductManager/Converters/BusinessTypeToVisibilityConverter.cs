using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FactoryProductManager.Converters
{
    public class BusinessTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var businessType = value?.ToString();
            var expectedType = parameter?.ToString();
            var isMatch = string.Equals(businessType, expectedType, StringComparison.Ordinal);

            if (targetType == typeof(Visibility) || targetType == typeof(object))
            {
                return isMatch ? Visibility.Visible : Visibility.Collapsed;
            }

            return isMatch;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
