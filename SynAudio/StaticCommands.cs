using System.Windows.Input;

namespace SynAudio
{
    public static class StaticCommands
    {
        public static ICommand PlayNow { get; } = new RoutedCommand();
        public static ICommand BrowseLibraryItem { get; } = new RoutedCommand();
        public static ICommand BrowseByArtists { get; } = new RoutedCommand();
        public static ICommand BrowseByFolders { get; } = new RoutedCommand();
        public static ICommand NowPlaying_ChangeCurrentSong { get; } = new RoutedCommand();
        public static ICommand OpenContainingFolder { get; } = new RoutedCommand();
        public static ICommand DeleteSelectedSongsFromLibrary { get; } = new RoutedCommand();
        public static ICommand CopyToClipboard { get; } = new RoutedCommand();
    }
}
