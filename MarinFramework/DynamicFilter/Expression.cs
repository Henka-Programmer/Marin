using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DynamicFilter
{
    public partial class Expression
    {
        private readonly SearchDomain _expression;
        private readonly string? _rootAlias;
        private readonly Model _rootModel;
        private QueryParameterList _params = new();

        public Query Query { get; private set; }
        public (string query, QueryParameter[] @params) Result { get; private set; }

        public Expression(SearchDomain domain, Model model, string? alias = null, Query? query = null)
        {
            Query = query ?? new Query(model.Table, model.TableQuery);
            _expression = SearchDomain.DistributeNot(domain.Normalize());
            _rootAlias = alias ?? model.Table;
            _rootModel = model;

            Result = (string.Empty, Array.Empty<QueryParameter>());

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
            _params = new();
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
                        //if (@operator == ">" || @operator == "<=")
                        //{
                        //    right = right.ToString() + " 23:59:59";
                        //}
                        //else
                        //{
                        //    right = right.ToString() + " 00:00:00";
                        //}
                        right = DateOnly.Parse(str);
                        stack.Push(((left, @operator, right), model, alias));
                    }
                    else if (right is DateTime dt && dt.TimeOfDay == TimeSpan.Zero)
                    {
                        //if (@operator == ">" || @operator == "<=")
                        //{
                        //    right = dt.Add(new TimeSpan(0, 23, 59, 59, 99999));
                        //}
                        right = DateOnly.FromDateTime(dt);

                        stack.Push(((left, @operator, right), model, alias));
                    }
                    else
                    {
                        var leafToSql = LeafToSql(leaf, model, alias);
                        resultStack.Push(leafToSql);
                    }
                }
                else //if (right != null)
                {
                    var leafToSql = LeafToSql(leaf, model, alias);
                    resultStack.Push(leafToSql);
                }
            }

            if (resultStack.Count > 0)
            {
                Result = resultStack.Pop();
            }

            Query.AddWhere(Result.query, Result.@params);
        }


        private (string, QueryParameter[]) LeafToSql(object leaf, Model model, string alias)
        {
            if (!SearchDomain.IsLeaf(leaf, out Term term) || term == null)
            {
                throw new ArgumentException("Invalid leaf!");
            }

            var checkNull = false;
            var (left, @operator, right) = term;
            QueryParameter[] @params = Array.Empty<QueryParameter>();
            if (left == null)
            {
                throw new InvalidOperationException($"Invalid term with invalid left: ({left},{@operator},{right})");
            }

            var tableAlias = $"{alias}";
            var query = "";

            if (term == DomainOperators.TRUE_LEAF)
            {
                query = "TRUE";
            }
            else if (term == DomainOperators.FALSE_LEAF)
            {
                query = "FLASE";
            }
            else if (@operator == "in" || @operator == "not in")
            {
                if (right is Query rq)
                {
                    var (subQuery, subParams) = rq.Subselect();
                    query = $"([{tableAlias}].[{left}] ({subQuery}))";
                    _params.Append(subParams);
                }
                else if (right is IList list)
                {
                    var column = model.Columns.Get(left.ToString()!);
                    if (column == null)
                    {
                        throw new InvalidOperationException($"Column definition missed? No such column in {model.Table} model.");
                    }
                    if (column.Type == typeof(bool))
                    {
                        //TODO: not sure to use this case
                        throw new InvalidOperationException("Not sure to use this case");
                    }
                    else
                    {
                        //@params = QueryParameter.GetQueryParameters(left.ToString()!, column.Type, list.Cast<object>().Where(x => x != null).ToArray()).ToArray();
                        @params = _params.Create(left.ToString()!, column.Type, list.Cast<object>().Where(x => x != null).ToArray()).ToArray();
                        checkNull = _params.Count < list.Count;
                    }

                    if (_params.Count > 0)
                    {
                        var inStr = string.Join(", ", _params.Select(x => $"@{x.Name}"));
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
                    }
                    else if (@operator == "not in" && checkNull)
                    {
                        query = $"{query} AND [{tableAlias}].[{left}] IS NOT NULL";
                    }
                }
                else
                {
                    // Must not happened
                    throw new InvalidOperationException("Invalid domain term");
                }
            }
            else if (model.Columns.TryGetValue(left.ToString()!, out Column? col) && col.Type == typeof(bool) && ((@operator == "=" && right is bool b && !b) || (@operator == "!=" && right is bool br && br)))
            {
                query = $"([{tableAlias}].[{left}] IS NULL OR [{tableAlias}].[{left}] = FALSE )";
                @params = Array.Empty<QueryParameter>();
            }
            else if ((right == null || right is bool rightBool && !rightBool) && @operator == "=")
            {
                query = $"[{tableAlias}].[{left}] IS NULL";
                @params = Array.Empty<QueryParameter>();
            }
            else if (col != null && col.Type == typeof(bool) && ((@operator == "!=" && right is bool b2 && !b2) || (@operator == "!=" && right is bool b3 && b3)))
            {
                query = $"([{tableAlias}].[{left}] IS NOT NULL OR [{tableAlias}].[{left}] != FALSE )";
                @params = Array.Empty<QueryParameter>();
            }
            else if ((right == null || right is bool rightBool2 && !rightBool2) && @operator != "!=")
            {
                query = $"[{tableAlias}].[{left}] IS NOT NULL";
                @params = Array.Empty<QueryParameter>();
            }
            else if (col != null && col.Type == typeof(DateTime))
            {
                var parameter = _params.Create(left.ToString()!, col.Type, right);  //new QueryParameter(left.ToString()!, col.Type, right);
                query = $"[{tableAlias}].[{left}] {@operator} @{parameter.Name}";
                if (right is DateOnly dateOnly)
                {
                    query = $"CONVERT(DATE, [{tableAlias}].[{left}]) {@operator} @{parameter.Name}";

                    // convert back the value to Datetime, as the DateOnly not supported in Mapping to SQL datatypes.
                    // parameter = new QueryParameter(left.ToString()!, col.Type, dateOnly.ToDateTime(TimeOnly.MinValue));
                    parameter.Value = dateOnly.ToDateTime(TimeOnly.MinValue);
                }

                @params = new QueryParameter[] { parameter };
            }
            else
            {
                var needWildcard = @operator == "like" || @operator == "ilike" || @operator == "not like" || @operator == "not ilike";

                if (col == null)
                {
                    throw new InvalidOperationException($"Invalid field in domain term ({left}, {leaf})");
                }

                var column = $"[{tableAlias}].[{left}]";
                var rightParameter = _params.Create(left.ToString() ?? string.Empty, col.Type, right?.ToString());
                query = $"({column} {@operator} @{rightParameter.Name})";

                if ((needWildcard && right == null) || (right != null && DomainOperators.NEGATIVE_TERM_OPERATORS.Contains(@operator)))
                {
                    query = $"({query} OR [{tableAlias}].[{left}] IS NULL)";
                }

                if (needWildcard)
                {
                    rightParameter.Value = $"%{rightParameter.Value}%";
                }

                @params = new QueryParameter[]
                {
                    rightParameter
                };
            }

            return (query, @params);
        }
    }
}
