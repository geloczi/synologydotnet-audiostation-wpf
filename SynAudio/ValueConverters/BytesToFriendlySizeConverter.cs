using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ValueConverters
{
    internal class BytesToFriendlySizeConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long l)
                return Convert(l);
            if (value is int i)
                return Convert(i);
            return value;
        }

        public static string Convert(long bytes)
        {
            return $"{bytes / 1024} KB";
        }
    }
}
