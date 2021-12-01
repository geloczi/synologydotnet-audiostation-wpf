using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace MusicPlayback
{
    public interface IAudioStreamPlayer : IDisposable
    {
        /// <summary>
        /// Raised after playback started.
        /// </summary>
        event EventHandler PlaybackStarted;

        /// <summary>
        /// Raised after end of the song reached.
        /// </summary>
        event EventHandler<StoppedEventArgs> EndOfSongReached;

        /// <summary>
        /// Raised after new samples are added to the playback buffer.
        /// </summary>
        event EventHandler SamplesAdded;

        /// <summary>
        /// Gets the amount of music stored in the playback buffer.
        /// </summary>
        TimeSpan BufferedDuration { get; }

        /// <summary>
        /// Gets or sets the volume of the output device.
        /// </summary>
        double Volume { get; set; }
        
        /// <summary>
        /// Call this method from another thread before calling Play() to start filling the playback buffer.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        void ReadStreamIntoBuffer(Stream stream, CancellationToken token);

        /// <summary>
        /// Starts playback.
        /// </summary>
        void Play();

        /// <summary>
        /// Stops playback
        /// </summary>
        void Pause();
    }
}
