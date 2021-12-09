using SynAudio.DAL;

namespace Utils
{
    public interface ISettingsRepository
    {
        long? ReadInt64(Int64Values key);
        bool TryReadInt64(Int64Values key, out long value);
        void WriteInt64(Int64Values key, long value);

        string ReadString(StringValues key);
        bool TryReadString(StringValues key, out string value);
        void WriteString(StringValues key, string value);

        byte[] ReadBlob(ByteArrayValues key);
        bool TryReadBlob(ByteArrayValues key, out byte[] value);
        void WriteBlob(ByteArrayValues key, byte[] value);
    }
}
