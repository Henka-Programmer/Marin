using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFilter
{
    public interface ITableMetadata
    {
        public string TableName { get; set; }
        public IList<Column> Columns { get; set; }
        public IEnumerable<Column> PrimaryKeys { get; }
    }

    public interface ITableMetadataProvider
    {
        public ITableMetadata[] GetTablesMetadata(params string[] tablesNames);
    }
}
