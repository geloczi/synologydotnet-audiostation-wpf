using System;
using System.Globalization;
using System.Windows;

namespace ValueConverters
{
    public sealed class BooleanToVisibilityConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = false;
            if (value is bool b)
                flag = b;
            else if (value is bool?)
                flag = ((bool?)value).GetValueOrDefault();
            if (parameter is string s && bool.TryParse(s, out var bb) && bb)
                flag = !flag;
            return BoolToVisibility(flag);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var back = (value is Visibility) && (((Visibility)value) == Visibility.Visible);
            if (parameter is bool b && b)
                back = !back;
            return back;
        }
    }
}
