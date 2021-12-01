using SqlCeLibrary;

namespace SynAudio.DAL
{
    [Table("NowPlayingItem")]
    public class NowPlayingItem
    {
        [Column, PrimaryKey]
        public int Position { get; set; }

        [Column, PrimaryKey]
        public string SongId { get; set; }

        [Column]
        public int OriginalPosition { get; set; }
    }
}
