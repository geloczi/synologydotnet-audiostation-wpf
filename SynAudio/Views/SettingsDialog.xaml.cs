using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SynAudio.ViewModels;

namespace SynAudio.Views
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private SettingsDialogModel VM { get; }
        public SettingsDialog(SettingsDialogModel vm)
        {
            InitializeComponent();
            DataContext = VM = vm;
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            VM.Main.Disconnect();
        }

        private void btnBackupUserData_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO");
            //VM.Main.BackupUserData();
        }

        private void btnRestoreUserData_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO");
            //VM.Main.RestoreUserData();
        }
    }

    public class HotkeyViewModel : ViewModelBase
    {
        public string KeyShortcut
        {
            get
            {
                var items = ((ModifierKeys[])Enum.GetValues(typeof(ModifierKeys))).Where(x => (int)x > 0 && Modifiers.HasFlag(x)).Select(x => x.ToString()).ToList();
                items.AddRange(Keys.OrderBy(x => x).Select(x => x.ToString()));
                return string.Join(" + ", items);
            }
        }
        public string Command { get; set; }

        public ModifierKeys Modifiers { get; set; }
        public Key[] Keys { get; set; } = new Key[0];

        public HotkeyViewModel() { }
        public HotkeyViewModel(string command, ModifierKeys modifiers, Key[] keys)
        {
            Command = command;
            Modifiers = modifiers;
            Keys = keys;
        }
        public HotkeyViewModel(string command, ModifierKeys modifiers, Key key) : this(command, modifiers, new Key[] { key })
        {
        }
        public HotkeyViewModel(string command, ModifierKeys modifiers) : this(command, modifiers, new Key[0])
        {
        }
    }

}
