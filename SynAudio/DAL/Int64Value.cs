using SqlCeLibrary;

namespace SynAudio.DAL
{
    [Table("Int64Value")]
    class Int64Value
    {
        [Column, PrimaryKey]
        public string Key { get; set; }
        [Column, NotNull]
        public long Value { get; set; }

        public static long? Read(SqlCe sql, string key) => sql.SelectFirstByPrimaryKeys<Int64Value>(key)?.Value;
        public static bool TryRead(SqlCe sql, string key, out long value)
        {
            var v = sql.SelectFirstByPrimaryKeys<Int64Value>(key);
            value = v is null ? 0 : v.Value;
            return v is null;
        }
        public static void Write(SqlCe sql, string key, long? value)
        {
            sql.DeleteSingleByPrimaryKey<Int64Value>(key);
            if (value.HasValue)
                sql.Insert(new Int64Value() { Key = key, Value = value.Value });
        }
    }
}
