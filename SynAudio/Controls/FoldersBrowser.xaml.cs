using System.Windows.Controls;
using System.Windows.Input;
using SynAudio.Models;
using SynAudio.ViewModels;

namespace SynAudio.Controls
{
    /// <summary>
    /// Interaction logic for FoldersBrowser.xaml
    /// </summary>
    public partial class FoldersBrowser : UserControl
    {
        public FoldersBrowser()
        {
            InitializeComponent();
            Loaded += FoldersBrowser_Loaded;
            PreviewKeyDown += FoldersBrowser_PreviewKeyDown;
        }

        private void FoldersBrowser_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (listBox1.SelectedItem is FolderViewModel item)
                    StaticCommands.BrowseLibraryItem.Execute(item);
                e.Handled = true;
            }
        }

        private void FoldersBrowser_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            listBox1.Focus();
        }

        private void listBox1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listBox1.SelectedItem is FolderViewModel)
                StaticCommands.BrowseLibraryItem.Execute(listBox1.SelectedItem);
            else if (listBox1.SelectedItem is SongViewModel songToPlay)
            {
                var toPlay = new SongsToPlayModel()
                {
                    StartPlaybackWith = songToPlay
                };
                foreach (var item in listBox1.Items)
                    if (item is SongViewModel svm)
                        toPlay.Songs.Add(svm);
                StaticCommands.PlayNow.Execute(toPlay);
            }
        }
    }
}
