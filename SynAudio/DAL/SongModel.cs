using System;
using SqlCeLibrary;
using SynAudio.ViewModels;

namespace SynAudio.DAL
{
    [Table("Song")]
    [SQLite.Table("Song")]
    public class SongModel : ViewModelBase
    {
        #region DAL/ViewModel

        [Column, PrimaryKey(AutoIncrement = false)]
        [SQLite.Column(nameof(Id))]
        [SQLite.PrimaryKey]
        public string Id { get; set; }

        [Column]
        [SQLite.Column(nameof(AlbumId))]
        public int AlbumId { get; set; }

        /// <summary>
        /// The artist of the song.
        /// </summary>
        [Column]
        [SQLite.Column(nameof(Artist))]
        public string Artist { get; set; }

        /// <summary>
        /// The artist of the album. This can be "VA" (Various artists) if the album contains different songs from various artists. 
        /// For example: a compilation CD or a movie soundtrack CD. 
        /// </summary>
        [Column]
        [SQLite.Column(nameof(AlbumArtist))]
        [SQLite.MaxLength(255)]
        public string AlbumArtist { get; set; }

        [Column]
        [SQLite.Column(nameof(Album))]
        [SQLite.MaxLength(255)]
        public string Album { get; set; }

        [Column]
        [SQLite.Column(nameof(Title))]
        [SQLite.MaxLength(255)]
        public string Title { get; set; }

        [Column]
        [SQLite.Column(nameof(Rating))]
        public int Rating { get; set; }

        [Column(Size = "20")]
        [SQLite.Column(nameof(Format))]
        [SQLite.MaxLength(20)]
        public string Format { get; set; }

        [Column]
        [SQLite.Column(nameof(Path))]
        [SQLite.MaxLength(400)]
        public string Path { get; set; }

        [Column]
        [SQLite.Column(nameof(Track))]
        public int? Track { get; set; }

        [Column]
        [SQLite.Column(nameof(Year))]
        public int? Year { get; set; }

        [Column]
        [SQLite.Column(nameof(Duration))]
        public TimeSpan Duration { get; set; }

        [Column]
        [SQLite.Column(nameof(FileSize))]
        public int FileSize { get; set; }

        [Column]
        [SQLite.Column(nameof(AudioBitrate))]
        public int AudioBitrate { get; set; }

        [Column]
        [SQLite.Column(nameof(AudioChannels))]
        public int AudioChannels { get; set; }

        [Column(Size = "20")]
        [SQLite.Column(nameof(AudioCodec))]
        [SQLite.MaxLength(20)]
        public string AudioCodec { get; set; }

        [Column(Size = "20")]
        [SQLite.Column(nameof(AudioContainer))]
        [SQLite.MaxLength(20)]
        public string AudioContainer { get; set; }

        [Column]
        [SQLite.Column(nameof(AudioFrequency))]
        public int AudioFrequency { get; set; }

        [Column]
        [SQLite.Column(nameof(Comment))]
        [SQLite.MaxLength(255)]
        public string Comment { get; set; }

        [Column]
        [SQLite.Column(nameof(Composer))]
        [SQLite.MaxLength(255)]
        public string Composer { get; set; }

        [Column]
        [SQLite.Column(nameof(Disc))]
        public int Disc { get; set; }

        [Column]
        [SQLite.Column(nameof(Genre))]
        [SQLite.MaxLength(255)]
        public string Genre { get; set; }

        [Column]
        [SQLite.Column(nameof(RgAlbumGain))]
        public double RgAlbumGain { get; set; }

        [Column]
        [SQLite.Column(nameof(RgAlbumPeak))]
        public double RgAlbumPeak { get; set; }

        [Column]
        [SQLite.Column(nameof(RgTrackGain))]
        public double RgTrackGain { get; set; }

        [Column]
        [SQLite.Column(nameof(RgTrackPeak))]
        public double RgTrackPeak { get; set; }

        #endregion

        #region Internally used columns
        [Column, CompareIgnore]
        public DateTime InsertDate { get; set; }

        [Column, CompareIgnore, Nullable]
        public DateTime? LastPlayDate { get; set; }

        [Column, CompareIgnore]
        public int PlayCount { get; set; }

        [Column, CompareIgnore]
        public int HashCode { get; set; }
        #endregion

        #region OnlyViewModel
        public bool PlaybackError { get; set; }
        public string PathWithBackSlashes => Path.Replace('/', '\\');
        public float? PeakVolume { get; private set; }
        public string FileName => Path.Remove(0, Path.LastIndexOf('/') + 1);
        #endregion

        public SongModel()
        {
        }

