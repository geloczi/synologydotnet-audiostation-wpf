using System;
using System.IO;

namespace MusicPlayback.Utils
{
    /// <summary>
    /// Wraps a stream and provides blocking read operation.
    /// </summary>
    public class BlockingReadStream : Stream
    {
        private long _position;
        private readonly Stream _stream;

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length { get; }

        /// <summary>
        /// Gets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Number of bytes to read.
        /// </summary>
        public int RemainingBytes => (int)(Length - Position);

        public BlockingReadStream(Stream stream) : this(stream, stream.Length)
        {
        }

        /// <summary>
        /// Creates a new instance of BlockingReadStream
        /// </summary>
        /// <param name="stream">The underlying stream</param>
        /// <param name="length">Length of the data.</param>
        /// <exception cref="ArgumentException"></exception>
        public BlockingReadStream(Stream stream, long length)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The source stream is not readable.", nameof(stream));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            _stream = stream;
            Length = length;
        }

        /// <summary>
        /// Has no effect.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Reads the specified number of bytes from the stream.
        /// </summary>
        /// <param name="buffer">Target byte array to write into.</param>
        /// <param name="offset">Buffer offset.</param>
        /// <param name="count">Number of bytes to read from the stream. Must be smaller or equal to the Length of the stream.</param>
        /// <returns>Returns the number of bytes ridden from the stream.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            BlockingRead(buffer, offset, count);
            _position += count;
            return count;
        }

        /// <summary>
        /// Reads all bytes from the stream into memory.
        /// </summary>
        /// <returns>Byte array containing the data.</returns>
        public byte[] ReadToEnd()
        {
            if (RemainingBytes == 0)
                return new byte[0];
            byte[] data = new byte[RemainingBytes];
            Read(data, 0, RemainingBytes);
            return data;
        }

        /// <summary>
        /// Reads the underlying stream to end without storing the bytes.
        /// </summary>
        public void ReadToEndDropBytes()
        {
            if (RemainingBytes == 0)
                return;
            byte[] buffer = new byte[4096];
            while (RemainingBytes > 0)
                Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException"></exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <exception cref="NotSupportedException"></exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads exactly the specified amount of bytes from the stream (count). 
        /// It will block the caller thread when there is not enough data and will wait to get all the bytes from the stream.
        /// </summary>
        /// <param name="buffer">Buffer to write.</param>
        /// <param name="offset">Buffer offset.</param>
        /// <param name="count">Number of bytes to read.</param>
        private void BlockingRead(byte[] buffer, int offset, int count)
        {
            if (RemainingBytes == 0)
                throw new EndOfStreamException();
            if (count > RemainingBytes)
                throw new ArgumentOutOfRangeException(nameof(count));
            int read;
            while (count > 0)
            {
                read = _stream.Read(buffer, offset, count);
                offset += read;
                count -= read;
            }
        }
    }
}
