using System;
using System.Collections.Generic;

namespace DynamicFilter
{
    public class QueryParameter
    {
        public string Name { get; private set; }
        public object? Value { get; set; }
        public Type Type { get; set; }

        internal QueryParameter(string name, Type type, object? value)
        {
            Name = NormalizeName(name);
            Type = type;
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

        //internal static List<QueryParameter> GetQueryParameters(string prefix, Type type, params object[] values)
        //{
        //    var counter = 1;
        //    var result = new List<QueryParameter>();
        //    foreach (var value in values)
        //    {
        //        var name = $"{prefix}{counter}";
        //        result.Add(new QueryParameter(name, type, value));
        //        counter++;
        //    }

        //    return result;
        //}
    }
}
