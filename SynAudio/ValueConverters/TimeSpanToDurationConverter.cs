using System;
using System.Globalization;

namespace ValueConverters
{
    public class TimeSpanToDurationConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
                return Convert(ts);
            return ConvertToNothing();
        }

        public static string Convert(TimeSpan ts)
        {
            if (ts.Days > 0)
                return $"{(int)ts.TotalDays}d " + ts.ToString(@"h\:mm\:ss"); // This will show the total number of days (int) + time
            else if (ts.Hours > 0)
                return ts.ToString(@"h\:mm\:ss");
            else
                return ts.ToString(@"mm\:ss");
        }
    }
}
