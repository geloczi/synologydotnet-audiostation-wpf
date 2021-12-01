using System.Windows;
using SynAudio.Models;

namespace SynAudio.Views
{
    /// <summary>
    /// Interaction logic for LoginDialog.xaml
    /// </summary>
    public partial class LoginDialog : Window
    {
        public Credentials Result { get; }

        public LoginDialog(Credentials Credentials)
        {
            InitializeComponent();
            DataContext = Result = Credentials;
            Owner = Application.Current.MainWindow;
            Closing += LoginDialog_Closing;
            tbPassword.Password = Credentials.Password;
        }

        private void LoginDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Result.Password = tbPassword.Password;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
