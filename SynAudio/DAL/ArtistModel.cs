using SQLite;
using SynAudio.ViewModels;
using SynologyDotNet.AudioStation.Model;

namespace SynAudio.DAL
{
    [Table("Artist")]
    public class ArtistModel : ViewModelBase
    {
        [Column(nameof(Name))]
        [PrimaryKey]
        [MaxLength(255)]
        public string Name { get; set; }

        [Column(nameof(CoverFileName))]
        [MaxLength(64)]
        public string CoverFileName { get; set; }

        public string Cover => AlbumModel.GetCoverFileFullPath(CoverFileName);

        public string DisplayName => string.IsNullOrEmpty(Name) ? "(Unknown)" : TruncateString(Name, 26, "..");

        public override string ToString() => Name ?? base.ToString();

        public void LoadFromDto(Artist dto)
        {
            Name = CleanString(dto.Name);
        }
    }
}
