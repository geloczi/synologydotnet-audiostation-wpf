using SqlCeLibrary;

namespace SynAudio.DAL
{
    [Table("SongBackup")]
    public class SongBackup
    {
        #region [Song metadata]
        // Here are the metadata fields which can identify a particual song
        [Column, PrimaryKey]
        public string Artist { get; set; }
        [Column, PrimaryKey]
        public string Album { get; set; }
        [Column, PrimaryKey]
        public string Title { get; set; }

        [Column]
        public string Path { get; set; } // Use the Path first to try to match a song. This may fail if the user replaced the song (re-encode, move, rename, ...)
        #endregion

        #region [User related]
        [Column]
        public int Rating { get; set; }
        #endregion

        public SongBackup() { }
        public SongBackup(SongModel song)
        {
            Artist = !string.IsNullOrEmpty(song.Artist) ? song.Artist : song.AlbumArtist;
            Album = song.Album;
            Title = song.Title;
            Path = song.Path;
            Rating = song.Rating;
        }

        public override string ToString() => $"{Artist}, {Album}, {Title}";

        public bool CompareMetadata(SongBackup sb) => CompareMetadata(this, sb);

        public static bool CompareMetadata(SongBackup a, SongBackup b)
        {
            return (a.Artist == b.Artist && a.Album == b.Album && a.Title == b.Title);
        }
    }
}
