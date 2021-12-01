using System;

namespace SqlCeLibrary
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TableAttribute : Attribute
    {
        public string Name { get; set; }

        public TableAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ViewAttribute : Attribute
    {
        public string TableName { get; set; }

        public ViewAttribute(string tableName)
        {
            TableName = tableName;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class ColumnAttribute : Attribute
    {
        public string Type { get; set; }
        public string Size { get; set; }
        public string Default { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class CompareIgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class NotNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
        public bool AutoIncrement { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class NullableAttribute : Attribute
    {
    }
}
