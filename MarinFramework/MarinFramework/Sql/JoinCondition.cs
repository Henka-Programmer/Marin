using System.Collections.Generic;

namespace MarinFramework.Sql
{
    public class JoinCondition : System.Collections.Generic.Dictionary<(string table_a, (string table_b, string table_a_col, string table_b_col, string join)), (string condition, object[] @params)>
    {
        public new(string condition, object[] @params) this[(string table_a, (string table_b, string table_a_col, string table_b_col, string join)) key]
        {

            get => base[key];
            set => base[key] = value;
        }
    }
}
