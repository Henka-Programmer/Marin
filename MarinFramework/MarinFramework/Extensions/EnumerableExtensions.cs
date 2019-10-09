using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarinFramework
{
    public static class EnumerableExtensions
    {

        
        internal static T[] Slice<T>(this IEnumerable<T> e, int? from = null, int? to = null)
        {
            return e.ToArray().Slice(from, to);
        }
        internal static T[] Slice<T>(this T[] e, int? from = null, int? to = null)
        {
            if (from == null && to == null)
            {
                // throw new InvalidOperationException("must be eather from or to parameters passed or both.");
                return e;
            }

            from = from ?? 0;
            to = to ?? e.Length;

            if (from < 0)
            {
                from = e.Length + from;
            }

            if (to < 0)
            {
                to = e.Length + to;
            }

            if (from < 0)
            {
                from = 0;
            }

            if (to < 0 || to <= from)
            {
                return new T[] { };
            }

            if (e == null)
            {
                return e;
            }

            if (from > e.Length)
            {
                from = e.Length;
            }

            if (to > e.Length)
            {
                to = e.Length;
            }


            if (to != 0)
            {
                --to;
            }
            int i = 0;
            return e.OrderBy(x => i++).Skip(from.Value).Take(to.Value - from.Value + 1).ToArray();
            //  return e.Substring(from.Value, to.Value - from.Value + 1);
        }

    }
}
