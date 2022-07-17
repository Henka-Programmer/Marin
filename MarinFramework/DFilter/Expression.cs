using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace DFilter
{

    public class Query
    {
        /// <summary>
        /// holds the list of tables joined using default JOIN.
        /// the table names are stored double-quoted
        /// </summary>        
        private readonly StringDictionary _tables = new StringDictionary();
        private readonly Dictionary<string, (string kind, string table, string condition, object[] parameters)> _joins = new Dictionary<string, (string kind, string rhsTable, string conditiona, object[] p)>();
        private readonly List<string> _whereClauses = new List<string>();
        private readonly List<object> _whereClauseParams = new List<object>();

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
        public string Join(string lhsAlias, string lhsColumn, string rhsTable, string rhsColumn, string link, string extra = null, object[] extraParams = null)
        {
            return Join("JOIN", lhsAlias, lhsColumn, rhsTable, rhsColumn, link, extra, extraParams);
        }

        private string Join(string kind, string lhsAlias, string lhsColumn, string rhsTable, string rhsColumn, string link, string extra, object[] extraParams)
        {
            Debug.Assert(_tables.ContainsKey(lhsAlias) || _joins.ContainsKey(lhsAlias), "Alias not in lhsAlias");

            var rhsAlias = GenerateTableAlias(lhsAlias, link);
            Debug.Assert(!_tables.ContainsKey(rhsAlias) || _joins.ContainsKey(rhsAlias), "Alias not in rhsAlias");

            if(!_joins.ContainsKey(rhsAlias))
            {
                var condition = $"{lhsAlias}.{lhsColumn} = {rhsAlias}.{rhsColumn}";
                var conditionParams = new List<object>();

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
        public string LeftJoin(string lhsAlias,string lhsColumn,string rhsTable,string rhsColumn, string link, string extra = null, object[] extraParams = null)
        {
            return Join("LEFT JOIN", lhsAlias, lhsColumn, rhsTable, rhsColumn, link, extra, extraParams);
        }
        private string GenerateTableAlias(string srcTableAlias, string link)
        {
            //TODO: check server lenght limts
            return $"{srcTableAlias}_{link}";
        }

        public (string queryStr, object[] parameters) Select(params object[] args)
        {
            var qSql = GetSql();
            var selectColumns = args != null && args.Length > 0 ? string.Join(", ", args) : "*";
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
        public (string query_from, string query_where, object[] query_params) GetSql()
        {
            var tables = _tables.Keys.Cast<string>().Select(alias => FromTable(_tables[alias], alias)).ToArray();
            var joins = new List<string>();
            var parameters = new List<object>();

            foreach (var entry in _joins)
            {
                joins.Add($"{entry.Value.kind} {FromTable(entry.Value.table, entry.Key)} ON ({entry.Value.condition})");
                parameters.AddRange(entry.Value.parameters);
            }

            var fromClause = string.Join(", ", tables) + " " + string.Join(" ", joins);
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

        public void AddWhere(string query, object[] @params)
        {
            _whereClauses.Add(query);
            _whereClauseParams.AddRange(@params);
        }

        public (string query, object[] @params) SubSelect()
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
                return $"\"{alias}\"";
            }

            return $"{table} AS {alias}";
        }
    }

    public class Term : Tuple<object, string, object>
    {
        public Term(object item1, string item2, object item3) : base(item1, item2, item3)
        {
        }

        public static implicit operator Term((int, string, int) v)
        {
            return new Term(v.Item1, v.Item2, v.Item3);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is Term other && this.Item1 == other.Item1 && this.Item2 == other.Item2 && this.Item3 == other.Item3);
        }
    }

    public class SearchDomain : List<object>
    {
        public SearchDomain(params object[] components) : base(components)
        {

        }

        public SearchDomain(params string[] operators) : base(operators)
        {

        }
        public SearchDomain()
        {

        }

        public static implicit operator SearchDomain(Term term)
        {
            return new SearchDomain(term);
        }

        public static SearchDomain operator +(SearchDomain left, SearchDomain right)
        {
            var result = new SearchDomain();

            result.AddRange(left.ToArray());
            result.AddRange(right.ToArray());
            return result;
        }

        public static SearchDomain operator +(SearchDomain left, List<string> right)
        {
            var result = new SearchDomain();
            result.AddRange(left.ToArray());
            result.AddRange(right.ToArray());
            return result;
        }

        public static SearchDomain operator +(SearchDomain left, Term term)
        {
            var result = new SearchDomain();
            result.AddRange(left.ToArray());
            result.Add(term);
            return result;
        }

        public SearchDomain Normalize()
        {
            if (this.Count == 0)
            {
                return DomainOperators.TRUE_LEAF;
            }

            var operatorsArity = new Dictionary<string, int>
            {
                { DomainOperators.NOT, 1 },
                { DomainOperators.AND, 2 },
                { DomainOperators.OR, 2 },
            };
            var expected = 1;
            var result = new SearchDomain();
            foreach (var token in this)
            {
                if (expected == 0)
                {
                    result.Insert(0, DomainOperators.AND);
                    expected = 1;
                }

                if (token is Term term || token is System.Runtime.CompilerServices.ITuple ituple)
                {
                    expected -= 1;
                }
                else
                {
                    expected += operatorsArity.Get(token.ToString(), 0) - 1;
                }

                result.Add(token);
            }
            Debug.Assert(expected == 0, "This domain is syntactically not correct: %s");
            return result;
        }

        public static int IsFalse(SearchDomain domain)
        {
            var stack = new Stack<int>();
            var reversedAndNormalizedDomain = domain.Normalize().Reverse<object>();

            foreach (var token in reversedAndNormalizedDomain)
            {
                if (token is string op)
                {
                    switch (op)
                    {
                        case DomainOperators.AND: { stack.Push(Math.Min(stack.Pop(), stack.Pop())); break; }
                        case DomainOperators.OR: { stack.Push(Math.Max(stack.Pop(), stack.Pop())); break; }
                        case DomainOperators.NOT: { stack.Push(-stack.Pop()); break; }
                        default:
                            // throw new InvalidOperationException($"{op} Operator not supported");
                            stack.Push(0);
                            break;
                    }
                }
                else if (token is Term term)
                {
                    if (term.Equals(DomainOperators.TRUE_LEAF))
                    {
                        stack.Push(+1);
                    }
                    else if (term.Equals(DomainOperators.FALSE_DOMAIN))
                    {
                        stack.Push(+1);
                    }
                    else
                    {
                        if (!(term.Item3 is Query) || ((term.Item3 != null) && Convert.ToBoolean(term.Item3)))
                        {
                            if (term.Item2 == "in")
                            {
                                stack.Push(-1);
                            }
                            else if (term.Item2 == "not in")
                            {
                                stack.Push(+1);
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }
                        else
                        {
                            stack.Push(0);
                        }
                    }
                }
                else
                {
                    stack.Push(0);
                }
            }

            return stack.Count > 0 ? stack.Pop() : -1;
        }

        public static SearchDomain Combine(string op, Term unit, Term zero, params SearchDomain[] domains)
        {
            var result = new SearchDomain();
            var unitDomain = new SearchDomain(unit);
            if (domains.SequenceEqual(unitDomain))
            {
                return unit;
            }

            int count = 0;
            var zeroDomain = new SearchDomain(zero);
            foreach (var domain in domains)
            {
                if (domain == null)
                {
                    continue;
                }

                if (domain.Equals(unitDomain))
                {
                    continue;
                }

                if (domain.Equals(zeroDomain))
                {
                    return zeroDomain;
                }

                result.AddRange(domain.Normalize().ToArray());
                count++;
            }

            var length = count - 1;
            result = length >= 1 
                ? (new SearchDomain(Enumerable.Repeat(op, length).ToArray()) + result)
                : result;

            return result.Count == 0 ? unitDomain : result;
        }

        public static SearchDomain AND(params SearchDomain[] domains)
        {
            return Combine(DomainOperators.AND, DomainOperators.TRUE_LEAF, DomainOperators.FALSE_LEAF, domains);
        }
        public static SearchDomain OR(params SearchDomain[] domains)
        {
            return Combine(DomainOperators.OR, DomainOperators.FALSE_LEAF, DomainOperators.TRUE_LEAF, domains);
        }

        public static bool IsLeaf(object element, out Term term)
        {
            term = element as Term;
            return term != null;
        }

        public static bool IsOperator(object element, out string @operator)
        {
            @operator = element as string;
            return @operator != null && DomainOperators.DOMAIN_OPERATORS.Contains(@operator);
        }

        public static bool IsBoolean(object element)
        {
            return element is Term leaf && (DomainOperators.TRUE_LEAF.Equals(leaf) || DomainOperators.FALSE_LEAF.Equals(leaf));
        }

        public static void CheckLeaf(object element)
        {
            if (!IsOperator(element, out _) && !IsLeaf(element, out Term term))
            {
                throw new InvalidOperationException($"Value error, invalid leaf: {element}");
            }
        }

        public static SearchDomain DistributeNot(SearchDomain domain)
        {
            var result = new SearchDomain();
            var stack = new Stack<bool>();
            stack.Push(false);

            foreach (var token in domain)
            {
                var negate = stack.Pop();

                if (IsLeaf(token, out Term term))
                {
                    if (negate)
                    {
                        if (DomainOperators.TERM_OPERATORS_NEGATION.TryGetValue(term.Item2, out string opNegation))
                        {
                            if (DomainOperators.TRUE_LEAF.Equals(term) || DomainOperators.FALSE_LEAF.Equals(term))
                            {
                                result.Add(DomainOperators.TRUE_LEAF.Equals(term) ? DomainOperators.FALSE_DOMAIN : DomainOperators.TRUE_LEAF);
                            }
                            else
                            {
                                result.Add((term.Item1, DomainOperators.TERM_OPERATORS_NEGATION[opNegation], term.Item3));
                            }
                        }
                        else
                        {
                            result.Add(DomainOperators.NOT);
                            result.Add(term);
                        }
                    }
                    else
                    {
                        result.Add(token);
                    }
                }
                else if (token is string op)
                {
                    if (op == DomainOperators.NOT)
                    {
                        stack.Push(!negate);
                    }
                    else if (DomainOperators.DOMAIN_OPERATORS_NEGATION.ContainsKey(op))
                    {
                        result.Add(negate ? DomainOperators.DOMAIN_OPERATORS_NEGATION[op] : op);
                        stack.Push(negate);
                        stack.Push(negate);
                    }

                }
                else
                {
                    result.Add(token);
                }
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is SearchDomain other && this.SequenceEqual(other));
        }
    }

    public static class DomainOperators
    {
        public const string NOT = "!";
        public const string OR = "|";
        public const string AND = "&";

        public static readonly string[] DOMAIN_OPERATORS = { NOT, OR, AND };
        public static readonly string[] TERM_OPERATORS = { "=", "!=", "<=", "<", ">", ">=", "=?", "=like", "=ilike",
                  "like", "not like", "ilike", "not ilike", "in", "not in" };

        public static readonly string[] NEGATIVE_TERM_OPERATORS = { "!=", "not like", "not ilike", "not in" };

        public static readonly Dictionary<string, string> DOMAIN_OPERATORS_NEGATION = new Dictionary<string, string>
        {
            {AND, OR },
            {OR, AND },
        };

        public static readonly Dictionary<string, string> TERM_OPERATORS_NEGATION = new Dictionary<string, string>
        {
            {"<", ">="},
            {">", "<="},
            {"<=", ">"},
            {">=", "<"},
            {"=", "!="},
            {"!=", "="},
            {"in", "not in"},
            {"like", "not like"},
            {"ilike", "not ilike"},
            {"not in", "in"},
            {"not like", "like"},
            {"not ilike", "ilike" }
        };

        public static readonly Term TRUE_LEAF = (1, "=", 1);
        public static readonly Term FALSE_LEAF = (0, "=", 1);

        public static readonly SearchDomain TRUE_DOMAIN = TRUE_LEAF;
        public static readonly SearchDomain FALSE_DOMAIN = FALSE_LEAF;
    }

    public class Column
    {
        public string Name { get; set; }
        public Type Type { get; set; }
    }

    public class Model
    {
        public string Table { get; set; }
        public string TableQuery { get; set; }
        internal Dictionary<string, Column> _Columns;
    }

    public class Expression
    {
        private class TermStack : Stack<(object leaf, Model model, string alias)>
        {
            public TermStack()
            {

            }

            public new void Push((object leaf, Model model, string alias) leaf)
            {
                SearchDomain.CheckLeaf(leaf.leaf);
                base.Push(leaf);
            }
        }

        private readonly SearchDomain _expression;
        private readonly string _rootAlias;
        private readonly Model _rootModel;
        public Query Query { get; private set; }

        public Expression(SearchDomain domain, Model model, string alias = null, Query query = null)
        {
            Query = query ?? new Query(model.Table, model.TableQuery);
            _expression = SearchDomain.DistributeNot(domain.Normalize());
            _rootAlias = alias;
            _rootModel = model;

            // parse the domain expression
            Parse();
        }

        private static string FormatQuery(string @operator, string lhs, string rhs)
        {
            if (@operator == DomainOperators.AND)
            {
                return $"( {lhs} AND {rhs})";
            }
            else if (@operator == DomainOperators.OR)
            {
                return $"( {lhs} OR {rhs})";
            }

            throw new ArgumentException($"{@operator} not supported");
        }

        public void Parse()
        {
            var stack = new TermStack();

            foreach (var leaf in _expression)
            {
                stack.Push((leaf, _rootModel, _rootAlias));
            }

            // stack of SQL expressions in the form: (expr, params)
            var resultStack = new Stack<(string query, object[] @params)>();
            while (stack.Count > 0)
            {
                var (leaf, model, alias) = stack.Pop();

                // ----------------------------------------
                // SIMPLE CASE
                // 1. leaf is an operator
                // 2. leaf is a true/false leaf
                // -> convert and add directly to result
                // ----------------------------------------

                if (SearchDomain.IsOperator(leaf, out string op))
                {
                    if (op == DomainOperators.NOT)
                    {
                        var (expr, @params) = resultStack.Pop();
                        resultStack.Push(($"(NOT ({expr}))", @params));
                    }
                    else
                    {
                        var (lhs, lhs_params) = resultStack.Pop();
                        var (rhs, rhs_params) = resultStack.Pop();

                        var allParams = new List<object>();
                        allParams.AddRange(lhs_params);
                        allParams.AddRange(rhs_params);

                        resultStack.Push((FormatQuery(op, lhs, rhs), allParams.ToArray()));
                    }

                    continue;
                }

                if (SearchDomain.IsBoolean(leaf))
                {
                    resultStack.Push(LeafToSql(leaf, model, alias));
                }

                var term = leaf as Term;
                // Get working variables
                var (left, @operator, right) = (term.Item1 as string, term.Item2, term.Item3);

                var path = left.Split('.');
                var column = model._Columns.Get(path[0]);

                if (column == null)
                {
                    throw new InvalidOperationException($"column not found in {model}");
                }

                if (path.Length > 1)
                {
                    throw new NotSupportedException("Navigation properties not supported yet.");
                }

                if (column.Type == typeof(DateTime) && right != null)
                {
                    if (right is string str && str.Length == 10)
                    {
                        if (@operator == ">" || @operator == "<=")
                        {
                            right = right.ToString() + " 23:59:59";
                        }
                        else
                        {
                            right = right.ToString() + " 00:00:00";
                        }

                        stack.Push(((left, @operator, right), model, alias));
                    } else if (right is DateTime dt && dt.TimeOfDay == TimeSpan.Zero)
                    {
                        if (@operator == ">" || @operator == "<=")
                        {
                            right = dt.Add(new TimeSpan(0, 23, 59, 59, 99999));
                        }

                        stack.Push(((left, @operator, right), model, alias));
                    }
                    else
                    {
                        var leafToSql = this.LeafToSql(leaf, model, alias);
                        resultStack.Push(leafToSql);
                    }
                }
                else if (right != null)
                {
                    var leafToSql = this.LeafToSql(leaf, model, alias);
                    resultStack.Push(leafToSql);
                }
            }

            if (resultStack.Count > 0)
            {
                this.result = resultStack.Pop();
            }

            Query.AddWhere(result.query, result.@params);
        }

        private (string query, object[] @params) result;

        private (string, object[]) LeafToSql(object leaf, Model model, string alias)
        {
            var term = leaf as Term;
            var checkNull = false;
            var (left, @operator, right) = term;

            var tableAlias = $"'{alias}'";

            var query = "";
            object[] @params;

            if (term == DomainOperators.TRUE_LEAF)
            {
                query = "TRUE";
                @params = new object[] {};
            }else if (term == DomainOperators.FALSE_LEAF)
            {
                query = "FLASE";
                @params = new object[] {};
            }else if (@operator == "in" || @operator == "not in")
            {
                if (right is Query rq)
                {
                    var (subQuery, subParams) = rq.SubSelect();
                    query = $"({tableAlias}.\"{left}\" ({subQuery}))";
                    @params = subParams;
                }else if (right is IList list)
                {
                    var column = model._Columns.Get(left.ToString());
                    if (column.Type == typeof(bool))
                    {
                        //TODO: not sure to use this case
                        throw new InvalidOperationException("Not sure to use this case");
                    }
                    else
                    {
                        @params = list.Cast<object>().Where(x => x != null).ToArray();
                        checkNull = @params.Length < list.Count;
                    }

                    if (@params.Length > 0)
                    {
                        var inStr = string.Join(", ", @params);
                        query = $"({tableAlias}.\"{left}\" {@operator} {inStr})";
                    }
                    else
                    {
                        // The case for (left, 'in', []) or (left, 'not in', []).
                        query = @operator == "in" ? "FLASE" : "TRUE";
                    }

                    if ((@operator == "in" && checkNull) || (@operator == "not in" && !checkNull))
                    {
                        query = $"{query} OR {tableAlias}.{left} IS NULL";
                    } else if (@operator == "not in" && checkNull)
                    {
                        query = $"{query} AND {tableAlias}.{left} IS NOT NULL";
                    }
                }
                else
                {
                    // Must not happened
                    throw new InvalidOperationException("Invalid domain term");
                }
            }else if (model._Columns.TryGetValue(left.ToString(), out Column col) && col.Type == typeof(bool) && ((@operator == "=" && right is bool b && !b) || (@operator == "!=" && right is bool br && br)))
            {
                query = $"({tableAlias}.\" {left}\" IS NULL or {tableAlias}.\"{left}\" = FALSE )";
                @params = new object[] { };
            }else if ((right == null || right is bool rightBool && !rightBool) && @operator == "=")
            {
                query = $"{tableAlias}.\"{left}\" ";
                @params = new object[] { };
            }
            else if (col != null && col.Type == typeof(bool) && ((@operator == "!=" && right is bool b2 && !b2) || (@operator == "!=" && right is bool b3 && b3)))
            {
                query = $"({tableAlias}.\" {left}\" IS NOT NULL or {tableAlias}.\"{left}\" != FALSE )";
                @params = new object[] { };
            }
            else if ((right == null || right is bool rightBool2 && !rightBool2) && @operator != "!=")
            {
                query = $"{tableAlias}.\"{left}\" IS NOT NULL";
                @params = new object[] { };
            }else
            {
                var needWildcard = @operator == "like" || @operator == "ilike" || @operator == "not like" || @operator == "not ilike";
                var sqlOperator = "";

                if (col == null)
                {
                    throw new InvalidOperationException($"Invalid field in domain term ({left}, {leaf})");
                }

                query = "QUERY";
                @params = new Object[] {right};
            }

            return (query, @params);
        }
    }
}
