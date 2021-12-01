using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SqlCeLibrary
{
    public class TableInfo
    {
        private static readonly Dictionary<Type, TableInfo> _typeDictionary = new Dictionary<Type, TableInfo>();

        public string Name { get; }
        public bool IsView { get; }
        public ColumnInfo[] Columns { get; }
        public Dictionary<string, ColumnInfo> ColumnsByName { get; }
        public ColumnInfo[] PrimaryKeyColumns { get; }
        public Type Type { get; }
        public string NameWithBrackets => '[' + Name + ']';
        private Dictionary<string, string> _propertyNameToColumnNameDict;

        public TableInfo(Type type, string name, bool isView, ColumnInfo[] columns)
        {
            Type = type;
            Name = name;
            IsView = isView;
            Columns = columns;
            ColumnsByName = columns.ToDictionary(k => k.Name, v => v);
            PrimaryKeyColumns = columns.Where(x => x.IsPrimaryKey).ToArray();
            _propertyNameToColumnNameDict = Columns.ToDictionary(k => k.Property.Name, v => v.NameWithBrackets);
        }

        public string this[string propertyName] => _propertyNameToColumnNameDict[propertyName];

        public override string ToString() => NameWithBrackets;

        public string GetCreateScript()
        {
            if (IsView)
                throw new InvalidOperationException("Can't generate create script for views.");
            var sb = new StringBuilder();
            sb.Append("CREATE TABLE ");
            sb.Append(NameWithBrackets);
            sb.AppendLine(" (");

            var primaryKeyCols = Columns.Where(c => c.IsPrimaryKey).ToArray();

            if (primaryKeyCols.Length > 1)
            {
                sb.AppendLine(string.Join(Environment.NewLine + ",", Columns.Select(col => col.FormattedWithDataType)));
                sb.AppendFormat(",PRIMARY KEY({0})", string.Join(",", primaryKeyCols.Select(c => c.NameWithBrackets)));
            }
            else if (primaryKeyCols.Length == 1)
            {
                if (primaryKeyCols[0].AutoIncrement)
                    sb.AppendLine($"{primaryKeyCols[0].FormattedWithDataType} IDENTITY PRIMARY KEY");
                else
                    sb.AppendLine($"{primaryKeyCols[0].FormattedWithDataType} PRIMARY KEY");

                var others = Columns.Where(c => !c.IsPrimaryKey).ToArray();
                if (others.Length > 0)
                    sb.AppendLine("," + string.Join(Environment.NewLine + ",", others.Select(col => col.FormattedWithDataType)));
            }
            else
            {
                sb.AppendLine(string.Join(Environment.NewLine + ",", Columns.Select(col => col.FormattedWithDataType)));
            }

            sb.AppendLine(")");
            return sb.ToString();
        }

        public bool Compare(object a, object b, bool caseInsenstive = false)
        {
            foreach (var col in Columns.Where(c => !c.IsCompareIgnore))
            {
                var aValue = col.GetValue(a);
                var bValue = col.GetValue(b);
                if (aValue is null ^ bValue is null)
                    return false;

                if (caseInsenstive && col.Property.PropertyType == typeof(string) && !(aValue is null))
                    return ((string)aValue).Equals((string)bValue, StringComparison.OrdinalIgnoreCase);

                if (aValue?.Equals(bValue) != true)
                    return false;
            }
            return true;
        }

        public object[] GetPrimaryKeys(object o) => PrimaryKeyColumns.Select(col => col.GetValue(o)).ToArray();

        public ColumnInfo[] Diff(object a, object b)
        {
            var changedColumns = new List<ColumnInfo>();
            foreach (var col in Columns)
            {
                var left = col.GetValue(a);
                var right = col.GetValue(b);

                // (left as byte[]).SequenceEqual(right as byte[])
                if (left is Array leftArray && right is Array rightArray)
                {
                    if (leftArray.Length != rightArray.Length)
                    {
                        changedColumns.Add(col);
                    }
                    else
                    {
                        for (int i = 0; i < leftArray.Length; i++)
                        {
                            if (leftArray.GetValue(i)?.Equals(rightArray.GetValue(i)) != true)
                            {
                                changedColumns.Add(col);
                                break;
                            }
                        }
                    }
                }
                else if (left?.Equals(right) != true)
                    changedColumns.Add(col);
            }
            return changedColumns.ToArray();
        }

        public int CopyDifferentProperties(object from, object to)
        {
            var diff = Diff(from, to);
            foreach (var col in diff)
            {
                var val = col.GetValue(from);
                col.SetValue(to, val);
            }
            return diff.Length;
        }

        public void CopyAllProperties(object from, object to)
        {
            foreach (var col in Columns)
            {
                var val = col.GetValue(from);
                col.SetValue(to, val);
            }
        }

        public static TableInfo Get<T>() => Get(typeof(T));
        public static TableInfo Get(Type t)
        {
            if (!_typeDictionary.TryGetValue(t, out var tableInfo))
            {
                var isView = false;
                var tableName = (t.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute)?.Name;
                if (tableName is null)
                {
                    tableName = (t.GetCustomAttributes(typeof(ViewAttribute), true).FirstOrDefault() as ViewAttribute)?.TableName;
                    isView = true;
                }
                if (tableName is null)
                    throw new ArgumentException($"TableAttribute/ViewAttribute is missing from type '{t.Name}'", nameof(t));

                var allProps = t.GetProperties().ToArray();
                var keyProp = allProps.FirstOrDefault(prop => Attribute.IsDefined(prop, typeof(PrimaryKeyAttribute)));
                var orderedProps = new List<PropertyInfo>();
                if (!(keyProp is null))
                    orderedProps.Add(keyProp);
                orderedProps.AddRange(allProps.Where(prop => Attribute.IsDefined(prop, typeof(ColumnAttribute)) && prop != keyProp));

                tableInfo = new TableInfo(t, tableName, isView, orderedProps.Select(p => new ColumnInfo(p)).ToArray());

                if (!tableInfo.Columns.Any())
                    throw new NotImplementedException($"The '{t.FullName}' type does not implement columns");

                if (tableInfo.Columns.Count(c => c.AutoIncrement) > 1)
                    throw new NotSupportedException($"The '{t.FullName}' type contains more than one {nameof(PrimaryKeyAttribute.AutoIncrement)} attributes.");

                _typeDictionary.Add(t, tableInfo);
            }
            return tableInfo;
        }
    }
}
