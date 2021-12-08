using SQLite;

namespace SynAudio.DAL
{
    [Table("Int64Value")]
    class Int64Value
    {
        [Column(nameof(Key))]
        [PrimaryKey]
        [MaxLength(255)]
        public string Key { get; set; }

        [Column(nameof(Value))]
        public long Value { get; set; }

        public static long Read(SQLiteConnection sql, string key)
        {
            var obj = sql.Table<Int64Value>().FirstOrDefault(x => x.Key == key);
            return obj?.Value ?? 0;
        }

        public static bool TryRead(SQLiteConnection sql, string key, out long value)
        {
            var obj = sql.Table<Int64Value>().FirstOrDefault(x => x.Key == key);
            value = obj?.Value ?? 0;
            return !(obj is null);
        }

        public static void Write(SQLiteConnection sql, string key, long value)
        {
            sql.InsertOrReplace(new Int64Value { Key = key, Value = value });
        }
    }
}
