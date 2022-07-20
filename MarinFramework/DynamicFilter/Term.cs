using System;

namespace DynamicFilter
{
    public class Term : Tuple<object, string, object>
    {
        public Term(object item1, string item2, object item3) : base(item1, item2, item3)
        {
        }

        public static implicit operator Term((int, string, int) v)
        {
            return new Term(v.Item1, v.Item2, v.Item3);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is Term other && this.Item1 == other.Item1 && this.Item2 == other.Item2 && this.Item3 == other.Item3);
        }
    }
}
