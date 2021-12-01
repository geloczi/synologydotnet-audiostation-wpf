using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqlCeLibrary
{
    public class SqlCe : IDisposable
    {
        public SqlCeConnection Connection { get; private set; }
        public string FilePath { get; }
        public bool NewFileCreated { get; }
        public string Namespace { get; }

        public SqlCe(string filePath) : this(null, filePath, false) { }
        public SqlCe(string filePath, bool caseSensitive) : this(null, filePath, caseSensitive) { }
        public SqlCe(string typeNamespace, string filePath, bool caseSensitive)
        {
            Namespace = typeNamespace;
            FilePath = filePath;
            var connString = $"DataSource=\"{filePath}\"; Case Sensitive={caseSensitive};";

            if (!File.Exists(FilePath))
            {
                using (var engine = new SqlCeEngine(connString))
                {
                    engine.CreateDatabase();
                }
                NewFileCreated = true;
            }
            Connection = new SqlCeConnection(connString);
            Connection.Open();

            GenerateStructure();
        }

        public void Dispose()
        {
            if (!(Connection is null))
            {
                Connection.Close();
                Connection.Dispose();
                Connection = null;
            }
        }

        public void ExecuteNonQuery(string commandText, params object[] parameters)
        {
            using (var cmd = CreateCommand(commandText, parameters))
                cmd.ExecuteNonQuery();
        }

        public async Task ExecuteNonQueryAsync(string commandText, params object[] parameters)
        {
            using (var cmd = CreateCommand(commandText, parameters))
                await cmd.ExecuteNonQueryAsync();
        }

        public object ExecuteScalar(string commandText, params object[] parameters)
        {
            using (var cmd = CreateCommand(commandText, parameters))
            {
                var result = cmd.ExecuteScalar();
                return result;
            }
        }

        public async Task<object> ExecuteScalarAsync(string commandText, params object[] parameters)
        {
            using (var cmd = CreateCommand(commandText, parameters))
                return await cmd.ExecuteScalarAsync();
        }

        public async Task<T> ExecuteScalarAsync<T>(string commandText, params object[] parameters)
        {
            using (var cmd = CreateCommand(commandText, parameters))
            {
                var value = await cmd.ExecuteScalarAsync();
                return (T)value;
            }
        }

        public void ExecuteReader(string commandText, Action<DbDataReader> readerMethod, params object[] parameters)
        {
            using (var cmd = CreateCommand(commandText, parameters))
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    readerMethod(reader);
            }
        }

        public async Task ExecuteReaderAsync(string commandText, Action<DbDataReader> readerMethod, params object[] parameters)
        {
            using (var cmd = CreateCommand(commandText, parameters))
            {
                var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                    readerMethod(reader);
            }
        }

        /// <summary>
        /// Creates a command. 
        /// Help: https://www.sqlite.org/lang.html
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public SqlCeCommand CreateCommand(string commandText, params object[] parameters)
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = commandText;
            if (!(parameters is null))
                for (int i = 0; i < parameters.Length; i++)
                    cmd.Parameters.Add(new SqlCeParameter('@' + i.ToString(), parameters[i]));
            return cmd;
        }

        public SqlCeTransaction BeginTransaction() => Connection.BeginTransaction();

        public void Insert<T>(params T[] entities)
        {
            var tableInfo = TableInfo.Get<T>();
            var columns = tableInfo.Columns.Where(c => !c.AutoIncrement).ToArray();
            var autoIncrementColumn = tableInfo.Columns.FirstOrDefault(c => c.AutoIncrement);
            var sb = new StringBuilder();
            sb.Append("INSERT INTO ");
            sb.Append(tableInfo.ToString());

            // Column names
            sb.Append(" (");
            sb.Append(string.Join(", ", columns.Select(a => a.NameWithBrackets)));
            sb.AppendLine(")");

            // Values
            sb.Append("VALUES (");
            sb.Append(string.Join(", ", Enumerable.Range(0, columns.Length).Select(a => $"@{a}")));
            sb.AppendLine(")");

            T debugEntity = default(T);
            try
            {
                using (var cmd = CreateCommand(sb.ToString()))
                using (var idCmd = CreateCommand("SELECT @@IDENTITY"))
                {
                    foreach (var entity in entities)
                    {
                        debugEntity = entity;
                        // Insert entity
                        for (int i = 0; i < columns.Length; i++)
                        {
                            var paramName = $"@{i}";
                            var v = columns[i].GetValue(entity);
                            if (cmd.Parameters.Contains(paramName))
                                cmd.Parameters[paramName].Value = v;
                            else
                                cmd.Parameters.AddWithValue(paramName, v);
                        }
                        cmd.ExecuteNonQuery();

                        // Save the generated identity value to the entity
                        if (!(autoIncrementColumn is null))
                        {
                            var id = idCmd.ExecuteScalar();
                            autoIncrementColumn.SetValue(entity, id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var pk = tableInfo.Columns.Where(c => c.IsPrimaryKey).Select(a => a.GetValue(debugEntity)).ToArray();
                throw new Exception($"Error while inserting entity. PrimaryKey='{string.Join("|", pk)}'", ex);
            }
        }

        private PreparedParameter[] GetWhereParametersForEntity<T>(T entity)
        {
            var tableInfo = TableInfo.Get<T>();
            var pkCols = tableInfo.PrimaryKeyColumns;
            var result = new PreparedParameter[pkCols.Length];
            for (int i = 0; i < pkCols.Length; i++)
            {
                result[i] = new PreparedParameter()
                {
                    Index = i,
                    Column = pkCols[i].NameWithBrackets,
                    Value = pkCols[i].GetValue(entity)
                };
            }
            return result;
        }

        private PreparedParameter[] GetWhereParameters(TableInfo tableInfo, object[] primaryKeyValues)
        {
            var pkCols = tableInfo.PrimaryKeyColumns;
            var result = new PreparedParameter[pkCols.Length];
            for (int i = 0; i < pkCols.Length; i++)
            {
                result[i] = new PreparedParameter()
                {
                    Index = i,
                    Column = pkCols[i].NameWithBrackets,
                    Value = primaryKeyValues[i]
                };
            }
            return result;
        }

        private SqlCeCommand CreateUpdateCommand<T>(T entity, string[] fieldsToUpdate, out ColumnInfo[] dataColumns)
        {
            var tableInfo = TableInfo.Get<T>();
            dataColumns = tableInfo.Columns.Where(c => !c.IsPrimaryKey && (fieldsToUpdate is null || fieldsToUpdate.Length == 0 || fieldsToUpdate.Contains(c.Name))).ToArray();
            var pkCols = tableInfo.PrimaryKeyColumns;

            // Primary keys iare the first parameters
            var parameters = new PreparedParameter[pkCols.Length + dataColumns.Length];
            var pkParameters = GetWhereParametersForEntity(entity);
            for (int i = 0; i < pkParameters.Length; i++)
                parameters[i] = pkParameters[i];

            // Then the others
            for (int i = 0; i < dataColumns.Length; i++)
            {
                parameters[i + pkCols.Length] = new PreparedParameter()
                {
                    Index = i + pkCols.Length,
                    Column = dataColumns[i].NameWithBrackets,
                    Value = dataColumns[i].GetValue(entity)
                };
            }

            var sb = new StringBuilder();
            sb.Append("UPDATE ");
            sb.AppendLine(tableInfo.ToString());
            sb.Append(" SET ");
            sb.AppendLine(string.Join(", ", parameters.Skip(pkParameters.Length).Select(p => p.ToString())));

            sb.AppendLine("WHERE ");
            for (int i = 0; i < pkCols.Length; i++)
            {
                if (i > 0)
                    sb.Append(" AND ");
                sb.Append(pkCols[i].NameWithBrackets);
                sb.AppendLine($" = @{i}");
            }

            return CreateCommand(sb.ToString(), parameters.Select(a => a.Value).ToArray());
        }

        public void Update<T>(T entity, params string[] fieldsToUpdate)
        {
            using (var cmd = CreateUpdateCommand(entity, fieldsToUpdate, out _))
                cmd.ExecuteNonQuery();
        }

        public void Update<T>(T[] entities, params string[] fieldsToUpdate)
        {
            var tableInfo = TableInfo.Get<T>();
            var pkCols = tableInfo.PrimaryKeyColumns;
            using (var cmd = CreateUpdateCommand(entities[0], fieldsToUpdate, out var dataCols))
            {
                foreach (var entity in entities)
                {
                    // Set primary key values
                    for (int i = 0; i < pkCols.Length; i++)
                        cmd.Parameters[i].Value = pkCols[i].GetValue(entity);

                    // Set data values
                    for (int i = 0; i < dataCols.Length; i++)
                        cmd.Parameters[i + pkCols.Length].Value = dataCols[i].GetValue(entity);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public T SelectSingleEntity<T>(T entity, params string[] fieldsToLoad)
        {
            var tableInfo = TableInfo.Get<T>();
            var parameters = GetWhereParametersForEntity(entity);
            var sb = new StringBuilder();
            sb.Append("SELECT TOP 1 ");
            sb.Append(string.Join(", ", fieldsToLoad?.Length > 0 ? fieldsToLoad : tableInfo.Columns.Select(c => c.NameWithBrackets)));
            sb.Append(" FROM ");
            sb.Append(tableInfo.ToString());
            sb.Append(" WHERE ");
            sb.Append(string.Join(" AND ", parameters.Select(p => p.ToString())));

            T result = default(T);
            ExecuteReader(sb.ToString(), (r) =>
            {
                result = CreateInstance<T>(r, tableInfo);
            }, parameters.Select(p => p.Value).ToArray());
            return result;
        }

        public T SelectFirst<T>(string where, object[] parameters)
        {
            var ti = TableInfo.Get<T>();
            var query = $"SELECT TOP 1 * FROM {ti} {where}";
            T result = default(T);
            using (var cmd = CreateCommand(query, parameters))
            {
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                    result = CreateInstanceDynamicColumns<T>(reader, ti);
            }
            return result;
        }

        public T SelectFirstByPrimaryKeys<T>(params object[] primaryKeys)
        {
            if (!(primaryKeys?.Length > 0))
                throw new ArgumentException(nameof(primaryKeys));
            var tableInfo = TableInfo.Get<T>();
            var parameters = GetWhereParameters(tableInfo, primaryKeys);
            var query = $"SELECT TOP 1 * FROM {tableInfo.NameWithBrackets} WHERE {string.Join(" AND ", parameters.Select(p => p.ToString()))}";
            T result = default(T);
            ExecuteReader(query, (r) =>
            {
                result = CreateInstance<T>(r, tableInfo);
            }, parameters.Select(p => p.Value).ToArray());
            return result;
        }

        public T[] Select<T>() => Select<T>(null);
        public T[] Select<T>(string where, params object[] parameters)
        {
            var tableInfo = TableInfo.Get<T>();
            var query = GetSelectQuery(tableInfo.NameWithBrackets, tableInfo.Columns.Select(x => x.NameWithBrackets).ToArray(), where);
            var entities = new List<T>();
            ExecuteReader(query, (r) =>
            {
                entities.Add(CreateInstance<T>(r, tableInfo));
            }, parameters);
            return entities.ToArray();
        }

        public T[] SelectCustom<T>(string query, params object[] parameters)
        {
            var tableInfo = TableInfo.Get<T>();
            var sb = new StringBuilder();
            var entities = new List<T>();
            ExecuteReader(query, (r) =>
            {
                entities.Add(CreateInstanceDynamicColumns<T>(r, tableInfo));
                //entities.Add(CreateInstance<T>(r, tableInfo));
            }, parameters);
            return entities.ToArray();
        }

        //public async Task<T[]> SelectAsync<T>(string where = null, params object[] parameters)
        //{
        //	var tableInfo = TableInfo.Get<T>();
        //	var query = GetSelectQuery(tableInfo.NameWithBrackets, tableInfo.Columns.Select(x => x.NameWithBrackets).ToArray(), where);
        //	var entities = new List<T>();
        //	await ExecuteReaderAsync(query, (r) =>
        //	{
        //		entities.Add(CreateInstance<T>(r, tableInfo));
        //	}, parameters);
        //	return entities.ToArray();
        //}

        public void DeleteSingle<T>(T entity)
        {
            var tableInfo = TableInfo.Get<T>();
            var parameters = GetWhereParametersForEntity(entity);
            var query = $"DELETE FROM {tableInfo.NameWithBrackets} WHERE {string.Join(" AND ", parameters.Select(p => p.ToString()))}";
            ExecuteNonQuery(query, parameters.Select(p => p.Value).ToArray());
        }

        public void Delete<T>()
        {
            var tableInfo = TableInfo.Get<T>();
            var query = $"DELETE FROM {tableInfo.NameWithBrackets}";
            ExecuteNonQuery(query);
        }

        public void Delete<T>(string where, params object[] parameters)
        {
            var tableInfo = TableInfo.Get<T>();
            var query = $"DELETE FROM {tableInfo.NameWithBrackets} {where}";
            ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKeyValues">The primary key values of the entity to delete. The value order must match the order of the primary key proerties on the type.</param>
        public void DeleteSingleByPrimaryKey<T>(params object[] primaryKeyValues)
        {
            var tableInfo = TableInfo.Get<T>();
            var pkCols = tableInfo.Columns.Where(c => c.IsPrimaryKey).ToArray();
            if (pkCols.Length == 0)
                throw new NotSupportedException("The type does not define primary key(s).");
            if (primaryKeyValues.Length != pkCols.Length)
                throw new ArgumentException("The number of items mutch match the number of primary key columns on the type.", nameof(primaryKeyValues));

            var filter = new List<string>();
            for (int i = 0; i < pkCols.Length; i++)
                filter.Add($"{pkCols[i].NameWithBrackets} = @{i}");
            var query = $"DELETE FROM {tableInfo.NameWithBrackets} WHERE {string.Join(" AND ", filter)}";
            ExecuteNonQuery(query, primaryKeyValues);
        }

        public void DeleteMultipleByPrimaryKey<T>(ICollection<object[]> primaryKeyValueCollection)
        {
            if (primaryKeyValueCollection.Count == 0)
                return;
            var tableInfo = TableInfo.Get<T>();
            var pkCols = tableInfo.PrimaryKeyColumns;
            if (pkCols.Length == 0)
                throw new InvalidOperationException($"{tableInfo.NameWithBrackets} does not contain any PK column.");

            var filter = new List<string>();
            for (int i = 0; i < pkCols.Length; i++)
                filter.Add($"{pkCols[i].NameWithBrackets} = @{i}");
            var query = $"DELETE FROM {tableInfo.NameWithBrackets} WHERE {string.Join(" AND ", filter)}";
            using (var cmd = CreateCommand(query, primaryKeyValueCollection.First()))
            {
                foreach (var pkValues in primaryKeyValueCollection)
                {
                    for (int i = 0; i < pkValues.Length; i++)
                        cmd.Parameters[i].Value = pkValues[i];
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //public async Task DeleteAsync<T>(string id)
        //{
        //	var tableInfo = GetTableInfoForType(typeof(T));
        //	var query = $"DELETE FROM {tableInfo.NameWithBrackets} WHERE {tableInfo.Columns.Single(c => c.IsPrimaryKey).NameWithBrackets} = @0";
        //	await ExecuteNonQueryAsync(query, id);
        //}

        /// <summary>
        /// You can write the entire SELECT statement by yourself. The selected columns automatically loaded to the specified type mapped by property name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<T[]> QueryAsync<T>(string query)
        {
            var tableInfo = TableInfo.Get<T>();
            var columns = tableInfo.Columns;
            var entities = new List<T>();
            var columnMap = new Dictionary<string, ColumnInfo>();
            await ExecuteReaderAsync(query, (r) =>
            {
                var entity = Activator.CreateInstance<T>();
                for (int i = 0; i < r.FieldCount; i++)
                {
                    var n = r.GetName(i);
                    if (!columnMap.TryGetValue(n, out var col))
                    {
                        col = columns.FirstOrDefault(a => a.Name.Equals(r.GetName(i), StringComparison.OrdinalIgnoreCase));
                        columnMap[n] = col;
                    }
                    if (!(col is null))
                    {
                        var v = r[i];
                        col.SetValue(entity, v);
                    }
                }
                entities.Add(entity);
            });
            return entities.ToArray();
        }

        private static string GetSelectQuery(string tableName, string[] columns, string where)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(string.Join(", ", columns));
            sb.AppendLine();
            sb.Append("FROM ");
            sb.AppendLine(tableName);
            if (!string.IsNullOrEmpty(where))
                sb.AppendLine(where);
            return sb.ToString();
        }

        private static T CreateInstance<T>(DbDataReader r, TableInfo tableInfo)
        {
            var obj = Activator.CreateInstance<T>();
            for (int i = 0; i < tableInfo.Columns.Length; i++)
                tableInfo.Columns[i].SetValue(obj, r[tableInfo.Columns[i].Name]);
            return obj;
        }

        private static T CreateInstanceDynamicColumns<T>(DbDataReader r, TableInfo tableInfo)
        {
            var obj = Activator.CreateInstance<T>();
            for (int i = 0; i < r.FieldCount; i++)
                tableInfo.ColumnsByName[r.GetName(i)].SetValue(obj, r[tableInfo.Columns[i].Name]);
            return obj;
        }

        /// <summary>
        /// Splits a big array of items into multiple smaller arrays using the specified batch size.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static IEnumerable<T[]> PageCollection<T>(IEnumerable<T> collection, int pageSize)
        {
            int total = collection.Count();
            if (total <= pageSize)
                yield return collection.ToArray();
            int start = 0;
            while (start < total)
            {
                yield return collection.Skip(start).Take(pageSize).ToArray();
                start += pageSize;
            }
        }

        private void GenerateStructure()
        {
            var existingTables = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            ExecuteReader("SELECT [TABLE_NAME] FROM INFORMATION_SCHEMA.TABLES ORDER BY [TABLE_NAME]", (r) => { existingTables.Add(r.GetString(0)); });

            var tablesInfos = new List<TableInfo>();
            var types = Assembly.GetEntryAssembly().GetTypes();
            foreach (var t in types)
            {
                if (!string.IsNullOrEmpty(Namespace) && !t.Namespace.Equals(Namespace))
                    continue;

                var tableAttribute = t.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
                if (tableAttribute is null)
                    continue;

                tablesInfos.Add(TableInfo.Get(t));
            }

            var tablesToDelete = existingTables.Where(x => !tablesInfos.Any(ti => ti.Name.Equals(x, StringComparison.OrdinalIgnoreCase))).ToArray();
            var tablesToCreate = tablesInfos.Where(ti => !existingTables.Contains(ti.Name)).ToArray();

            foreach (var tableName in tablesToDelete)
            {
                try
                {
                    ExecuteNonQuery($"DROP TABLE [{tableName}]");
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex.ToString());
#endif
                }
            }

            foreach (var ti in tablesToCreate)
            {
                var createScript = ti.GetCreateScript();
                ExecuteNonQuery(createScript);
            }
        }

        private struct PreparedParameter
        {
            public string Column;
            public int Index;
            public object Value;

            public override string ToString()
            {
                return $"{Column} = @{Index}";
            }
        }

    }
}
