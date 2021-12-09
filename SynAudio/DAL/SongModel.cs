using System;
using SQLite;
using SynAudio.ViewModels;

namespace SynAudio.DAL
{

    [Table("Song")]
    public class SongModel : ViewModelBase
    {
        #region DAL/ViewModel

        [Column(nameof(Id))]
        [PrimaryKey]
        [MaxLength(50)]
        public string Id { get; set; }

        [Column(nameof(AlbumId))]
        [NotNull]
        public int AlbumId { get; set; }

        /// <summary>
        /// The artist of the song.
        /// </summary>
        [Column(nameof(Artist))]
        [NotNull]
        public string Artist { get; set; }

        /// <summary>
        /// The artist of the album. This can be "VA" (Various artists) if the album contains different songs from various artists. 
        /// For example: a compilation CD or a movie soundtrack CD. 
        /// </summary>
        [Column(nameof(AlbumArtist))]
        [NotNull]
        [MaxLength(255)]
        public string AlbumArtist { get; set; }

        [Column(nameof(Album))]
        [NotNull]
        [MaxLength(255)]
        public string Album { get; set; }

        [Column(nameof(Title))]
        [NotNull]
        [MaxLength(255)]
        public string Title { get; set; }

        [Column(nameof(Rating))]
        [NotNull]
        public int Rating { get; set; }

        [Column(nameof(Format))]
        [NotNull]
        [MaxLength(20)]
        public string Format { get; set; }

        [Column(nameof(Path))]
        [NotNull]
        [MaxLength(500)]
        public string Path { get; set; }

        [Column(nameof(Track))]
        [NotNull]
        public int? Track { get; set; }

        [Column(nameof(Year))]
        [NotNull]
        public int Year { get; set; }

        [Column(nameof(Duration))]
        [NotNull]
        public TimeSpan Duration { get; set; }

        [Column(nameof(FileSize))]
        [NotNull]
        public int FileSize { get; set; }

        [Column(nameof(AudioBitrate))]
        [NotNull]
        public int AudioBitrate { get; set; }

        [Column(nameof(AudioChannels))]
        [NotNull]
        public int AudioChannels { get; set; }

        [Column(nameof(AudioCodec))]
        [NotNull]
        [MaxLength(20)]
        public string AudioCodec { get; set; }

        [Column(nameof(AudioContainer))]
        [NotNull]
        [MaxLength(20)]
        public string AudioContainer { get; set; }

        [Column(nameof(AudioFrequency))]
        [NotNull]
        public int AudioFrequency { get; set; }

        [Column(nameof(Comment))]
        [NotNull]
        [MaxLength(255)]
        public string Comment { get; set; }

        [Column(nameof(Composer))]
        [NotNull]
        [MaxLength(255)]
        public string Composer { get; set; }

        [Column(nameof(Disc))]
        [NotNull]
        public int Disc { get; set; }

        [Column(nameof(Genre))]
        [NotNull]
        [MaxLength(255)]
        public string Genre { get; set; }

        [Column(nameof(RgAlbumGain))]
        [NotNull]
        public double RgAlbumGain { get; set; }

        [Column(nameof(RgAlbumPeak))]
        [NotNull]
        public double RgAlbumPeak { get; set; }

        [Column(nameof(RgTrackGain))]
        [NotNull]
        public double RgTrackGain { get; set; }

        [Column(nameof(RgTrackPeak))]
        [NotNull]
        public double RgTrackPeak { get; set; }

        #endregion

        #region Internally used columns

        [Column(nameof(InsertDate))]
        [NotNull]
        public DateTime InsertDate { get; set; }

        [Column(nameof(LastPlayDate))]
        public DateTime? LastPlayDate { get; set; }

        [Column(nameof(PlayCount))]
        [NotNull]
        public int PlayCount { get; set; }

        [Column(nameof(Md5Hash))]
        [NotNull]
        [MaxLength(32)]
        public string Md5Hash { get; set; }
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

            Md5Hash = Utils.Md5Hash.FromObject(dto);
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

        public static void Copy(SongModel from, SongModel to)
        {
            to.Album = from.Album;
            to.AlbumArtist = from.AlbumArtist;
            to.AlbumId = from.AlbumId;
            to.Artist = from.Artist;
            to.AudioBitrate = from.AudioBitrate;
            to.AudioChannels = from.AudioChannels;
            to.AudioCodec = from.AudioCodec;
            to.AudioContainer = from.AudioContainer;
            to.AudioFrequency = from.AudioFrequency;
            to.Comment = from.Comment;
            to.Composer = from.Composer;
            to.Disc = from.Disc;
            to.Duration = from.Duration;
            to.FileSize = from.FileSize;
            to.Format = from.Format;
            to.Genre = from.Genre;
            to.InsertDate = from.InsertDate;
            to.LastPlayDate = from.LastPlayDate;
            to.Path = from.Path;
            to.PlayCount = from.PlayCount;
            to.Rating = from.Rating;
            to.RgAlbumGain = from.RgAlbumGain;
            to.RgAlbumPeak = from.RgAlbumPeak;
            to.RgTrackGain = from.RgTrackGain;
            to.RgTrackPeak = from.RgTrackPeak;
            to.Title = from.Title;
            to.Track = from.Track;
            to.Year = from.Year;
        }
    }
}
