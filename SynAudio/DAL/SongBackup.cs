using SQLite;

namespace SynAudio.DAL
{
    [Table("SongBackup")]
    public class SongBackup
    {
        #region [Song metadata]

        [Column(nameof(Id))]
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [Column(nameof(Artist))]
        public string Artist { get; set; }

        [Column(nameof(Album))]
        public string Album { get; set; }

        [Column(nameof(Title))]
        public string Title { get; set; }

        [Column(nameof(Path))]
        public string Path { get; set; }
        #endregion

        #region [User related]

        [Column(nameof(Rating))]
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
