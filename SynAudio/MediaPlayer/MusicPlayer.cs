using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MusicPlayback;
using MusicPlayback.Utils;
using SynAudio.DAL;
using SynAudio.Library;
using SynAudio.ViewModels;
using SynologyDotNet.AudioStation;
using Utils;

namespace SynAudio.MediaPlayer
{
    public class MusicPlayer : ViewModelBase, IDisposable
    {
        #region [Fields]
        private const int BufferLengthInSeconds = 60;
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly object _lock = new object();
        private readonly AudioLibrary _audioLibrary;
        private readonly Stopwatch _downloadSpeedStopwatch = new Stopwatch();
        private bool _disposed;
        private BackgroundThreadWorker _streamWorker;
        private BackgroundAsyncTaskWorker _pollTask;
        private BackgroundAsyncTaskWorker _pauseCleanupWorker;
        private SongModel _song;
        private BlockingReadStream _songStream;
        private long _songStreamFullLength;
        private long _downloadStopwatchLastPosition;
        private TranscodeMode _songTranscodeMode;
        #endregion

        #region [Events]
        public event EventHandler<PlaybackStateChangedEventArgs> PlaybackStateChanged;
        #endregion

        #region [Properties]
        public IAudioStreamPlayer Player;

        private PlaybackStateType _playbackState = PlaybackStateType.Stopped;
        public PlaybackStateType PlaybackState
        {
            get => _playbackState;
            private set
            {
                var args = new PlaybackStateChangedEventArgs(_playbackState, value);
                _playbackState = value;
                PlaybackStateChanged.FireAsync(this, args);
            }
        }

        public bool IsPlaying => PlaybackState == PlaybackStateType.Playing;
        public TimeSpan Length { get; private set; }

        private TimeSpan _position;
        public TimeSpan Position
        {
            get => _position;
            set => Seek(value);
        }

