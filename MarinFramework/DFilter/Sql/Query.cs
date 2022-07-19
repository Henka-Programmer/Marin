using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace DFilter
{
    public class Query
    {
        /// <summary>
        /// Holds the list of tables joined using default JOIN.
        /// the table names are stored double-quoted
        /// </summary>        
        private readonly StringDictionary _tables = new StringDictionary();
        private readonly Dictionary<string, (string kind, string table, string condition, QueryParameter[] parameters)> _joins = new Dictionary<string, (string kind, string rhsTable, string conditiona, QueryParameter[] p)>();
        private readonly List<string> _whereClauses = new List<string>();
        private readonly List<QueryParameter> _whereClauseParams = new List<QueryParameter>();

        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string Order { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tables">the list of tables (without double-quotes) joined using default JOIN</param>
        /// <param name="whereClause">the list of WHERE clause elements, to be joined with 'AND' when generating the final query</param>
        /// <param name="whereClauseParams"></param>
        /// <param name="joins"></param>
        /// <param name="extras"></param>
        public Query(string alias, string table = null)
        {
            _tables.Add(alias, table ?? alias);
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
        public string Join(string lhsAlias, string lhsColumn, string rhsTable, string rhsColumn, string link, string extra = null, QueryParameter[] extraParams = null)
        {
            return Join("JOIN", lhsAlias, lhsColumn, rhsTable, rhsColumn, link, extra, extraParams);
        }

        private string Join(string kind, string lhsAlias, string lhsColumn, string rhsTable, string rhsColumn, string link, string extra, QueryParameter[] extraParams)
        {
            Debug.Assert(_tables.ContainsKey(lhsAlias) || _joins.ContainsKey(lhsAlias), "Alias not in lhsAlias");

            var rhsAlias = GenerateTableAlias(lhsAlias, link);
            Debug.Assert(!_tables.ContainsKey(rhsAlias) || _joins.ContainsKey(rhsAlias), "Alias not in rhsAlias");

            if(!_joins.ContainsKey(rhsAlias))
            {
                var condition = $"[{lhsAlias}].[{lhsColumn}] = [{rhsAlias}].[{rhsColumn}]";
                var conditionParams = new List<QueryParameter>();

                if (extra != null)
                {
                    condition = $"{condition} AND {string.Format(extra, lhsAlias, rhsAlias)}";
                    conditionParams.AddRange(extraParams);
                }

                if (!string.IsNullOrEmpty(kind))
                {
                    _joins[rhsAlias] = (kind, rhsTable, condition, conditionParams.ToArray());
                }
                else
                {
                    _tables.Add(rhsAlias, rhsTable);
                    this.AddWhere(condition, conditionParams.ToArray());
                }
            }

            return rhsAlias;
        }
        public string LeftJoin(string lhsAlias,string lhsColumn,string rhsTable,string rhsColumn, string link, string extra = null, QueryParameter[] extraParams = null)
        {
            return Join("LEFT JOIN", lhsAlias, lhsColumn, rhsTable, rhsColumn, link, extra, extraParams);
        }
        private string GenerateTableAlias(string srcTableAlias, string link)
        {
            //TODO: check server lenght limts
            return $"{srcTableAlias}_{link}";
        }

        public (string queryStr, QueryParameter[] parameters) Select(params object[] args)
        {
            var qSql = GetSql();
            var selectColumns = args != null && args.Length > 0 ? string.Join(", ", args.Select(x => $"[{x}]")) : "*";
            var selectWhere = string.IsNullOrEmpty(qSql.query_where) ? "TRUE" : qSql.query_where;
            var orderBy = string.IsNullOrEmpty(Order) ? "" : $" ORDER BY {Order}";
            var selectOffset = Limit == null ? "" : $" OFFSET {Offset} ROWS";
            var selectLimit = Limit == null ? "" : $" FETCH NEXT {Limit} ROWS ONLY";
            var queryStr = $"SELECT {selectColumns} FROM {qSql.query_from} WHERE {selectWhere}{selectOffset}{selectLimit}{orderBy}{selectOffset}{selectLimit}";
       
            return (queryStr, qSql.query_params);
        }

        public (string queryStr, object[] parameters) Subselect(params object[] args)
        {
            if (Limit != null || !string.IsNullOrEmpty(Order))
            {
                return Select(args);
            }

            var qSql = GetSql();
            var selectColumns = args != null && args.Length > 0 ? string.Join(", ", args) : "*";
            var selectWhere = string.IsNullOrEmpty(qSql.query_where) ? "TRUE" : qSql.query_where;

            return ($"SELECT {selectColumns} FROM {qSql.query_from} WHERE {selectWhere}", qSql.query_params);
        }

        /// <summary>
        /// Returns (query_from, query_where, query_params)
        /// </summary>
        /// <returns></returns>
        public (string query_from, string query_where, QueryParameter[] query_params) GetSql()
        {
            var tables = _tables.Keys.Cast<string>().Select(alias => FromTable(_tables[alias], alias)).ToArray();
            var joins = new List<string>();
            var parameters = new List<QueryParameter>();

            foreach (var entry in _joins)
            {
                joins.Add($"{entry.Value.kind} {FromTable(entry.Value.table, entry.Key)} ON ({entry.Value.condition})");
                parameters.AddRange(entry.Value.parameters);
            }

            var fromClause = string.Join(", ", tables) + string.Join(" ", joins);
            var whereClause = string.Join(" AND ", _whereClauses);

            parameters.AddRange(_whereClauseParams);
            return (fromClause, whereClause, parameters.ToArray());
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

        public void AddWhere(string query, QueryParameter[] @params)
        {
            _whereClauses.Add(query);
            _whereClauseParams.AddRange(@params);
        }

        public (string query, QueryParameter[] @params) SubSelect()
        {
            return (null, null);
        }

        public void AddTable(string alias, string table = null)
        {
            Debug.Assert(!_tables.ContainsKey(alias) && !_joins.ContainsKey(alias), $"Alias {alias} already in Query");
            _tables.Add(alias, table ?? alias);
        }

        private string FromTable(string table, string alias)
        {
            if (alias == table)
            {
                return $"[{alias}]";
            }

            return $"[{table}] AS [{alias}]";
        }
    }
}
