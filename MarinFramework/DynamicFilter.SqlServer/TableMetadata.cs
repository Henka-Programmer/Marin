using System.Data;

namespace DynamicFilter.SqlServer
{
    public sealed class TableMetadata : ITableMetadata
    {
        public string TableName { get; set; }
        public IList<Column> Columns { get; set; } = new List<Column>();
        public IEnumerable<Column> PrimaryKeys { get => Columns.Where(c => c.PrimaryKey); }
    }
}