        public void UpdateHashCode()
        {
            // A variation of Bernstein hash
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (Id?.GetHashCode() ?? 0);
                hash = hash * 31 + (Artist?.GetHashCode() ?? 0);
                hash = hash * 31 + (AlbumArtist?.GetHashCode() ?? 0);
                hash = hash * 31 + (Album?.GetHashCode() ?? 0);
                hash = hash * 31 + (Comment?.GetHashCode() ?? 0);
                hash = hash * 31 + (Composer?.GetHashCode() ?? 0);
                hash = hash * 31 + Disc.GetHashCode();
                hash = hash * 31 + (Genre?.GetHashCode() ?? 0);
                hash = hash * 31 + RgAlbumGain.GetHashCode();
                hash = hash * 31 + RgAlbumPeak.GetHashCode();
                hash = hash * 31 + RgTrackGain.GetHashCode();
                hash = hash * 31 + RgTrackPeak.GetHashCode();
                hash = hash * 31 + (Title?.GetHashCode() ?? 0);
                hash = hash * 31 + Rating.GetHashCode();
                hash = hash * 31 + (Format?.GetHashCode() ?? 0);
                hash = hash * 31 + (Path?.GetHashCode() ?? 0);
                hash = hash * 31 + Track.GetHashCode();
                hash = hash * 31 + Year.GetHashCode();
                hash = hash * 31 + AudioBitrate.GetHashCode();
                hash = hash * 31 + AudioChannels.GetHashCode();
                hash = hash * 31 + AudioCodec.GetHashCode();
                hash = hash * 31 + AudioContainer.GetHashCode();
                hash = hash * 31 + Duration.GetHashCode();
                hash = hash * 31 + FileSize.GetHashCode();
                hash = hash * 31 + AudioFrequency.GetHashCode();
                HashCode = hash;
            }
        }

        public SongModel(SynologyDotNet.AudioStation.Model.Song dto)
        {
            LoadFromDto(dto);
        }

        public override string ToString() => Title ?? base.ToString();

        public void LoadFromDto(SynologyDotNet.AudioStation.Model.Song dto)
        {
            Id = dto.ID;
            Artist = GetFirstCleanString(dto.Additional.Tag.Artist, dto.Additional.Tag.AlbumArtist);
            AlbumArtist = GetFirstCleanString(dto.Additional.Tag.AlbumArtist, dto.Additional.Tag.AlbumArtist);
            Album = CleanString(dto.Additional.Tag.Album);
            Comment = CleanString(dto.Additional.Tag.Comment);
            Composer = CleanString(dto.Additional.Tag.Composer);
            Disc = dto.Additional.Tag.Disc;
            Genre = CleanString(dto.Additional.Tag.Genre);
            RgAlbumGain = StringToDouble(dto.Additional.Tag.AlbumGain);
            RgAlbumPeak = StringToDouble(dto.Additional.Tag.AlbumPeak);
            RgTrackGain = StringToDouble(dto.Additional.Tag.TrackGain);
            RgTrackPeak = StringToDouble(dto.Additional.Tag.TrackPeak);
            Title = CleanString(dto.Title);
            Rating = dto.Additional.Rating.Value;
            Format = dto.Type;
            Path = dto.Path;
            Track = dto.Additional.Tag.Track;
            Year = dto.Additional.Tag.Year;
            AudioBitrate = dto.Additional.Audio.Bitrate;
            AudioChannels = dto.Additional.Audio.Channels;
            AudioCodec = dto.Additional.Audio.Codec;
            AudioContainer = dto.Additional.Audio.Container;
            Duration = TimeSpan.FromSeconds(dto.Additional.Audio.Duration);
            FileSize = dto.Additional.Audio.FileSize;
            AudioFrequency = dto.Additional.Audio.Frequency;

            UpdateHashCode();
        }

        /// <summary>
        /// Loads additional properties (PeakVolume) from the Comment property, also returns the deserialized instance.
        /// </summary>
        /// <returns></returns>
        public SongCustomization LoadCustomizationFromCommentTag()
        {
            if (SongCustomization.TryDeserialize(Comment, out var custom))
            {
                PeakVolume = custom.peak;
            }
            return custom;
        }

        //public SongModel Clone()
        //{
        //	var result = new SongModel();
        //	TableInfo.Get<SongModel>().CopyAllProperties(this, result);
        //	result.LoadCustomizationFromCommentTag();
        //	return result;
        //}

        public static void Copy(SongModel from, SongModel to)
        {
            TableInfo.Get<SongModel>().CopyAllProperties(from, to);
            to.LoadCustomizationFromCommentTag();
        }

        public static void CopyDifferentProperties(SongModel from, SongModel to)
        {
            TableInfo.Get<SongModel>().CopyDifferentProperties(from, to);
            to.LoadCustomizationFromCommentTag();
        }
    }
}
