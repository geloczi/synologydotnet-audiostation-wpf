using SqlCeLibrary;

namespace SynAudio.DAL
{
    [Table("ByteArray")]
    public class ByteArrayValue
    {
        [Column, PrimaryKey]
        public string Key { get; set; }
        [Column]
        public byte[] Value { get; set; }

        public static byte[] Read(SqlCe sql, string key) => sql.SelectFirstByPrimaryKeys<ByteArrayValue>(key)?.Value;
        public static bool TryRead(SqlCe sql, string key, out byte[] value)
        {
            var v = sql.SelectFirstByPrimaryKeys<ByteArrayValue>(key);
            value = v?.Value;
            return !(value is null);
        }
        public static void Write(SqlCe sql, string key, byte[] value)
        {
            sql.DeleteSingleByPrimaryKey<ByteArrayValue>(key);
            if (value?.Length > 0 == true)
                sql.Insert(new ByteArrayValue() { Key = key, Value = value });
        }
    }
}
