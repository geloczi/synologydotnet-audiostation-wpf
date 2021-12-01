using System;
using SqlCeLibrary;
using SynAudio.ViewModels;
using SynologyDotNet.AudioStation.Model;

namespace SynAudio.DAL
{
    [Table("Artist")]
    public class ArtistModel : ViewModelBase
    {
        [Column, PrimaryKey]
        public string Name { get; set; }

        [Column, Nullable]
        public int? CoverAlbumId { get; set; }

        public string DisplayName => string.IsNullOrEmpty(Name) ? "(Unknown)" : TruncateString(Name, 26, "..");
        public string Cover => CoverAlbumId.HasValue ? AlbumModel.GetCoverFileFullPath(CoverAlbumId.Value) : null;

        public override string ToString() => Name ?? base.ToString();

        public void LoadFromDto(Artist dto)
        {
            Name = CleanString(dto.Name);
        }
    }
}
