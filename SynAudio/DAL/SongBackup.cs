using SQLite;

namespace SynAudio.DAL
{
    [Table("SongBackup")]
    public class SongBackup
    {
        #region [Song metadata]

        [Column(nameof(Id))]
        [PrimaryKey]
        [MaxLength(40)]
        public string Id { get; set; }

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
            Artist = (!string.IsNullOrEmpty(song.Artist) ? song.Artist : song.AlbumArtist).Trim();
            Album = song.Album.Trim();
            Title = song.Title.Trim();
            Path = song.Path;
            Rating = song.Rating;

            Id = Utils.DeterministicHash.HashObject(new { Artist, Album, Title });
        }

        public override string ToString() => $"{Artist}, {Album}, {Title}";

        public bool CompareMetadata(SongBackup sb) => CompareMetadata(this, sb);

        public static bool CompareMetadata(SongBackup a, SongBackup b)
        {
            return (a.Artist == b.Artist && a.Album == b.Album && a.Title == b.Title);
        }
    }
}
