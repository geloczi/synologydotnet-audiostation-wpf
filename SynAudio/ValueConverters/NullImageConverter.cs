using System;
using System.Globalization;
using System.Windows;

namespace ValueConverters
{
    /// <summary>
    /// Resolves the "Cannot convert '<null>' from type '<null>' to type 'System.Windows.Media.ImageSource'" exception by converting NULL value to DependencyProperty.UnsetValue.
    /// </summary>
    public class NullImageConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is null) ? DependencyProperty.UnsetValue : value;
        }
    }
}
