using DynamicFilter.SqlServer.Properties;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DynamicFilter.SqlServer
{
    public class TableMetadataProvider : ITableMetadataProvider
    {
        private readonly IDbConnection _dbConnection;
        public TableMetadataProvider(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public ITableMetadata[] GetTablesMetadata(params string[] tablesNames)
        {
            try
            {
                _dbConnection.Open();
                using var sqlCommand = CreateDbCommand(tablesNames);
                using var dataReader = sqlCommand.ExecuteReader();
                return BuildMetadata(dataReader, tablesNames);
            }
            finally
            {
                _dbConnection.Close();
            }
        }
        
        private ITableMetadata[] BuildMetadata(IDataReader dataReader, params string[] tablesNames)
        {
            var tablesColumns = tablesNames.ToDictionary(k => k, v => new List<Column>());

            while (dataReader.Read())
            {
                var tableName = dataReader["TABLE_NAME"].ToString();
                if (string.IsNullOrEmpty(tableName))
                {
                    continue;
                }
                var uesNoNullable = dataReader["IS_NULLABLE"]?.ToString() ?? "NO";
                var nullable = uesNoNullable == "YES";
                var column = new Column
                {
                    Name = dataReader["COLUMN_NAME"]?.ToString() ?? string.Empty,
                    Type = MapDbType(dataReader["DATA_TYPE"], nullable),
                    Nullable = nullable,
                    PrimaryKey = Convert.ToBoolean(dataReader["PK"]),
                    ForeignKey = Convert.ToBoolean(dataReader["FK"])
                };
                tablesColumns[tableName].Add(column);
            }

            return tablesColumns
                .Select(kv => new TableMetadata
                    {
                        TableName = kv.Key,
                        Columns = kv.Value
                    })
                .ToArray();
        }

        private Type MapDbType(object type, bool nullable)
        {
            switch (type.ToString())
            {
                case "int": { return nullable ? typeof(int?) : typeof(int); }
                case "varchar":
                case "nvarchar": { return typeof(string); }
                case "decimal": { return nullable ?  typeof(decimal?) : typeof(decimal); }
                case "datetime":
                case "datetime2": { return nullable ?  typeof(DateTime?) : typeof(DateTime); }
                case "bit": { return nullable ?  typeof(bool?) : typeof(bool); }
                case "float": { return nullable ?  typeof(double?) : typeof(double); }
                default:
                    throw new NotSupportedException($"'{type}' db type not mapped yet!");
            }
        }

        private IDbCommand CreateDbCommand(params string[] tablesNames)
        {
            var sqlCommand = _dbConnection.CreateCommand();
            sqlCommand.CommandText = Resources.TABLE_METADATA_SQL2019_QUERY;

            if (tablesNames.Length > 0)
            {
                for (int i = 0; i < tablesNames.Length; i++)
                {
                    string? pName = tablesNames[i];
                    var tableNameParameter = new SqlParameter($"tableName_{i}", pName);
                    sqlCommand.Parameters.Add(tableNameParameter);
                }
                sqlCommand.CommandText += string.Format(Resources.TABLE_METADATA_FILTER_IN_FORMAT, string.Join(", ", sqlCommand.Parameters.Cast<SqlParameter>().Select(x => $"@{x.ParameterName}")));
            }
            return sqlCommand;
        }
    }
}