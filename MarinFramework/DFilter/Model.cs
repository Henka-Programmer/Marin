using System.Collections.Generic;

namespace DFilter
{
    public class Model
    {
        public string Table { get; set; }
        public string TableQuery { get; set; }
        public Dictionary<string, Column> Columns { get; private set; } = new Dictionary<string, Column>();
    }
}
