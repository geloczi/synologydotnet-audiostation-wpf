using System;
using System.IO;
using SQLite;
using SynAudio.ViewModels;
using SynologyDotNet.AudioStation.Model;

namespace SynAudio.DAL
{
    [Table("Album")]
    public class AlbumModel : ViewModelBase
    {
        public static readonly string CoversDirectory = Path.Combine(App.UserDataFolder, "albumcovers");

        [Column(nameof(Id))]
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [Column(nameof(InsertDate))]
        [NotNull]
        public DateTime InsertDate { get; set; }

        [Column(nameof(Artist))]
        [NotNull]
        public string Artist { get; set; }

        [Column(nameof(Name))]
        [NotNull]
        public string Name { get; set; }

        [Column(nameof(Rating))]
        [NotNull]
        public double Rating { get; set; }

        [Column(nameof(Year))]
        [NotNull]
        public int Year { get; set; }

        /// <summary>
        /// Cover file state
        /// </summary>
        [Column(nameof(CoverFile))]
        [NotNull]
        public ResourceState CoverFile { get; set; }

        public override string ToString() => Name ?? base.ToString();

        public void LoadFromDto(Album dto)
        {
            Name = CleanString(dto.Name);
            Rating = dto.Additional.AverageRating.Value;
            Year = dto.Year;
            Artist = GetFirstCleanString(dto.DisplayArtist, dto.AlbumArtist, dto.Artist);
        }

        public string Cover => CoverFile ==  ResourceState.Exists ? GetCoverFileFullPath() : null;

        public string GetCoverFileFullPath() => GetCoverFileFullPath(Id);

        public bool TryToFindCoverFile()
        {
            var file = GetCoverFileFullPath();
            CoverFile = File.Exists(file) ? ResourceState.Exists : ResourceState.DoesNotExist;
            return CoverFile == ResourceState.Exists;
        }

        public string DisplayName => string.IsNullOrEmpty(Name) ? "Unknown" : TruncateString(Name, 26, "..");

        public static string ConstructSimpleKey(AlbumModel album) => ConstructSimpleKey(album.Artist, album.Name, album.Year);
        public static string ConstructSimpleKey(string artistName, string albumName, int? albumYear) => string.Join("\u000B", artistName, albumName, albumYear.HasValue ? albumYear.Value : 0);
        public static string GetCoverFileFullPath(int albumId) => Path.Combine(CoversDirectory, $"{albumId}.dat");

        public void SaveCover(byte[] bytes)
        {
            File.WriteAllBytes(GetCoverFileFullPath(), bytes);
            CoverFile = ResourceState.Exists;
        }

        public void DeleteCover()
        {
            var path = GetCoverFileFullPath();
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch { }
                CoverFile = ResourceState.DoesNotExist;
            }
        }
    }
}
