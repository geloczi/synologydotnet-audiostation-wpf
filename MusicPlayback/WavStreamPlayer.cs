using System;
using System.IO;
using System.Threading;
using MusicPlayback.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MusicPlayback
{
    public class WavStreamPlayer : IAudioStreamPlayer
    {
        #region Fields
        private readonly object _lock = new object();
        private const int SleepMillisecondsWhenBufferFull = 2;
        private IWavePlayer _outputDevice;
        
        protected readonly MyBufferedWaveProvider _bufferedWaveProvider;
        private readonly VolumeSampleProvider _volumeSampleProvider;
        private readonly OutputApiType _outputType;
        #endregion Fields

        #region Events
        public event EventHandler PlaybackStarted;
        public event EventHandler SamplesAdded;
        public event EventHandler<StoppedEventArgs> EndOfSongReached;
        #endregion Events

        #region Properties
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Total amount of music can be stored in the playback buffer.
        /// </summary>
        public TimeSpan BufferDuration => _bufferedWaveProvider.BufferDuration;

        /// <summary>
        /// Amount of music present in the playback buffer.
        /// </summary>
        public TimeSpan BufferedDuration => _bufferedWaveProvider.BufferedDuration;

        /// <summary>
        /// Total number of bytes the buffer can store.
        /// </summary>
        public int BufferLength => _bufferedWaveProvider.BufferLength;

        /// <summary>
        /// Number of bytes in the buffer.
        /// </summary>
        public int BufferedBytes => _bufferedWaveProvider.BufferedBytes;


        private double _volume = 1;
        public double Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                lock (_lock)
                {
                    if (_outputDevice != null)
                        _volumeSampleProvider.Volume = (float)Volume;
                }
            }
        }
        #endregion Properties

        #region Constructor
        /// <summary>
        /// Creates a BufferedWavPlayer with standard WAV format.
        /// </summary>
        public WavStreamPlayer(OutputApiType outputType, int bufferSizeInSeconds)
        {
            _outputType = outputType;
            _bufferedWaveProvider = new MyBufferedWaveProvider(new WaveFormat(), bufferSizeInSeconds);
            _volumeSampleProvider = new VolumeSampleProvider(_bufferedWaveProvider.ToSampleProvider());
        }
        #endregion Constructor

        #region Public Methods
        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            DisposeOutputDevice();
        }

        /// <summary>
        /// Start playback.
        /// </summary>
        public void Play()
        {
            lock (_lock)
            {
                if (IsDisposed)
                    return;
                if (_outputDevice is null)
                    CreateOutputDevice();
                _outputDevice.Play();

                if (PlaybackStarted != null)
                    PlaybackStarted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Pause playback.
        /// </summary>
        public void Pause()
        {
            _outputDevice?.Pause();
        }

        public virtual void ReadStreamIntoBuffer(Stream stream, CancellationToken token)
        {
            bool playbackStarted = false;
            try
            {
                var buffer = new byte[(int)Math.Min(4096, stream.Length)];
                int read;
                long remaining = stream.Length;

                while (!IsDisposed && !token.IsCancellationRequested && remaining > 0)
                {
                    read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    remaining = stream.Length - stream.Position;
                    if (!playbackStarted && BufferedDuration < TimeSpan.FromSeconds(1))
                    {
                        if (!CanAddSamples(read) || token.IsCancellationRequested)
                            return; // Stream is not playable
                        AddSamples(buffer, 0, read, token);
                    }
                    else
                    {
                        // Continue adding samples
                        AddSamples(buffer, 0, read, token);
                        if (!playbackStarted)
                        {
                            playbackStarted = true;
                            Play();
                        }
                    }
                }
            }
            finally
            {
                // Finished filling the buffer, playback will stop automatically when the buffer gets empty
                _bufferedWaveProvider.ReadFully = false;
                if (!playbackStarted && _bufferedWaveProvider.BufferedBytes > 0)
                    Play();
            }
        }
        #endregion Public Methods

        #region Protected Methods
        protected bool CanAddSamples(int count)
        {
            return _bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes > count;
        }

        /// <summary>
        /// Add samples to the playback buffer. Blocks the caller thread until the specified number of bytes are written to the playback buffer.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="count">Number of bytes to copy from the buffer into the playback buffer.</param>
        /// <param name="token">CancellationToken</param>
        protected void AddSamples(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(WavStreamPlayer));

            while (_bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes <= count)
            {
                Thread.Sleep(SleepMillisecondsWhenBufferFull);
                if (token.IsCancellationRequested || IsDisposed)
                    return;
            }
            _bufferedWaveProvider.AddSamples(buffer, offset, count);

            if (!(SamplesAdded is null))
                SamplesAdded(this, EventArgs.Empty);
        }

        #endregion Protected Methods

        private void CreateOutputDevice()
        {
            if (!(_outputDevice is null))
                throw new InvalidOperationException("Output device already created.");

            
            switch (_outputType)
            {
                case OutputApiType.ASIO:
                    _outputDevice = new AsioOut();
                    break;
                case OutputApiType.DirectSound:
                    _outputDevice = new DirectSoundOut();
                    break;
                case OutputApiType.Wasapi:
                    _outputDevice = new WasapiOut();
                    break;
                case OutputApiType.WaveOut:
                    _outputDevice = new WaveOutEvent();
                    break;
                default:
                    throw new NotSupportedException($"{_outputType}");
            }
            _outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
            _volumeSampleProvider.Volume = (float)Volume;
            _outputDevice.Init(_volumeSampleProvider);
        }

        private void DisposeOutputDevice()
        {
            if (_outputDevice is null)
                return;
            _outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped;
            _outputDevice.Dispose();
            _outputDevice = null;
        }

        private void OutputDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (!(EndOfSongReached is null))
                EndOfSongReached(this, e);
        }
    }
}
