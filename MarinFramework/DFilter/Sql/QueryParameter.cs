using System;
using System.Collections.Generic;

namespace DFilter
{
    public class QueryParameter
    {
        public string Name { get; private set; }
        public object Value { get; private set; }

        internal QueryParameter(string name, object value)
        {
            Name = NormalizeName(name);
            Value = value;
        }

        public static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Invalid parameter name");
            }
            name = name.Replace(" ", string.Empty);

            return name.StartsWith("p") ? name : $"p{name}";
        }

        internal static List<QueryParameter> GetQueryParameters(string prefix, params object[] values)
        {
            var counter = 1;
            var result = new List<QueryParameter>();
            foreach (var value in values)
            {
                var name = $"{prefix}{counter}";
                result.Add(new QueryParameter(name, value));
                counter++;
            }

            return result;
        }
    }
}
