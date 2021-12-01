using System.Collections.Generic;
using SynAudio.ViewModels;

namespace SynAudio.Models
{
    public class SongsToPlayModel
    {
        public List<SongViewModel> Songs { get; } = new List<SongViewModel>();
        public SongViewModel StartPlaybackWith { get; set; }
    }
}
