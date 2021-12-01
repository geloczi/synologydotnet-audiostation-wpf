using SqlCeLibrary;

namespace SynAudio.DAL
{
    [Table("StringValue")]
    class StringValue
    {
        [Column, PrimaryKey]
        public string Key { get; set; }

        [Column(Size = "1000"), NotNull]
        public string Value { get; set; }

        public static string Read(SqlCe sql, string key) => sql.SelectFirstByPrimaryKeys<StringValue>(key)?.Value;
        public static bool TryRead(SqlCe sql, string key, out string value) => !((value = sql.SelectFirstByPrimaryKeys<StringValue>(key)?.Value) is null);
        public static void Write(SqlCe sql, string key, string value)
        {
            sql.DeleteSingleByPrimaryKey<StringValue>(key);
            if (!(value is null))
                sql.Insert(new StringValue() { Key = key, Value = value });
        }
    }
}
