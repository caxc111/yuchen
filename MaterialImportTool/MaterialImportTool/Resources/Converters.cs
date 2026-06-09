using System;
using System.Globalization;
using System.Windows.Data;

namespace MaterialImportTool.Resources
{
    public class IdToTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int id && id > 0)
            {
                return "编辑工厂";
            }
            return "新增工厂";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}