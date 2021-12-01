using System.IO;
using System.Threading;

namespace SynCommon.IO
{
    public class WritableBlockingReadStream : Stream
    {
        private Stream _stream;
        private int _millisecondsToWaitAfterZeroRead = 10;
        private readonly long _maxLength;
        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public WritableBlockingReadStream(Stream stream, long maxLength)
        {
            _stream = stream;
            _maxLength = maxLength;
        }

        public override void Flush() => _stream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = -1;
            int actualCount = 0;
            do
            {
                if (read == 0)
                    Thread.Sleep(_millisecondsToWaitAfterZeroRead);
                if (!_stream.CanRead)
                    throw new IOException("Can't read stream");
                read = _stream.Read(buffer, offset + actualCount, count - actualCount);
                actualCount += read;
            }
            while (actualCount < count && _stream.Position < _maxLength);
            return actualCount;
        }

        public override void Close()
        {
            if (!(_stream is null))
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Close();
            base.Dispose(disposing);
        }
    }
}
