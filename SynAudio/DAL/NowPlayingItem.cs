using SQLite;

namespace SynAudio.DAL
{
    [Table("NowPlayingItem")]
    public class NowPlayingItem
    {
        [Column(nameof(Position))]
        [NotNull]
        public int Position { get; set; }

        [Column(nameof(SongId))]
        [NotNull]
        public string SongId { get; set; }

        [Column(nameof(OriginalPosition))]
        [NotNull]
        public int OriginalPosition { get; set; }
    }
}
