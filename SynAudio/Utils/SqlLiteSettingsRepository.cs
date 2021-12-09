using SQLite;
using SynAudio.DAL;

namespace Utils
{
    class SqlLiteSettingsRepository : ISettingsRepository
    {
        private readonly SQLiteConnection _sql;

        public SqlLiteSettingsRepository(SQLiteConnection sqlLiteConnection)
        {
            _sql = sqlLiteConnection;
        }

        #region Int64Value

        public long? ReadInt64(Int64Values key) => Int64Value.Read(_sql, key.ToString());
        public bool TryReadInt64(Int64Values key, out long value) => Int64Value.TryRead(_sql, key.ToString(), out value);
        public void WriteInt64(Int64Values key, long value) => Int64Value.Write(_sql, key.ToString(), value);

        #endregion Int64Value

        #region StringValue

        public string ReadString(StringValues key) => StringValue.Read(_sql, key.ToString());
        public bool TryReadString(StringValues key, out string value) => StringValue.TryRead(_sql, key.ToString(), out value);
        public void WriteString(StringValues key, string value) => StringValue.Write(_sql, key.ToString(), value);

        #endregion StringValue

        #region BlobValue

        public byte[] ReadBlob(ByteArrayValues key) => ByteArrayValue.Read(_sql, key.ToString());
        public bool TryReadBlob(ByteArrayValues key, out byte[] value) => ByteArrayValue.TryRead(_sql, key.ToString(), out value);
        public void WriteBlob(ByteArrayValues key, byte[] value) => ByteArrayValue.Write(_sql, key.ToString(), value);

        #endregion BlobValue

    }
}
