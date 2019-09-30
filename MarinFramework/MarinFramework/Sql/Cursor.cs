using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarinFramework.Sql
{
    /// <summary>
    /// Represents an open transaction to the PostgreSQL DB backend, acting as a lightweight wrapper around Npgsl
    /// </summary>
    public class Cursor : IDisposable
    {
        /// <summary>
        ///  Decent limit on size of IN queries - guideline = Oracle limit
        /// </summary>
        public static int IN_MAX { get; set; } = 1000;

        private bool _Closed;

        public string DatabaseName { get; }

        /// <summary>
        ///  Whether to enable snapshot isolation level for this cursor.
        /// </summary>
        private bool _serialized;
        private NpgsqlTransaction _cnx;
        private Dictionary<string, object> cache;

        private static void Check(Cursor cr)
        {
            if (cr._Closed)
            {
                throw new InvalidOperationException("Unable to use a closed cursor.");
            }
        }

        public Cursor(NpgsqlTransaction npgsqlTransaction, string dbName, string dsn, bool serialized = true)
        {
            _Closed = true;
            DatabaseName = dbName;
            _serialized = serialized;
            _cnx = npgsqlTransaction;
            _Closed = false; //  real initialisation value
            AutoCommit(false);
            cache = new Dictionary<string, object>();
        }

        private void AutoCommit(bool v)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (!_Closed && _cnx.Connection.State != System.Data.ConnectionState.Closed)
            {
                // Oops. this has not been closed explicitly.
                Close(true);
            }
        }

        private Dictionary<string, object> BuildDictionary(Npgsql.NpgsqlDataReader row)
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i < row.VisibleFieldCount; i++)
            {
                result[row.GetName(i)] = row[i];
            }
            return result;
        }

        public Dictionary<string, object> FetchOne()
        {
            if (result != null && !result.IsOnRow)
            {
                result.Read();
            }
            return BuildDictionary(result);
        }

        public Dictionary<string, object>[] FetchAll()
        {
            var rows = new List<Dictionary<string, object>>();
            while (result.Read())
            {
                rows.Add(BuildDictionary(result));
            }
            return rows.ToArray();
        }

        public Dictionary<string, object>[] FetchMany(int size)
        {
            var rows = new List<Dictionary<string, object>>();
            var s = size;
            while (s > 0 && result.Read())
            {
                --s;
                rows.Add(BuildDictionary(result));
            }
            return rows.ToArray();
        }

        Npgsql.NpgsqlDataReader result = null;
        public int Execute(string query, params object[] @params)
        {
            using (var npgsqlCommand = new Npgsql.NpgsqlCommand(query, _cnx.Connection, _cnx))
            {
                result = npgsqlCommand.ExecuteReader();
            }
            return result.RecordsAffected;
        }

        public void Commit()
        {
            _cnx.Commit();
        }

        public void Rollback()
        {
            _cnx.Rollback();
        }

        public SavePoint SavePoint()
        {
            return new SavePoint();
        }

        private void Close(bool v)
        {
            throw new NotImplementedException();
        }
    }
}
