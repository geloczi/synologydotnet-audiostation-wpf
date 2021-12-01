using System.Windows;
using SynAudio.ViewModels;

namespace SynAudio.Views
{
    /// <summary>
    /// Interaction logic for LoginDialog.xaml
    /// </summary>
    public partial class LoginDialog : Window
    {
        public LoginDialogViewModel Result { get; set; }

        public LoginDialog(LoginDialogViewModel model)
        {
            InitializeComponent();
            this.DataContext = Result = model;
            this.Closing += LoginDialog_Closing;
            tbPassword.Password = model.Password;
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
