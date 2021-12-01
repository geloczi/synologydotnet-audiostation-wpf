using System;
using SynAudio.DAL;

namespace SynAudio.ViewModels
{
    public class SongViewModel : ViewModelBase
    {
        public SongModel Song { get; }
        public bool IsSelected { get; set; }
        public bool IsPlaying { get; set; }

        public SongViewModel() { }

        public SongViewModel(SongModel song)
        {
            if (song is null)
                throw new NullReferenceException(nameof(song));
            Song = song;
        }

        public override string ToString() => Song.FileName ?? base.ToString();
    }
}
