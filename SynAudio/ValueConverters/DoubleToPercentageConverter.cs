using System;
using System.Globalization;

namespace ValueConverters
{
    public class DoubleToPercentageConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int decimals = 0;
            if (parameter is not null)
                int.TryParse(parameter.ToString(), out decimals);

            if (value is double d)
                return Math.Round(d * 100.0, decimals);
            if (value is float f)
                return Math.Round(f * 100.0, decimals);
            return ConvertToNothing();
        }
    }
}
