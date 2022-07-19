using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DFilter
{
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
                return $"({lhs} AND {rhs})";
            }
            else if (@operator == DomainOperators.OR)
            {
                return $"({lhs} OR {rhs})";
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
            var resultStack = new Stack<(string query, QueryParameter[] @params)>();
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

                        var allParams = new List<QueryParameter>();
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

                if (!SearchDomain.IsLeaf(leaf, out Term term))
                {
                    throw new InvalidOperationException($"Invalid leaf, {leaf}");
                }

                // Get working variables
                var (left, @operator, right) = (term.Item1 as string, term.Item2, term.Item3);

                var path = left.Split('.');
                var column = model.Columns.Get(path[0]);

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
                    var leafToSql = LeafToSql(leaf, model, alias);
                    resultStack.Push(leafToSql);
                }
            }

            if (resultStack.Count > 0)
            {
                this.Result = resultStack.Pop();
            }

            Query.AddWhere(Result.query, Result.@params);
        }

        public (string query, QueryParameter[] @params) Result { get; private set; }

        private (string, QueryParameter[]) LeafToSql(object leaf, Model model, string alias)
        {
            SearchDomain.IsLeaf(leaf, out Term term);

            var checkNull = false;
            var (left, @operator, right) = term;

            var tableAlias = $"{alias}";

            var query = "";
            QueryParameter[] @params;

            if (term == DomainOperators.TRUE_LEAF)
            {
                query = "TRUE";
                @params = new QueryParameter[] {};
            }else if (term == DomainOperators.FALSE_LEAF)
            {
                query = "FLASE";
                @params = new QueryParameter[] {};
            }else if (@operator == "in" || @operator == "not in")
            {
                if (right is Query rq)
                {
                    var (subQuery, subParams) = rq.SubSelect();
                    query = $"([{tableAlias}].[{left}] ({subQuery}))";
                    @params = subParams;
                }else if (right is IList list)
                {
                    var column = model.Columns.Get(left.ToString());
                    if (column.Type == typeof(bool))
                    {
                        //TODO: not sure to use this case
                        throw new InvalidOperationException("Not sure to use this case");
                    }
                    else
                    {
                        @params = QueryParameter.GetQueryParameters(left.ToString(), list.Cast<object>().Where(x => x != null).ToArray()).ToArray();
                        checkNull = @params.Length < list.Count;
                    }

                    if (@params.Length > 0)
                    {
                        var inStr = string.Join(", ", @params.Select(x => $"@{x.Name}"));
                        query = $"([{tableAlias}].[{left}] {@operator} ({inStr}))";
                    }
                    else
                    {
                        // The case for (left, 'in', []) or (left, 'not in', []).
                        query = @operator == "in" ? "FLASE" : "TRUE";
                    }

                    if ((@operator == "in" && checkNull) || (@operator == "not in" && !checkNull))
                    {
                        query = $"{query} OR [{tableAlias}].[{left}] IS NULL";
                    } else if (@operator == "not in" && checkNull)
                    {
                        query = $"{query} AND [{tableAlias}].[{left}] IS NOT NULL";
                    }
                }
                else
                {
                    // Must not happened
                    throw new InvalidOperationException("Invalid domain term");
                }
            }else if (model.Columns.TryGetValue(left.ToString(), out Column col) && col.Type == typeof(bool) && ((@operator == "=" && right is bool b && !b) || (@operator == "!=" && right is bool br && br)))
            {
                query = $"([{tableAlias}].[{left}] IS NULL OR [{tableAlias}].[{left}] = FALSE )";
                @params = new QueryParameter[] { };
            }else if ((right == null || right is bool rightBool && !rightBool) && @operator == "=")
            {
                query = $"[{tableAlias}].[{left}] ";
                @params = new QueryParameter[] { };
            }
            else if (col != null && col.Type == typeof(bool) && ((@operator == "!=" && right is bool b2 && !b2) || (@operator == "!=" && right is bool b3 && b3)))
            {
                query = $"([{tableAlias}].[{left}] IS NOT NULL OR [{tableAlias}].[{left}] != FALSE )";
                @params = new QueryParameter[] { };
            }
            else if ((right == null || right is bool rightBool2 && !rightBool2) && @operator != "!=")
            {
                query = $"[{tableAlias}].[{left}] IS NOT NULL";
                @params = new QueryParameter[] { };
            }else
            {
                var needWildcard = @operator == "like" || @operator == "ilike" || @operator == "not like" || @operator == "not ilike";
                
                if (col == null)
                {
                    throw new InvalidOperationException($"Invalid field in domain term ({left}, {leaf})");
                }

                var column = $"[{tableAlias}].[{left}]";
                var rightParameterName = $"p{left}";
                query = $"({column} {@operator} @{rightParameterName})";

                if ((needWildcard && right == null) || (right != null && DomainOperators.NEGATIVE_TERM_OPERATORS.Contains(@operator)))
                {
                    query = $"({query} OR [{tableAlias}].[{left}] IS NULL)";
                }

                if (needWildcard)
                {
                    @params = new QueryParameter[] { new QueryParameter($"p{left}", $"%{right}%") };
                }
                else
                {
                    @params = new QueryParameter[] { new QueryParameter($"p{left}", right?.ToString()) };

                }
            }

            return (query, @params);
        }
    }
}
