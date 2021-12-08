using SQLite;
using SynAudio.DAL;

namespace SqlCeLibrary
{
    static class SqlCeExtensions
    {
        #region Int64Value
        public static long? ReadInt64(this SQLiteConnection sql, Int64Values key) => Int64Value.Read(sql, key.ToString());
        public static bool TryReadInt64(this SQLiteConnection sql, Int64Values key, out long value) => Int64Value.TryRead(sql, key.ToString(), out value);
        public static void WriteInt64(this SQLiteConnection sql, Int64Values key, long value) => Int64Value.Write(sql, key.ToString(), value);
        #endregion

        #region StringValue
        public static string ReadString(this SQLiteConnection sql, StringValues key) => StringValue.Read(sql, key.ToString());
        public static bool TryReadString(this SQLiteConnection sql, StringValues key, out string value) => StringValue.TryRead(sql, key.ToString(), out value);
        public static void WriteString(this SQLiteConnection sql, StringValues key, string value) => StringValue.Write(sql, key.ToString(), value);
        #endregion

        #region BlobValue
        public static byte[] ReadBlob(this SQLiteConnection sql, ByteArrayValues key) => ByteArrayValue.Read(sql, key.ToString());
        public static bool TryReadBlob(this SQLiteConnection sql, ByteArrayValues key, out byte[] value) => ByteArrayValue.TryRead(sql, key.ToString(), out value);
        public static void WriteBlob(this SQLiteConnection sql, ByteArrayValues key, byte[] value) => ByteArrayValue.Write(sql, key.ToString(), value);
        #endregion
    }
}
