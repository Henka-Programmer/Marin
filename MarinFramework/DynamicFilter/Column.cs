using System;

namespace DynamicFilter 
{
    public class Column
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool Nullable { get; set; }
        public bool ForeignKey { get; set; }
        public bool PrimaryKey { get; set; }
    }
}
