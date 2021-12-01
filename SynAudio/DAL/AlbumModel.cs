using System;
using System.IO;
using SqlCeLibrary;
using SynAudio.ViewModels;
using SynologyDotNet.AudioStation.Model;

namespace SynAudio.DAL
{
    [Table("Album")]
    public class AlbumModel : ViewModelBase
    {
        public static readonly string CoversDirectory = Path.Combine(App.UserDataFolder, "albumcovers");

        [Column, PrimaryKey(AutoIncrement = true)]
        public int Id { get; set; }

        [Column, CompareIgnore]
        public DateTime InsertDate { get; set; }

        [Column]
        public string Artist { get; set; }
        [Column]
        public string Name { get; set; }
        [Column(Default = "0")]
        public int Rating { get; set; }
        [Column]
        public int Year { get; set; }
        /// <summary>
        /// 0: NotSet, 1: Exists, -1: DoesNotExist
        /// </summary>
        [Column]
        public int CoverFileState { get; set; }

        public override string ToString() => Name ?? base.ToString();

        public void LoadFromDto(Album dto)
        {
            Name = CleanString(dto.Name);
            Rating = dto.Additional.AverageRating.Rating;
            Year = dto.Year;
            Artist = GetFirstCleanString(dto.DisplayArtist, dto.AlbumArtist, dto.Artist);
        }

        public string Cover => CoverFileState == 1 ? GetCoverFileFullPath() : null;

        public string GetCoverFileFullPath() => GetCoverFileFullPath(Id);

        public bool TryToFindCoverFile()
        {
            var file = GetCoverFileFullPath();
            CoverFileState = File.Exists(file) ? 1 : -1;
            return CoverFileState == 1;
        }

        public string DisplayName => string.IsNullOrEmpty(Name) ? "Unknown" : TruncateString(Name, 26, "..");

        public static string ConstructSimpleKey(AlbumModel album) => ConstructSimpleKey(album.Artist, album.Name, album.Year);
        public static string ConstructSimpleKey(string artistName, string albumName, int? albumYear) => string.Join("\u000B", artistName, albumName, albumYear.HasValue ? albumYear.Value : 0);
        public static string GetCoverFileFullPath(int albumId) => Path.Combine(CoversDirectory, $"{albumId}.dat");

        public void SaveCover(byte[] bytes)
        {
            File.WriteAllBytes(GetCoverFileFullPath(), bytes);
            CoverFileState = 1;
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
                CoverFileState = -1;
            }
        }
    }
}
