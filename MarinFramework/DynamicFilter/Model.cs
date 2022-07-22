using System.Collections.Generic;

namespace DynamicFilter
{
    public class Model
    {
        public string Table { get; private set; }
        public string? TableQuery { get; set; }
        public IDictionary<string, Column> Columns { get; private set; } = new Dictionary<string, Column>();

        public Model(string table, string? tableQuery = null)
        {
            Table = table;
            TableQuery = tableQuery ?? table;
        }
    }
}
