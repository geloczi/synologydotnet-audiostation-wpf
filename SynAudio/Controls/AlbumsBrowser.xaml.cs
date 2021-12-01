using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SynAudio.DAL;

namespace SynAudio.Controls
{
    public partial class AlbumsBrowser : UserControl
    {
        public AlbumsBrowser()
        {
            InitializeComponent();
            grid1.Loaded += grid1_Loaded;
            PreviewKeyDown += AlbumsBrowser_PreviewKeyDown;
        }

        private void AlbumsBrowser_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (grid1.SelectedItem is AlbumModel item)
                    StaticCommands.BrowseLibraryItem.Execute(item);
                e.Handled = true;
            }
        }

        private void grid1_Loaded(object sender, RoutedEventArgs e)
        {
            grid1.Loaded -= grid1_Loaded;
            if (!(grid1.SelectedItem is null))
            {
                grid1.ScrollIntoView(grid1.SelectedItem);
            }
            grid1.Focus();
        }
    }
}
