using System;
using System.Collections.Generic;
using System.Text;

namespace MarinFramework.Sql
{
    internal static class SqlHeper
    {
        internal static string ReverseOrder(string order)
        {
            return order;
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
            return toEscape;
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
