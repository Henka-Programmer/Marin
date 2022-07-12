using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MarinFramework.Sql
{
    internal enum CONFDELTYPES
    {
        RESTRICT,
        NO_ACTION,
        CASCADE,
        SET_NULL,
        SET_DEFAULT
    }

    internal enum TABLEKIND
    {
        BASE_TABLE,
        VIEW,
        FOREIGN_TABLE,
        LOCAL_TEMPORARY,
    }

    internal static class SqlHeper
    {
        static System.Collections.Generic.Dictionary<CONFDELTYPES, string> _CONFDELTYPES = new System.Collections.Generic.Dictionary<CONFDELTYPES, string>
        {
            [CONFDELTYPES.RESTRICT] = "r",
            [CONFDELTYPES.NO_ACTION] = "a",
            [CONFDELTYPES.CASCADE] = "c",
            [CONFDELTYPES.SET_NULL] = "n",
            [CONFDELTYPES.SET_DEFAULT] = "d"
        };
        static System.Collections.Generic.Dictionary<string, string> _TABLE_KIND = new System.Collections.Generic.Dictionary<string, string>
        {
            ["BASE TABLE"] = "r",
            ["VIEW"] = "v",
            ["FOREIGN TABLE"] = "f",
            ["LOCAL TEMPORARY"] = "t"
        };

        /// <summary>
        /// Return the names of existing tables among tablenames. 
        /// </summary> 
        internal static string[] ExistingTables(Cursor cr, params string[] tableNames)
        {
            var query = @"SELECT c.relname
          FROM pg_class c
          JOIN pg_namespace n ON(n.oid = c.relnamespace)
          WHERE c.relname IN (@tableNames)
          AND c.relkind IN('r', 'v', 'm')
          AND n.nspname = 'public'";
            cr.Execute(query, new
            {
                tableNames = $"'{string.Join("', '", tableNames)}'"
            });
            return cr.FetchAll().Select(x => x.Values.ElementAt(0).ToString()).ToArray();
        }
        /// <summary>
        /// Return whether the given table exists.
        /// </summary> 
        internal static bool TableExists(Cursor cr, string tableName)
        {
            return ExistingTables(cr, tableName).Length == 1;
        }

        /// <summary>
        /// Return the kind of a table: 'r' (regular table), 'v' (view),
        /// 'f' (foreign table), 't' (temporary table), or null.
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="tableName"></param>
        internal static string GetTableKind(Cursor cr, string tableName)
        {
            var query = "SELECT table_type FROM information_schema.tables WHERE table_name=@tableName";
            cr.Execute(query, new { tableName });

            return cr.HasRows ? _TABLE_KIND[cr.FetchOne().Values.ElementAt(0).ToString()] : null;
        }
        /// <summary>
        /// Create the table for a model.
        /// </summary> 
        internal static void CreateModelTable(Cursor cr, string tableName, string comment = "")
        {
            cr.Execute($"CREATE TABLE \"{tableName}\" (id SERIAL NOT NULL, PRIMARY KEY(id))");
            if (!string.IsNullOrEmpty(comment))
            {
                cr.Execute($"COMMENT ON TABLE \"{tableName}\" (id SERIAL NOT NULL, PRIMARY KEY(id))");
            }
        }
        /// <summary>
        /// Return a dict mapping column names to their configuration. The latter is 
        /// a dict with the data from the table ``information_schema.columns``.
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        internal static Dictionary<string, Dictionary<string, object>> GetTableColumns(Cursor cr, string tableName)
        {
            // Do not select the field `character_octet_length` from `information_schema.columns`
            // because specific access right restriction in the context of shared hosting (Heroku, OVH, ...)
            // might prevent a postgres user to read this field.
            var query = @"SELECT column_name, udt_name, character_maximum_length, is_nullable
               FROM information_schema.columns WHERE table_name =@tableName";
            cr.Execute(query, new { tableName });
            return cr.FetchAll().Select(x => new { name = x["column_name"]?.ToString(), row = x }).ToDictionary(k => k.name, v => v.row);
        }

        /// <summary>
        /// Return whether the given column exists.
        /// </summary> 
        internal static bool ColumnExists(Cursor cr, string tableName, string columnName)
        {
            cr.Execute($"SELECT 1 FROM information_schema.columns WHERE table_name=@tableName AND column_name=@columnName", new
            {
                tableName,
                columnName
            });
            return cr.HasRows;
        }

        /// <summary>
        /// Create a column with the given type.
        /// </summary> 
        internal static void CreateColumn(Cursor cr, string tableName, string columnName, string columnType, string comment = "")
        {
            var colDefault = (columnType.ToUpper() == "BOOLEAN") ? "DEFAULT false" : string.Empty;
            cr.Execute($"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnType} {colDefault}");
        }

        /// <summary>
        /// Rename the given column
        /// </summary> 
        internal static void RenameColumn(Cursor cr, string tableName, string columnName1, string columnName2)
        {
            cr.Execute($"ALTER TABLE \"{tableName}\" RENAME COLUMN \"{columnName1}\" TO \"{columnName2}\"");
        }

        /// <summary>
        /// Convert the column to the given type.
        /// </summary> 
        internal static void ConvertColumn(Cursor cr, string tableName, string columnName, string columnType)
        {
            try
            {
                using (var sp = cr.SavePoint())
                {
                    cr.Execute($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{columnName}\" TYPE {columnType}");
                }
            }
            catch (NotSupportedException)
            {
                // can't do inplace change -> use a casted temp column
                var query = @"ALTER TABLE \""{0}\"" RENAME COLUMN \""{1}\"" TO __temp_type_cast;
                ALTER TABLE \""{0}\"" ADD COLUMN \""{1}\"" {2};
                UPDATE \""{0}\"" SET \""{1}\"" = __temp_type_cast::{2};
                ALTER TABLE \""{0}\"" DROP COLUMN  __temp_type_cast CASCADE;";
                cr.Execute(string.Format(query, tableName, columnName, columnType));
            }
        }
        /// <summary>
        /// Add a NOT NULL constraint on the given column.
        /// </summary> 
        internal static void SetNotNull(Cursor cr, string tableName, string columnName)
        {
            try
            {
                using (var sp = cr.SavePoint())
                {
                    cr.Execute($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{columnName}\" SET NOT NULL");
                    //TODO log table added constraint NOT NULL on column name debug level
                }
            }
            catch (Exception)
            {
                //TODO: log table unable to set not null on column name warning level.
                // If you want to have it, you should update the records and execute manually
            }
        }

        /// <summary>
        /// Drop the NOT NULL constraint on the given column.
        /// </summary> 
        internal static void DropNotNull(Cursor cr, string tableName, string columnName)
        {
            cr.Execute($"ALTER TABLE \"{tableName}\" ALTER COLUMN \"{columnName}\" DROP NOT NULL");
            //TODO: log table dropped a constraint not null on column name debug level.
        }

        /// <summary>
        /// Return the given constraint's definition.
        /// </summary> 
        /// <returns></returns>
        internal static string GetConstraintDefinition(Cursor cr, string tableName, string constraintName)
        {
            var query = @"SELECT COALESCE(d.description, pg_get_constraintdef(c.oid))
FROM pg_constraint c
JOIN pg_class t ON t.oid = c.conrelid
LEFT JOIN pg_description d ON c.oid = d.objoid
WHERE t.relname =@tableName AND conname = @constraintName;";
            cr.Execute(query, new
            {
                tableName,
                constraintName
            });
            return cr.HasRows ? cr.FetchOne().FirstOrDefault().Value?.ToString() : null;
        }

        /// <summary>
        /// Add a constraint on the given table.
        /// </summary> 
        internal static void AddConstraints(Cursor cr, string tableName, string constraintName, string definition)
        {
            var query1 = $"ALTER TABLE \"{tableName}\" ADD CONSTRAINT \"{constraintName}\" {definition}";
            var query2 = $"COMMENT ON CONSTRAINT \"{constraintName}\" ON \"{tableName}\" IS @definition";
            try
            {
                using (var sp = cr.SavePoint())
                {
                    cr.Execute(query1);
                    cr.Execute(query1, new
                    {
                        definition
                    });
                    //TODO: log debug message that table added new constraint
                }
            }
            catch (Exception)
            {
                var msg = $@"Table {tableName}: unable to add constraint {constraintName}!\nIf you want to have it, you should update the records and execute manually:\n{query1}";
                Debug.WriteLine(msg, "warning");
            }
        }
        /// <summary>
        /// Create the given foreign key
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="tableName1"></param>
        /// <param name="colName1"></param>
        /// <param name="tableName2"></param>
        /// <param name="colName2"></param>
        /// <param name="ondelete"></param>
        private static bool AddForeignKey(Cursor cr, string tableName1, string colName1, string tableName2, string colName2, CONFDELTYPES ondelete)
        {
            var query = "ALTER TABLE \"@t1\" ADD FOREIGN KEY (\"@cl1\") REFERENCES \"@t2\"(\"@cl2\") ON DELETE @ondelete";
            cr.Execute(query, ("t1", tableName1), ("t2", tableName2), ("cl1", colName1), ("cl2", colName2), ("ondelete", ondelete.ToString().Replace("_", " ")));
            return true;
        }
        /// <summary>
        /// Update the foreign keys between tables to match the given one
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="tableName1"></param>
        /// <param name="colName1"></param>
        /// <param name="tableName2"></param>
        /// <param name="colName2"></param>
        /// <param name="ondelete"></param>
        /// <returns>true if the given foreign key has been recreated</returns>
        internal static bool FixForeignKey(Cursor cr, string tableName1, string colName1, string tableName2, string colName2, CONFDELTYPES ondelete = CONFDELTYPES.NO_ACTION)
        {
            var delType = _CONFDELTYPES[ondelete];
            var query = @" SELECT con.conname, c2.relname, a2.attname, con.confdeltype as deltype
                  FROM pg_constraint as con, pg_class as c1, pg_class as c2,
                       pg_attribute as a1, pg_attribute as a2
                 WHERE con.contype = 'f' AND con.conrelid = c1.oid AND con.confrelid = c2.oid
                   AND array_lower(con.conkey, 1)= 1 AND con.conkey[1] = a1.attnum
                   AND array_lower(con.confkey, 1)= 1 AND con.confkey[1] = a2.attnum
                   AND a1.attrelid = c1.oid AND a2.attrelid = c2.oid
                   AND c1.relname =@tableName1 AND a1.attname =@colName1 ";
            cr.Execute(query, ("tableName1", tableName1), ("colName1", colName1));
            var found = false;
            foreach (var fk in cr.FetchAll())
            {
                var values = fk.Values.Cast<string>().ToArray();
                Debug.Assert(values.Length == 4);
                if (!found && (values[0], values[1], values[2]) == (tableName2, colName2, delType))
                {
                    found = true;
                }
                else
                {
                    DropConstraints(cr, tableName1, values[0]);
                }
            }
            if (!found)
            {
                AddForeignKey(cr, tableName1, colName1, tableName2, colName2, ondelete);
            }
            return true;
        }


        /// <summary>
        /// Drop the given constraint.
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="tableName1"></param>
        /// <param name="v"></param>
        private static void DropConstraints(Cursor cr, string tablename, string constraintname)
        {
            try
            {
                using (var sp = cr.SavePoint())
                {
                    cr.Execute($"ALTER TABLE \"{tablename}\" DROP CONSTRAINT \"{constraintname}\"");
                }
            }
            catch (Exception)
            {
                //TODO: Log the warning
                Debug.WriteLine($"Table {tablename}: unable to drop constraint {constraintname}!", "warning");
            }
        }


        /// <summary>
        /// Create the given index unless it exists.
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="indexName"></param>
        /// <param name="tableName"></param>
        /// <param name="expressions"></param>
        internal static void CreateUniqueIndex(Cursor cr, string indexName, string tableName, string[] expressions)
        {
            if (IndexExists(cr, indexName))
            {
                return;
            }

            var args = string.Join(", ", expressions);
            cr.Execute($"CREATE UNIQUE INDEX \"{indexName}\" ON \"{tableName}\" ({args})");
        }


        /// <summary>
        /// Returns the VARCHAR declaration for the provided size:
        /// * If no size (or an empty or negative size is provided) return an 'infinite' VARCHAR
        ///  Otherwise return a VARCHAR(n)
        /// </summary>
        /// <param name="size">varchar size, optional</param>
        internal static string PgVarchar(int size = 0)
        {
            if (size > 0)
            {
                return $"VARCHAR({size})";
            }
            return "VARCHAR";
        }


        /// <summary>
        /// Reverse an ORDER BY clause
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        internal static string ReverseOrder(string order)
        {
            var items = new List<string>();
            foreach (var item in order.Split(','))
            {
                var itemParts = item.ToLower().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var direction = itemParts.Slice(from: 1).FirstOrDefault() == "desc" ? "asc" : "desc";
                items.Add($"{itemParts[0]} {direction}");
            }
            return string.Join(", ", items.ToArray());
        }

        /// <summary>
        /// Return whether the given index exists.
        /// </summary>  
        internal static bool IndexExists(Cursor cr, string indexName)
        {
            return cr.ExecuteScalar<int>($"SELECT 1 FROM pg_indexes WHERE indexname={indexName}") > 0;
        }

        /// <summary>
        /// Create the given index unless it exists.
        /// </summary>
        internal static void CreateIndex(Cursor cr, string indexName, string tableName, params string[] expressions)
        {
            if (IndexExists(cr, indexName))
            {
                return;
            }
            var args = string.Join(", ", expressions);
            cr.Execute($"CREATE INDEX \"{indexName}\" ON \"{tableName}\" ({args})");
        }

        internal static void DropIndex(Cursor cr, string index, string tableName)
        {
            cr.Execute($"DROP INDEX IF EXISTS \"{index}\"");
        }

        internal static void drop_view_if_exists(Cursor cr, string viewname)
        {
            cr.Execute($"DROP view IF EXISTS {viewname} CASCADE");
        }

        internal static string EscapePSql(string toEscape)
        {
            // to_escape.replace('\\', r'\\').replace('%', '\%').replace('_', '\_')
            return toEscape.Replace("\\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        }

        /// <summary>
        /// Returns the VARCHAR declaration for the provided size\
        /// </summary>
        /// <param name="size">varchar size</param>
        /// <returns>
        /// * If no size (or an empty or negative size is provided) return an 'infinite' VARCHAR
        /// * Otherwise return a VARCHAR(n)
        /// </returns>
        internal static string PgVarChar(int size)
        {
            return "";
        }
    }
}
