using System;
using System.Collections.Generic;
using System.Text;

namespace MarinFramework.Collections
{
    public class Dictionary<Tkey, TValue> : System.Collections.Generic.Dictionary<Tkey, TValue>
    {
        public static implicit operator bool(Dictionary<Tkey, TValue> dict)
        {
            return dict != null && dict.Count == 0;
        }
    } 
}
