namespace SynAudio.ViewModels
{
    public enum ActionType
    {
        OpenNewTab,

        BrowseByArtists,
        BrowseByFolders,

        OpenArtistAlbums,
        OpenArtistSongs,
        OpenAlbumSongs,

        NowPlaying
    }

    public class NavigationItem
    {
        public string Title { get; set; }
        public ActionType Action { get; set; }
        public string EntityId { get; set; }
        public bool IsSeparator { get; }
        public object SelectedItem { get; set; }

        public NavigationItem() { }
        public NavigationItem(bool isSeparator)
        {
            IsSeparator = isSeparator;
        }
        public NavigationItem(ActionType action, string title, string entityId)
        {
            Action = action;
            Title = title;
            EntityId = entityId;
        }

        public override string ToString() => Title;
    }
}
