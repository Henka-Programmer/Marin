using System.Collections.Generic;

namespace DynamicFilter
{
    public static class DomainOperators
    {
        public const string NOT = "!";
        public const string OR = "|";
        public const string AND = "&";

        public static readonly string[] DOMAIN_OPERATORS = { NOT, OR, AND };
        public static readonly string[] TERM_OPERATORS = { "=", "!=", "<=", "<", ">", ">=", "=like", "=ilike",
                  "like", "not like", "ilike", "not ilike", "in", "not in" };

        public static readonly string[] NEGATIVE_TERM_OPERATORS = { "!=", "not like", "not ilike", "not in" };

        public static IDictionary<string, string> DOMAIN_OPERATORS_NEGATION { get; private set; } = new Dictionary<string, string>
        {
            {AND, OR },
            {OR, AND },
        };

        public static IDictionary<string, string> TERM_OPERATORS_NEGATION { get; private set; } = new Dictionary<string, string>
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
}
