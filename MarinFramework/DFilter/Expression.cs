using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFilter
{
    public class Query
    {

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
            return new SearchDomain() { term };
        }

        public static SearchDomain operator +(SearchDomain left, SearchDomain right)
        {
            var result = new SearchDomain(left.ToList());

            result.AddRange(right.ToList());
            return result;
        }

        public static SearchDomain operator +(SearchDomain left, List<string> right)
        {
            var result = new SearchDomain(left.ToList());

            result.AddRange(right.ToList());
            return result;
        }

        public static SearchDomain operator +(SearchDomain left, Term term)
        {
            var result = new SearchDomain(left.ToList());

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

                if (token is Term term)
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

            return stack.Pop() == -1;
        }

        public static SearchDomain Combine(string op, Term unit, Term zero, params SearchDomain[] domains)
        {
            var result = new SearchDomain();
            if (domains.SequenceEqual(new SearchDomain(unit)))
            {
                return unit;
            }

            int count = 0;
            foreach (var domain in domains)
            {
                if (domain == null)
                {
                    continue;
                }

                if (domain.Equals(unit))
                {
                    continue;
                }

                if (domain.Equals(zero))
                {
                    return zero;
                }

                result.AddRange(domain.Normalize());
                count++;
            }

            return new SearchDomain(Enumerable.Repeat(op, count - 1)) + result;
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
            if (!(obj is SearchDomain))
            {
                return false;
            }

            var searchDomain = obj as SearchDomain;

            return this.SequenceEqual(searchDomain);
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

    public class Model
    {

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
            Query = query ?? new Query();
            _expression = SearchDomain.DistributeNot(domain.Normalize());
            _rootAlias = alias;
            _rootModel = model;
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

                if (SearchDomain.IsOperator(leaf, out string @operator))
                {
                    if (@operator == DomainOperators.NOT)
                    {
                        var (expr, @params) = resultStack.Pop();
                        resultStack.Push(($"(NOT (expr))", @params));
                    }
                    else
                    {
                        var (lhs, lhs_params) = resultStack.Pop();
                        var (rhs, rhs_params) = resultStack.Pop();

                        var allParams = new List<object>();
                        allParams.AddRange(lhs_params);
                        allParams.AddRange(rhs_params);

                        resultStack.Push((FormatQuery(@operator, lhs, rhs), allParams.ToArray()));
                    }

                    continue;
                }

                if (SearchDomain.IsBoolean(leaf))
                {
                    resultStack.Push(LeafToSql(leaf, model, alias));
                }

            }
        }

        private (string, object[]) LeafToSql(object leaf, Model model, string alias)
        {
            return ("SQL TO BE GENERATED", new object[] { });
        }
    }
}
