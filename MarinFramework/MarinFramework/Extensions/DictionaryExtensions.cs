using System;
using System.Collections.Generic;
using System.Text;

namespace MarinFramework
{
    internal static class DictionaryExtensions
    {
        internal static Dictionary<TKey, TValue> SetDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out TValue ov))
            {
                dict[key] = value;
            }
            return dict;
        }

        internal static IEnumerable<(TKey key, TValue value)> Items<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            foreach (var item in dict)
            {
                yield return (item.Key, item.Value);
            }
        }
        internal static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue @default = default(TValue))
        {
            if (dict.TryGetValue(key, out TValue value))
            {
                return value;
            }
            return @default;
        }

        internal static void Update<TKey, TValue>(this Dictionary<TKey, TValue> dic, Dictionary<TKey, TValue> update)
        {
            foreach (var item in update)
            {
                dic[item.Key] = item.Value;
            }
        }
    }
}
