using System;
using System.Collections.Generic;
using System.Text;

namespace MarinFramework
{
    internal static class DictionaryExtensions
    {
        internal static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue @default)
        {
            if (dict.TryGetValue(key, out TValue value))
            {
                return value;
            }
            return @default;
        }
    }
}
