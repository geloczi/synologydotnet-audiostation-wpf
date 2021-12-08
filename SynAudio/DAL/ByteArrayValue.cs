using SQLite;

namespace SynAudio.DAL
{
    [Table("ByteArray")]
    public class ByteArrayValue
    {
        [Column(nameof(Key))]
        [PrimaryKey]
        [MaxLength(255)]
        public string Key { get; set; }

        [Column(nameof(Value))]
        public byte[] Value { get; set; }

        public static byte[] Read(SQLiteConnection sql, string key)
        {
            var obj = sql.Table<ByteArrayValue>().FirstOrDefault(x => x.Key == key);
            return obj?.Value;
        }

        public static bool TryRead(SQLiteConnection sql, string key, out byte[] value)
        {
            var obj = sql.Table<ByteArrayValue>().FirstOrDefault(x => x.Key == key);
            value = obj?.Value;
            return !(value is null);
        }

        public static void Write(SQLiteConnection sql, string key, byte[] value)
        {
            sql.InsertOrReplace(new ByteArrayValue { Key = key, Value = value });
        }
    }
}
