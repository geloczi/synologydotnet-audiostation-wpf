using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SqlCeLibrary
{
    public class ColumnInfo
    {
        /// <summary>
        /// Name of the column. Matches with the property name in the entity.
        /// </summary>
        public string Name { get; private set; }
        public string NameWithBrackets => '[' + Name + ']';
        public string DataType { get; private set; }
        public bool IsPrimaryKey { get; private set; }
        public bool IsNullable { get; private set; }
        public bool IsCompareIgnore { get; private set; }
        public PropertyInfo Property { get; private set; }
        public bool AutoIncrement { get; private set; }
        private string _formatted;
        public string FormattedWithDataType
        {
            get
            {
                if (_formatted is null)
                {
                    var components = new List<string>();
                    components.Add($"[{Name}] {DataType}");
                    if (!IsPrimaryKey)
                    {
                        if (IsNullable)
                            components.Add("NULL");
                        else
                            components.Add("NOT NULL");
                    }
                    _formatted = string.Join(" ", components);
                }
                return _formatted;
            }
        }

        public ColumnInfo() { }
        public ColumnInfo(PropertyInfo prop)
        {
            Name = prop.Name;
            Property = prop;
            IsCompareIgnore = !(prop.GetCustomAttribute<CompareIgnoreAttribute>(false) is null);
            IsNullable = !(prop.GetCustomAttribute<NotNullAttribute>(false) is null) && prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) || !(prop.GetCustomAttribute<NullableAttribute>(false) is null);

            var pkAttr = prop.GetCustomAttribute<PrimaryKeyAttribute>(false);
            if (!(pkAttr is null))
            {
                IsPrimaryKey = true;
                AutoIncrement = pkAttr.AutoIncrement;
            }

            DataType = GetDataTypeForProperty(prop);
        }
        public override string ToString()
        {
            return NameWithBrackets;
        }

        public object GetValue(object o)
        {
            var value = Property.GetValue(o);
            if (value is null)
                return DBNull.Value;
            else if (value is DateTime dt)
                return dt.Ticks;
            else if (value is TimeSpan ts)
                return ts.Ticks;
            else
                return value;
        }

        public string GetDataTypeForProperty(PropertyInfo prop)
        {
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>(false);
            var result = columnAttr.Type;
            var size = columnAttr.Size;

            // DataTypeAttribute
            if (string.IsNullOrEmpty(result))
            {
                if (prop.PropertyType == typeof(string))
                {
                    result = "NVARCHAR";
                    if (string.IsNullOrEmpty(size))
                        size = "256";
                }
                else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                    result = "INT";
                else if (prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?))
                    result = "BIGINT";
                else if (prop.PropertyType == typeof(byte) || prop.PropertyType == typeof(byte?))
                    result = "TINYINT";
                else if (prop.PropertyType == typeof(TimeSpan) || prop.PropertyType == typeof(TimeSpan?))
                    result = "BIGINT"; //To keep the precision, and there's no SQL equivalent
                else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                    result = "BIGINT"; //To keep the precision, do not use the 'datetime' SQL type
                else if (prop.PropertyType == typeof(bool))
                    result = "BIT";
                else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(float))
                    result = "FLOAT";
                else if (prop.PropertyType == typeof(byte[]))
                    result = "IMAGE";
                else
                    throw new NotImplementedException();
            }

            if (!string.IsNullOrEmpty(size))
                result += $"({size})";

            if (!string.IsNullOrEmpty(columnAttr.Default))
                result += $" DEFAULT {columnAttr.Default}";

            return result;
            /* Available types in Sql Ce
				System.Boolean 	bit
				System.Byte 		tinyint
				System.SByte 		numeric(3,0)
				System.Char 		nchar(1)
				System.Decimal 	numeric(19,4)
				System.Double 		float
				System.Single 		real
				System.Int16 		smallint
				System.UInt16 		numeric(5,0)
				System.Int32 		int
				System.UInt32 		numeric(10,0)
				System.Int64 		bigint
				System.UInt64 		numeric(20,0)
				System.Guid 		uniqueidentifier
				System.String 		nvarchar
				System.String	 	ntext (Unlimited size string, see SizeAttribute)
				System.DateTime 	datetime
				System.TimeSpan 	float
				System.Byte[] 		image or varbinary(N)
				
				Note: "nvarchar(max)" is not supported, "ntext" can be used instead
			*/
        }

        public void SetValue(object entity, object value)
        {
            if (value is DBNull)
                value = null;
            else if (!(value is null))
            {
                if (Property.PropertyType == typeof(DateTime))
                    value = new DateTime((long)value);
                else if (Property.PropertyType == typeof(DateTime?))
                    value = new DateTime((long)value);
                else if (Property.PropertyType == typeof(TimeSpan))
                    value = new TimeSpan((long)value);
                else if (Property.PropertyType == typeof(TimeSpan?))
                    value = new TimeSpan((long)value);
                else if (Property.PropertyType == typeof(long?))
                    value = new long?(Convert.ToInt64(value));
                else if (Property.PropertyType == typeof(int?))
                    value = new int?(Convert.ToInt32(value));
                else if (Property.PropertyType == typeof(int))
                    value = Convert.ToInt32(value);
                else if (Property.PropertyType == typeof(bool?))
                    value = new bool?(Convert.ToBoolean(value));
                else if (Property.PropertyType == typeof(bool))
                    value = Convert.ToBoolean(value);
            }
            Property.SetValue(entity, value);
        }
    }
}
