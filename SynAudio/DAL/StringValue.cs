using SQLite;

namespace SynAudio.DAL
{
    [Table("StringValue")]
    class StringValue
    {
        [Column(nameof(Key))]
        [PrimaryKey]
        [MaxLength(255)]
        public string Key { get; set; }

        [Column(nameof(Value))]
        public string Value { get; set; }

        public static string Read(SQLiteConnection sql, string key)
        {
            var obj = sql.Table<StringValue>().FirstOrDefault(x => x.Key == key);
            return obj?.Value;
        }

        public static bool TryRead(SQLiteConnection sql, string key, out string value)
        {
            var obj = sql.Table<StringValue>().FirstOrDefault(x => x.Key == key);
            value = obj?.Value;
            return !(value is null);
        }

        public static void Write(SQLiteConnection sql, string key, string value)
        {
            sql.InsertOrReplace(new StringValue { Key = key, Value = value });
        }
    }
}
