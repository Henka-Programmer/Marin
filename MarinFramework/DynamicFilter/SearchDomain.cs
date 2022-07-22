using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DynamicFilter
{
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
            if (Count == 0)
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
                    expected += operatorsArity.Get(token.ToString()!, 0) - 1;
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

            result = count-1 > 0
                ? (new SearchDomain(Enumerable.Repeat(op, count-1).ToArray()) + result)
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
            if (element is Term t)
            {
                term = t;
                return true;
            }

            if (element is System.Runtime.CompilerServices.ITuple ituple && ituple.Length == 3)
            {
                term = new Term(ituple[0], ituple[1].ToString(), ituple[2]);
                return true;
            }

            term = null;
            return false;
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

        public static bool Equals(SearchDomain? domain, SearchDomain? other)
        {
            if ((domain == null && other == null) || ReferenceEquals(domain, other))
            {
                return true;
            }

            if (domain == null || other == null)
            {
                return false;
            }

            if (domain!.Count != other!.Count)
            {
                return false;
            }

            if (domain.Count == 0 && other.Count == 0)
            {
                return true;
            }

            return domain.Zip(other, (d, o) => 
                                            {
                                                if ((d == null && o == null) || ReferenceEquals(d, o))
                                                {
                                                    return true;
                                                }

                                                if (d == null || o == null)
                                                {
                                                    return false;
                                                }

                                                if (d is Term dterm && o is Term oterm)
                                                {
                                                    return dterm.Equals(oterm);
                                                }

                                                if (d is System.Runtime.CompilerServices.ITuple tupleD && o is System.Runtime.CompilerServices.ITuple tupleO && tupleD.Length == tupleO.Length)
                                                {
                                                    var result = true;
                                                    for(int i = 0; i < tupleD.Length-1; i++)
                                                    {
                                                        result = result && (tupleD[i] == tupleO[i]);
                                                    }
                                                    return result;
                                                }

                                                return d == o;
                                            }).All(v => v);
        }
    }
}
