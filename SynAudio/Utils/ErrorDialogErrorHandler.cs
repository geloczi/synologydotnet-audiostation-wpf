using System;
using System.Windows;
using SynAudio.Utils;

namespace Utils
{
    public class ErrorDialogErrorHandler : IErrorHandler
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        public ErrorDialogErrorHandler()
        {
        }

        public void HandleError(Exception ex)
        {
            _log.Error(ex);
            string msg = ex.ToString();
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }
}
