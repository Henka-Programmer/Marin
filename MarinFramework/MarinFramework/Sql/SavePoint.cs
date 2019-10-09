using System;
using Npgsql;

namespace MarinFramework.Sql
{
    public class SavePoint : IDisposable
    {
        private NpgsqlTransaction cnx;
        public string Name { get; }
        internal SavePoint(NpgsqlTransaction cnx)
        {
            this.cnx = cnx;
            Name = "SP";
            //TODO: savepoints names
            cnx.Save(Name);
        }

        public void Dispose()
        {
            cnx.Release(Name);
        }
    }
}
