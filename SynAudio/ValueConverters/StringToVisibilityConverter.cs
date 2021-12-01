using System;
using System.Globalization;

namespace ValueConverters
{
    public class StringToVisibilityConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool trueValue = !ParameterToBool(parameter); //Negate output if parameter is True
            return BoolToVisibility(Convert(value, trueValue));
        }

        public static bool Convert(object value, bool trueValue)
        {
            if (value is null)
                return !trueValue;
            else if (value is string s && string.IsNullOrEmpty(s))
                return !trueValue;
            return trueValue;
        }
    }
}
