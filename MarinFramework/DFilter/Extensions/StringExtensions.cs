using DFilter.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFilter.Extensions
{
    internal static class StringExtensions
    {
        internal static string Format(this string str, params (string name, object value)[] namedParameters)
        {
            return namedParameters.Aggregate(str, (current, parameter) => current.Replace($"{{{parameter.name}}}", parameter.value.ToString()));
        }


        internal static string Slice(this string str, int? from = null, int? to = null)
        {
            if (from == null && to == null)
            {
                // throw new InvalidOperationException("must be eather from or to parameters passed or both.");
                return str;
            }

            from = from ?? 0;
            to = to ?? str.Length;

            if (from < 0)
            {
                from = str.Length + from;
            }

            if (to < 0)
            {
                to = str.Length + to;
            }

            if (from < 0)
            {
                from = 0;
            }

            if (to < 0 || to <= from)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            if (from > str.Length)
            {
                from = str.Length;
            }

            if (to > str.Length)
            {
                to = str.Length;
            }


            if (to != 0)
            {
                --to;
            }

            return str.Substring(from.Value, to.Value - from.Value + 1);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        internal static (string name, string alias) GetAliasFromQuery(this string fromQuery)
        {
            var fromSplited = fromQuery.Split(new string[] { " AS " }, StringSplitOptions.RemoveEmptyEntries);
            if (fromSplited.Length > 1)
            {
                return (name: fromSplited[0].Replace("\"", string.Empty), alias: fromSplited[1].Replace("\"", string.Empty));
            }

            return (name: fromSplited[0].Replace("\"", string.Empty), alias: fromSplited[0].Replace("\"", string.Empty));
        }

        /// <summary>
        ///  Generate a standard table alias name. An alias is generated as following:
        ///   - the base is the source table name (that can already be an alias)
        ///   - then, each joined table is added in the alias using a 'link field name' that is used to render unique aliases for a given path
        ///   - returns a tuple composed of the alias, and the full table alias to be added in a from condition with quoting done
        ///  Examples:
        ///   - src_table_alias='res_users', join_tables=[]:
        ///     alias = ('res_users','"res_users"')
        ///   - src_table_alias='res_users', join_tables=[(res.partner, 'parent_id')]
        ///     alias = ('res_users__parent_id', '"res_partner" as "res_users__parent_id"')
        /// </summary>
        /// <param name="srcTableAlias"> model source of the alias</param>
        /// <param name="joinedTabes">list of tuples (dst_model, link_field)</param>
        /// <returns>tuple: (table_alias, alias statement for from clause with quotes added)</returns>
        internal static (string tableAlias, string aliasStatement) GenerateTableAlias(this string srcTableAlias, params (string table, string column)[] joinedTabes)
        {
            var alias = srcTableAlias;
            if (joinedTabes == null || joinedTabes.Length == 0)
            {
                return (alias, alias.Quote());
            }
            foreach (var link in joinedTabes)
            {
                alias = $"{alias}__{link.column}";
            }

            //  Use an alternate alias scheme if length exceeds the PostgreSQL limit of 63 characters.
            if (alias.Length >= 64)
            {
                // We have to fit a crc32 hash and one underscore
                // into a 63 character alias. The remaining space we can use to add
                // a human readable prefix.
                Crc32 crc32 = new Crc32();
                var alias_hash = crc32.ComputeHash(alias.Slice(from: 2));
                var ALIAS_PREFIX_LENGTH = 63 - alias_hash.Length - 1;
                alias = $"{alias.Slice(to: ALIAS_PREFIX_LENGTH)}_{alias_hash}";
            }
            return (alias, $"{joinedTabes.Last().table} AS {alias.Quote()}");
        }

        internal static string Quote(this string toQuote)
        {
            return toQuote.Contains("\"") ? toQuote : $"\"{toQuote}\"";
        }
    }
}
