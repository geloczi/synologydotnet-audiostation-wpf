using System;
using System.Globalization;

namespace ValueConverters
{
    public sealed class ObjectToVisibilityConverter : ConverterBase
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
            return trueValue;
        }
    }
}
