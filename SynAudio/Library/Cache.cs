using SynologyDotNet.AudioStation.Model;
using System;
using System.Collections.Concurrent;

namespace SynAudio.Library
{
    [Serializable]
    public class Cache
    {
        public ConcurrentDictionary<string, Artist> Artists { get; set; }
        public ConcurrentDictionary<string, Album> Albums { get; set; }
        public ConcurrentDictionary<string, Song> Songs { get; set; }
        public ConcurrentDictionary<string, byte[]> ArtistCovers { get; set; }

        public void OnDeserialized()
        {
            if (Artists is null)
                Artists = new ConcurrentDictionary<string, Artist>();
            if (Albums is null)
                Albums = new ConcurrentDictionary<string, Album>();
            if (Songs is null)
                Songs = new ConcurrentDictionary<string, Song>();
            if (ArtistCovers is null)
                ArtistCovers = new ConcurrentDictionary<string, byte[]>();
        }
    }
}
