using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SynAudio.DAL;

namespace SynAudio.Controls
{
    public partial class ArtistsBrowser : UserControl
    {
        public ArtistsBrowser()
        {
            InitializeComponent();
            grid1.Loaded += grid1_Loaded;
            PreviewKeyDown += ArtistsBrowser_PreviewKeyDown;
        }

        private void ArtistsBrowser_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (grid1.SelectedItem is ArtistModel item)
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
