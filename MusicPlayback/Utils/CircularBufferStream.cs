using System;
using System.IO;
using System.Threading;

namespace MusicPlayback.Utils
{
    /// <summary>
    /// Buffer implementation with blocking Read and blocking Write functions. The Write and Read functions should be invoked from different threads.
    /// The underlying byte array will be overwritten over and over again during the writes, the memory consumption does not increase, the stream is infinite.
    /// </summary>
    public class CircularBufferStream : Stream
    {
        private readonly byte[] _bytes;
        private readonly object _syncRoot = new object();
        private readonly EventWaitHandle _readHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly EventWaitHandle _writeHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private bool _disposed;
        private int _readPosition = 0;
        private int _writePosition = 0;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _bytes.Length;
        public override long Position
        {
            get => _readPosition;
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferSize">Buffer length in bytes</param>
        public CircularBufferStream(int bufferSize) : base()
        {
            _bytes = new byte[bufferSize];
        }

        /// <summary>
        /// Reads bytes from the buffer. Blocks the call until the desired number of bytes are ridden from the buffer (as they arrive via the Write method).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="IndexOutOfRangeException"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CircularBufferStream));
            if (count == 0)
                return 0;
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException(nameof(offset));

            int read = 0;
            while (read < count)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CircularBufferStream));

                int toRead = 0;
                lock (_syncRoot)
                {
                    int _readUntilPosition;
                    if (_writePosition == -1 || _writePosition < _readPosition)
                        _readUntilPosition = _bytes.Length;
                    else
                        _readUntilPosition = _writePosition;

                    if (_readUntilPosition > 0 && _readPosition <= _readUntilPosition)
                    {
                        toRead = Math.Min(count - read, _readUntilPosition - _readPosition);
                        if (toRead > 0)
                        {
                            Array.Copy(_bytes, _readPosition, buffer, offset + read, toRead);
                            read += toRead;
                            _readPosition += toRead;

                            if (read < count && _readPosition >= _bytes.Length && _writePosition > 0)
                            {
                                //Second read
                                toRead = Math.Min(count - read, _writePosition - 1);
                                Array.Copy(_bytes, 0, buffer, offset + read, toRead);
                                read += toRead;
                                _readPosition = toRead;
                            }
                            _writeHandle.Set();
                        }
                    }
                }

                if (read < count && toRead == 0)
                    _readHandle.WaitOne();
            }
            return read;
        }

        /// <summary>
        /// Writes the specified bytes to the buffer. Blocks the call until all the bytes are written to the buffer (as the Read function reads them out).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="IndexOutOfRangeException"/>
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer, offset, count, CancellationToken.None);

        /// <summary>
        /// Writes the specified bytes to the buffer. Blocks the call until all the bytes are written to the buffer (as the Read function reads them out).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="token"></param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void Write(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CircularBufferStream));
            if (count == 0)
                return;
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException(nameof(offset));

            int write = 0;
            while (write < count && !token.IsCancellationRequested)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CircularBufferStream));

                int toWrite = Fill(buffer, offset + write, count - write);
                write += toWrite;
                if (toWrite == 0)
                    _writeHandle.WaitOne();
            }
        }

        /// <summary>
        /// Tries to write bytes to the stream.
        /// </summary>
        /// <param name="buffer">Source byte array</param>
        /// <param name="offset">Read start position in source byte array</param>
        /// <param name="count">Number of bytes to write. The actual number of written bytes may be less, check the return value.</param>
        /// <returns>Returns the count of written bytes. If zero, this means that the stream has to be ridden first to make free space in its internal buffer. In this case, you should invoke Thread.Sleep to wait some time before the next call.</returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="IndexOutOfRangeException"/>
        public int Fill(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CircularBufferStream));
            if (count == 0)
                return 0;
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException(nameof(offset));

            int write = 0;
            lock (_syncRoot)
            {
                if (_writePosition == -1 && _readPosition >= _bytes.Length)
                {
                    // Restart from beginning
                    _writePosition = 0;
                    _readPosition = 0;
                }

                if (_writePosition >= _readPosition && _writePosition < _bytes.Length)
                {
                    // Write after read cursor
                    var toWrite = Math.Min(count, _bytes.Length - _writePosition);
                    if (toWrite > 0)
                    {
                        Array.Copy(buffer, offset, _bytes, _writePosition, toWrite);
                        _writePosition = _writePosition + toWrite;
                        write += toWrite;

                        if (_writePosition == _bytes.Length)
                        {
                            _writePosition = -1;
                            if (_readPosition > 0)
                            {
                                // Write behind read cursor
                                toWrite = Math.Min(count - write, _readPosition - 1);
                                if (toWrite > 0)
                                {
                                    Array.Copy(buffer, offset + write, _bytes, 0, toWrite);
                                    write += toWrite;
                                    _writePosition = toWrite;
                                }
                            }
                        }
                    }
                }

                if (_writePosition < _readPosition && _readPosition == _bytes.Length)
                    _readPosition = 0;
            }
            _readHandle.Set();
            return write;
        }
        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Invoke the Dispose method end streaming and free-up any blocked threads waiting for reading/writing.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                _writeHandle.Set();
                _readHandle.Set();
                _readHandle.Close();
                _writeHandle.Close();
                _readHandle.Dispose();
                _writeHandle.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
