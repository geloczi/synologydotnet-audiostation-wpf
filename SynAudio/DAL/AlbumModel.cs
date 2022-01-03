using System;
using System.IO;
using SQLite;
using SynAudio.Utils;
using SynAudio.ViewModels;
using SynologyDotNet.AudioStation.Model;

namespace SynAudio.DAL
{
    [Table("Album")]
    public class AlbumModel : ViewModelBase
    {
        public static readonly string CoversDirectory = Path.Combine(App.UserDataFolder, "covers");

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

        [Column(nameof(CoverFileName))]
        [MaxLength(64)]
        public string CoverFileName { get; set; }

        public override string ToString() => Name ?? base.ToString();

        public void LoadFromDto(Album dto)
        {
            Name = CleanString(dto.Name);
            Rating = dto.Additional.AverageRating.Value;
            Year = dto.Year;
            Artist = GetFirstCleanString(dto.DisplayArtist, dto.AlbumArtist, dto.Artist);
        }
        public string DisplayName => string.IsNullOrEmpty(Name) ? "Unknown" : TruncateString(Name, 26, "..");

        public string Cover => GetCoverFileFullPath(CoverFileName);

        public static string GetCoverFileFullPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;
            return Path.Combine(CoversDirectory, fileName);
        }

        public static string GetCoverFileNameFromArtistAndAlbum(string artist, string album)
        {
            string hash = Sha256Hash.FromObject(new { artist, album });
            if (hash.Length > 64)
                throw new Exception($"Hash value must not exceed 64 characters! It was {hash.Length}.");
            return hash;
        }

        public static void SaveCoverFile(string fileName, byte[] data)
        {
            string path = GetCoverFileFullPath(fileName);
            File.WriteAllBytes(path, data);
        }
    }
}
