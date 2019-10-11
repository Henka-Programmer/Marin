using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MarinFramework.Sql
{
    /// <summary>
    /// Dumb implementation of a Query object
    /// </summary>
    public class Query
    {
        /// <summary>
        /// holds the list of tables joined using default JOIN.
        /// the table names are stored double-quoted
        /// </summary>        
        private List<string> tables = new List<string>();
        private TableJoins joins = new TableJoins();
        private List<string> whereClause = new List<string>();
        private List<object> whereClauseParams = new List<object>();
        private JoinCondition extras = new JoinCondition();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tables">the list of tables (without double-quotes) joined using default JOIN</param>
        /// <param name="whereClause">the list of WHERE clause elements, to be joined with 'AND' when generating the final query</param>
        /// <param name="whereClauseParams"></param>
        /// <param name="joins"></param>
        /// <param name="extras"></param>
        public Query(string[] tables = null, string[] whereClause = null, object[] whereClauseParams = null, TableJoins joins = null, JoinCondition extras = null)
        {
            this.tables = tables != null ? new List<string>(tables) : new List<string>();
            this.whereClause = whereClause != null ? new List<string>(whereClause) : new List<string>();
            this.whereClauseParams = whereClauseParams != null ? new List<object>(whereClauseParams) : new List<object>();

            // holds table joins done explicitly, supporting outer joins. The JOIN
            // condition should not be in `where_clause`. The dict is used as follows:
            // self.joins = {
            //                    'table_a': [
            //                                  ('table_b', 'table_a_col1', 'table_b_col', 'LEFT JOIN'),
            //                                  ('table_c', 'table_a_col2', 'table_c_col', 'LEFT JOIN'),
            //                                  ('table_d', 'table_a_col3', 'table_d_col', 'JOIN'),
            //                               ]
            //                 }
            // which should lead to the following SQL:
            // SELECT ... FROM "table_a" LEFT JOIN "table_b" ON ("table_a"."table_a_col1" = "table_b"."table_b_col")
            // LEFT JOIN "table_c" ON ("table_a"."table_a_col2" = "table_c"."table_c_col")
            this.joins = joins ?? new TableJoins();

            // holds extra conditions for table joins that should not be in the where
            // clause but in the join condition itself. The dict is used as follows:
            //
            // self.extras = {
            //       ('table_a', ('table_b', 'table_a_col1', 'table_b_col', 'LEFT JOIN')):
            //           ('"table_b"."table_b_col3" = %s', [42])
            //   }
            //
            // which should lead to the following SQL:
            //
            // SELECT ... FROM "table_a"
            // LEFT JOIN "table_b" ON ("table_a"."table_a_col1" = "table_b"."table_b_col" AND "table_b"."table_b_col3" = 42)
            //   ...
            this.extras = extras ?? new JoinCondition();

        }

        protected IEnumerable<string> GetTableAliases()
        {
            foreach (var table in tables)
            {
                yield return table.GetAliasFromQuery().alias;
            }
        }

        protected Dictionary<string, string> GetAliasMapping()
        {
            var mapping = new System.Collections.Generic.Dictionary<string, string>();
            for (int i = 0; i < tables.Count; i++)
            {
                string t = tables[i];
                var r = t.GetAliasFromQuery();
                mapping[r.alias] = t;
            }
            return mapping;
        }

        /// <summary>
        /// Join a destination table to the current table.
        /// </summary>
        /// <param name="connection">a tuple ``(lhs, table, lhs_col, col, link)``.
        /// The join corresponds to the SQL equivalent of:
        /// (lhs.lhs_col = table.col)
        /// Note that all connection elements are strings.
        /// </param>
        /// <param name="implicit">False if the join is an explicit join.</param>
        /// <param name="outer"> True if a LEFT OUTER JOIN should be used, if possible
        /// (no promotion to OUTER JOIN is supported in case the JOIN
        /// was already present in the query, as for the moment
        /// implicit INNER JOINs are only connected from NON-NULL
        /// columns so it would not be correct (e.g. for
        /// inherits or when a domain criterion explicitly // TODO: inherits not supported yet in orm
        /// adds filtering)</param>
        /// <param name="extra">A string with the extra join condition (SQL), or None.
        /// This is used to provide an additional condition to the join
        /// clause that cannot be added in the where clause(e.g., for LEFT
        /// JOIN concerns). The condition string should refer to the table
        /// aliases as "{lhs}" and "{rhs}</param>
        /// <param name="extraParams"> a list of parameters for the `extra` condition.</param>
        internal (string alias, string alias_statement) AddJoin((string lhs, string table, string lhs_col, string col, string link) connection, bool @implicit = true, bool outer = false, string extra = null, object[] extraParams = null)
        {
            (string alias, string alias_statement) = connection.lhs.GenerateTableAlias((connection.table, connection.link));
            if (@implicit)
            {
                if (!tables.Contains(alias_statement))
                {
                    tables.Add(alias_statement);
                    var condition = $"(\"{connection.lhs}\".\"{connection.lhs_col}\" = \"{alias}\".\"{connection.col}\")";
                    whereClause.Add(condition);

                }
                //else
                //{
                //    // already joined 
                //}
                return (alias, alias_statement);
            }
            else
            {
                var aliases = GetTableAliases();
                Debug.Assert(aliases.Any(x => x == connection.lhs), $"Left-hand-side table {connection.lhs} must already be part of the query tables [{string.Join(", ", tables.ToArray())}]!");
                if (aliases.Any(x => x == connection.lhs))
                {
                    // already joined, must ignore (promotion to outer and multiple joins not supported yet)
                }
                else
                {
                    // add JOIN
                    tables.Add(alias_statement);
                    var join_tupe = (alias, connection.lhs_col, connection.col, outer ? "LEFT JOIN" : "JOIN");
                    joins[connection.lhs].Add(join_tupe);

                    if (!string.IsNullOrEmpty(extra) || (extraParams != null && extraParams.Length > 0))
                    {
                        extra = (extra ?? string.Empty).Format(("lhs", connection.lhs), ("rhs", alias));
                        extras[(connection.lhs, join_tupe)] = (extra, extraParams);
                    }
                }

                return (alias, alias_statement);
            }
        }

        /// <summary>
        /// Returns (query_from, query_where, query_params)
        /// </summary>
        /// <returns></returns>
        public (string query_from, string query_where, object[] query_params) GetSql()
        {
            var tablesToProcess = tables.ToList();
            var aliasMapping = GetAliasMapping();
            var fromClause = new List<string>();
            var fromParams = new List<object>();

            void AddJoinsForTable(string lhs)
            {
                foreach (var (rhs, lhs_col, rhs_col, join) in joins.Get(lhs, new List<(string table_b, string table_col, string table_b_col, string join)>()))
                {
                    tablesToProcess.Remove(aliasMapping[rhs]);
                    var fclause = $" {0} {1} ON (\"{2}\".\"{3}\" = \"{4}\".\"{5}\")";
                    fclause = string.Format(fclause, join, aliasMapping[rhs], lhs, lhs_col, rhs, rhs_col);
                    fromClause.Add(fclause);
                    if (extras.TryGetValue((lhs, (rhs, lhs_col, rhs_col, join)), out (string condition, object[] @params) extra))
                    {
                        if (!string.IsNullOrEmpty(extra.condition))
                        {
                            fromClause.Add(" AND ");
                            fromClause.Add(extra.condition);
                        }
                        if (extra.@params != null && extra.@params.Length > 0)
                        {
                            fromParams.AddRange(extra.@params.ToList());
                        }
                    }

                    fromClause.Add(")");
                    AddJoinsForTable(rhs);
                }
            }

            for (int i = 0; i < tablesToProcess.Count; i++)
            {
                if (i > 0)
                {
                    fromClause.Add(",");
                }

                string table = tables[i];
                fromClause.Add(table);
                var table_alias = table.GetAliasFromQuery().alias;
                if (joins.ContainsKey(table_alias))
                {
                    AddJoinsForTable(table_alias);
                }

            }

            return (string.Join("", fromClause.ToArray()), string.Join(" AND ", whereClause.ToArray()), fromParams.Concat(whereClauseParams).ToArray());
        }

        public static implicit operator (string query_from, string query_where, object[] query_params)(Query query)
        {
            return query.GetSql();
        }

        public override string ToString()
        {
            (string query_from, string query_where, object[] query_params) = GetSql();
            return $"[GORM.Query: \"SELECT ... FROM {query_from} WHERE {query_where}\" with params: ({string.Join(", ", query_params)})";
        }
    }
}
