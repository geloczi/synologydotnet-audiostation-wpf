using System;
using System.Globalization;

namespace ValueConverters
{
    public sealed class NumberToVisibilityConverter : ConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool trueValue = !ParameterToBool(parameter); //Negate output if parameter is True
            return BoolToVisibility(Convert(value, trueValue));
        }

        public static bool Convert(object value, bool trueValue)
        {
            if (value is byte b && b != 0)
                return trueValue;
            else if (value is sbyte sb && sb != 0)
                return trueValue;
            else if (value is ushort us && us != 0)
                return trueValue;
            else if (value is uint ui && ui != 0)
                return trueValue;
            else if (value is ulong ul && ul != 0)
                return trueValue;
            else if (value is short s && s != 0)
                return trueValue;
            else if (value is int i && i != 0)
                return trueValue;
            else if (value is long l && l != 0)
                return trueValue;
            else if (value is decimal d && d != 0)
                return trueValue;
            else if (value is double db && db != 0)
                return trueValue;
            else if (value is float f && f != 0)
                return trueValue;
            return !trueValue;
        }
    }
}
