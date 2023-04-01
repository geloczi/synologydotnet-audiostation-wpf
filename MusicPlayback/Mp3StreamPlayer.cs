using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace MusicPlayback
{
    public class Mp3StreamPlayer : WavStreamPlayer
    {
        public Mp3StreamPlayer(OutputApiType outputType, int bufferSizeInSeconds)
            : base(outputType, bufferSizeInSeconds)
        {
        }

        public override void ReadStreamIntoBuffer(Stream stream, CancellationToken token)
        {
            var playbackStarted = false;
            try
            {
                IMp3FrameDecompressor decompressor = null;
                var frameBuffer = new byte[16 * 1024]; // Size copied from Mp3Frame.MaxFrameLength
                while (!IsDisposed && !token.IsCancellationRequested && stream.Length - stream.Position > 0)
                {
                    // Parse MP3 frame
                    Mp3Frame frame = null;
                    var positionBeforeFrameParsing = stream.Position;
                    try
                    {
                        frame = Mp3Frame.LoadFromStream(stream);
                    }
                    catch { }
                    if (stream.Position == positionBeforeFrameParsing)
                        break; // Stop MP3 frame parsing if the parser cannot read more bytes
                    if (frame is null)
                        continue; // Drop corrupted frame

                    // Decompress MP3 frame to WAV
                    if (decompressor is null)
                        decompressor = CreateFrameDecompressor(frame);
                    int frameLength = decompressor.DecompressFrame(frame, frameBuffer, 0);
                    if (frameLength == 0)
                        continue; // Invalid frame

                    // Add bytes to playback buffer
                    if (!playbackStarted && BufferedDuration < TimeSpan.FromSeconds(1))
                    {
                        if (!CanAddSamples(frameLength) || token.IsCancellationRequested)
                            return; // Stream is not playable
                        AddSamples(frameBuffer, 0, frameLength, token);
                    }
                    else
                    {
                        // Continue adding samples
                        AddSamples(frameBuffer, 0, frameLength, token);
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

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }
    }
}