        private double _volume = 1;
        public double Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                lock (_lock)
                {
                    if (Player != null)
                        Player.Volume = _volume;
                }
            }
        }

        public int BytesPerSecond { get; private set; }

        public double BufferFillRatio { get; private set; }

        #endregion

        #region Constructor
        public MusicPlayer(AudioLibrary audioLibrary)
        {
            _audioLibrary = audioLibrary;
        }
        #endregion

        #region [Public Methods]
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        public void Play()
        {
            lock (_lock)
            {
                _pauseCleanupWorker?.Cancel();
                if (PlaybackState == PlaybackStateType.Paused && !(_song is null) && Player is null)
                {
                    ResumeFromPause();
                }
                else if (!(Player is null))
                {
                    Player.Play();
                    StartPolling();
                }
            }
        }

        public void Pause()
        {
            lock (_lock)
            {
                _pauseCleanupWorker?.Cancel();
                StopPolling();
                if (Player is null)
                    return;
                if (PlaybackState == PlaybackStateType.Playing)
                {
                    Player.Pause();
                    PlaybackState = PlaybackStateType.Paused;
                    UpdateCounters();

                    if (_pauseCleanupWorker is null)
                    {
                        _pauseCleanupWorker = new BackgroundAsyncTaskWorker(async (t) =>
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5), t);
                            try
                            {
                                lock (_lock)
                                {
                                    if (!t.IsCancellationRequested && PlaybackState == PlaybackStateType.Paused)
                                        CleanUpPlayback();
                                }
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex)
                            {
                                _log.Error(ex);
                            }
                        });
                    }
                    _pauseCleanupWorker.Start();
                }
            }
        }

        /// <summary>
        /// Shutdown the music player process
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                CleanUpPlayback();
                _song = null;
                _songStream = null;
                SetPosition(TimeSpan.Zero);
                Length = TimeSpan.Zero;
            }
            PlaybackState = PlaybackStateType.Stopped;
        }

        private void CleanUpPlayback()
        {
            StopPolling();
            if (!(_streamWorker is null))
            {
                _streamWorker.Cancel();
                _streamWorker.Wait();
                _streamWorker = null;
            }
            if (!(Player is null))
            {
                Player.EndOfSongReached -= Player_EndOfSongReached;
                Player.PlaybackStarted -= Player_PlaybackStarted;
                Player.Dispose();
                Player = null;
            }
        }

        public void RestoreState(SongModel song, TranscodeMode transcode, TimeSpan position)
        {
            lock (_lock)
            {
                Stop();

                _song = song;
                _songTranscodeMode = transcode;
                Length = song.Duration;
                SetPosition(position);
                _songStreamFullLength = -1;
                PlaybackState = PlaybackStateType.Paused;
            }
        }

        public void StreamSong(SongModel song, TranscodeMode transcode)
        {
            lock (_lock)
            {
                Stop();

                _song = song;
                _songTranscodeMode = transcode;
                Length = song.Duration;
                SetPosition(TimeSpan.Zero);
                _songStreamFullLength = -1;
                StartSongStreamingInternal(song, transcode, 0);
            }
        }

        private void StartSongStreamingInternal(SongModel song, TranscodeMode transcode, double positionSeconds)
        {
            CleanUpPlayback();

            if (transcode == TranscodeMode.None)
                throw new NotSupportedException();
            else if (transcode == TranscodeMode.WAV)
                Player = new WavStreamPlayer(BufferLengthInSeconds);
            else
                Player = new Mp3StreamPlayer(BufferLengthInSeconds);

            Player.Volume = _volume;
            Player.PlaybackStarted += Player_PlaybackStarted;
            Player.EndOfSongReached += Player_EndOfSongReached;

            _streamWorker = new BackgroundThreadWorker(worker =>
                {
                    try
                    {
                        // Request stream with position=0 to find out the full stream length
                        if (positionSeconds > 0 && _songStreamFullLength == -1)
                        {
                            _audioLibrary.StreamSongAsync(worker.Token, transcode, song.Id, 0, s =>
                            {
                                _songStreamFullLength = s.ContentLength;
                            }).GetAwaiter().GetResult();
                        }

                        // Start streaming
                        _audioLibrary.StreamSongAsync(worker.Token, transcode, song.Id, positionSeconds, s =>
                        {
                            _songStream = new BlockingReadStream(s.Stream, s.ContentLength);
                            if (positionSeconds == 0)
                            {
                                // Full stream length is in the current stream
                                _songStreamFullLength = s.ContentLength;
                            }

                            if (!worker.Token.IsCancellationRequested)
                            {
                                StartPolling();
                                Player.ReadStreamIntoBuffer(_songStream, worker.Token);
                            }
                        }).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex);
                    }
                },
                nameof(_streamWorker));
            _streamWorker.Start();
        }

        private void SetPosition(TimeSpan value)
        {
            if (value > Length)
                _position = TimeSpan.Zero;
            else
                _position = value;
            OnPropertyChanged(nameof(Position));
        }

        private void Seek(TimeSpan position)
        {
            lock (_lock)
            {
                StopPolling();

                if (_song is null)
                    return;
                
                if (position > Length - TimeSpan.FromSeconds(1))
                    position = Length > TimeSpan.FromSeconds(1) ? Length - TimeSpan.FromSeconds(1) : TimeSpan.Zero;

                if (PlaybackState == PlaybackStateType.Playing)
                {
                    StartSongStreamingInternal(_song, _songTranscodeMode, position.TotalSeconds);
                }
                else if (PlaybackState == PlaybackStateType.Paused)
                {
                    _pauseCleanupWorker?.Cancel();
                    CleanUpPlayback();
                    SetPosition(position);
                }
            }
        }

        private void ResumeFromPause()
        {
            lock (_lock)
            {
                StopPolling();
                PlaybackState = PlaybackStateType.Playing;
                Seek(Position);
            }
        }

        private void StartPolling()
        {
            lock (_lock)
            {
                BytesPerSecond = 0;
                _downloadStopwatchLastPosition = 0;
                _downloadSpeedStopwatch.Restart();

                if (_pollTask is null)
                {
                    _pollTask = new BackgroundAsyncTaskWorker(async (token) =>
                    {
                        try
                        {
                            while (!token.IsCancellationRequested)
                            {
                                UpdateCounters();
                                await Task.Delay(100);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                        }
                    });
                }
                _pollTask.Start();
            }
        }

        private void StopPolling()
        {
            lock (_lock)
            {
                if (!(_pollTask is null))
                {
                    _pollTask.Cancel();
                    _pollTask.Wait();
                }
                BytesPerSecond = 0;
            }
        }

        private void UpdateCounters()
        {
            lock (_lock)
            {
                if (!(Player is null) && !(_songStream is null) && _songStreamFullLength > 0)
                {
                    // Download speed
                    long songStreamPosition = _songStream.Position;
                    if (_downloadSpeedStopwatch.ElapsedMilliseconds >= 1000)
                    {
                        long bytesSinceTimerStart = songStreamPosition - _downloadStopwatchLastPosition;
                        double bytesPerMillisecond = bytesSinceTimerStart / (double)_downloadSpeedStopwatch.ElapsedMilliseconds;
                        BytesPerSecond = (int)(bytesPerMillisecond * 1000.0);
                        _downloadStopwatchLastPosition = songStreamPosition;
                        _downloadSpeedStopwatch.Restart();
                    }

                    // Position
                    var positionInFullStream = _songStreamFullLength - _songStream.Length + songStreamPosition;
                    TimeSpan downloaded = TimeSpan.FromMilliseconds(Length.TotalMilliseconds * (positionInFullStream / (double)_songStreamFullLength));
                    SetPosition(downloaded - Player.BufferedDuration);
                    BufferFillRatio = Player.BufferedDuration.TotalSeconds / BufferLengthInSeconds;
                    //OnPropertyChanged(nameof(BufferFillRatio));
                }
            }
        }

        private void Player_PlaybackStarted(object sender, EventArgs e)
        {
            PlaybackState = PlaybackStateType.Playing;
        }

        private void Player_EndOfSongReached(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            StopPolling();
            PlaybackState = PlaybackStateType.EndOfSong;
        }
        #endregion
    }
}
