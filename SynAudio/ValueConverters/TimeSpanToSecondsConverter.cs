using System;
using System.Globalization;

namespace ValueConverters
{
    public class TimeSpanToSecondsConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
                return ts.TotalSeconds;
            return ConvertToNothing();
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return TimeSpan.FromSeconds(d);
            if (value is int i)
                return TimeSpan.FromSeconds(i);
            return ConvertBackToNothing();
        }
    }
}
