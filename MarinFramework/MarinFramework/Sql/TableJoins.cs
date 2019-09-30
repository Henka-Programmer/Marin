using System.Collections.Generic;
using System.Text;

namespace MarinFramework.Sql
{
    public class TableJoins : Dictionary<string, List<(string table_b, string table_col, string table_b_col, string join)>>
    {
        public new List<(string table_b, string table_col, string table_b_col, string join)> this[string key]
        {

            get
            {
                if (ContainsKey(key))
                {
                    var v = base[key];
                    if (v == null)
                    {
                        base[key] = v = new List<(string table_b, string table_col, string table_b_col, string join)>();
                    }
                    return v;
                }
                else
                {
                    return base[key] = new List<(string table_b, string table_col, string table_b_col, string join)>();
                }
            }
            set => base[key] = value;
        }
    }
}
