using System.ComponentModel;

namespace SynAudio.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected double StringToDouble(string s)
        {
            double.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out var d);
            return d;
        }

        protected string TruncateString(string s, int maxLength, string suffixIfTruncated)
        {
            if (!string.IsNullOrEmpty(s) && s.Length > maxLength)
            {
                return s.Remove(maxLength - 1) + suffixIfTruncated;
            }
            return s;
        }

        protected string CleanString(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value;

        protected string GetFirstCleanString(params string[] values)
        {
            foreach (var value in values)
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            return string.Empty;
        }
    }
}
