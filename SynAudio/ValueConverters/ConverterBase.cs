using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ValueConverters
{
    public abstract class ConverterBase : IValueConverter
    {
        public abstract object Convert(object value, System.Type targetType, object parameter, CultureInfo culture);

        public virtual object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

        protected static bool ParameterToBool(object parameter)
        {
            if (!(parameter is null) && bool.TryParse(parameter.ToString(), out var b) && b)
                return b;
            return false;
        }

        protected static Visibility BoolToVisibility(bool b) => b ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Return this value instead of null in your Convert() function.
        /// </summary>
        /// <returns></returns>
        protected static object ConvertToNothing() => DependencyProperty.UnsetValue;

        /// <summary>
        /// Return this value instead of null in your ConvertBack() function.
        /// </summary>
        /// <returns></returns>
        protected static object ConvertBackToNothing() => Binding.DoNothing;
    }
}
