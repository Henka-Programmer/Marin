using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFilter.Integration.Tests
{

    [TestClass]
    public abstract class DbTestBase
    {
        protected readonly Model model;

        public Action<string>? Log { get; set; }
        protected DbTestBase()
        {
            model = new Model("testTable");

            model.Columns["ID"] = new Column { Name = "ID", Type = typeof(int) };
            model.Columns["StringProperty"] = new Column { Name = "StringProperty", Type = typeof(string) };
            model.Columns["DecimalProperty"] = new Column { Name = "DecimalProperty", Type = typeof(decimal) };
            model.Columns["DateTimeProperty"] = new Column { Name = "DateTimeProperty", Type = typeof(DateTime) };
            model.Columns["DoubleProperty"] = new Column { Name = "DoubleProperty", Type = typeof(double) };
            model.Columns["BooleanProperty"] = new Column { Name = "BooleanProperty", Type = typeof(bool) };
            model.Columns["NullableIntegerProperty"] = new Column { Name = "NullableIntegerProperty", Type = typeof(int?) };
        }

        //protected virtual TestDbContext GetInMemoryDbContext()
        //{
        //    var options = new DbContextOptionsBuilder<TestDbContext>()
        //                        .UseInMemoryDatabase(databaseName: "DynamicFilterInMemoryDatabase")
        //                        .Options;
        //    return new TestDbContext(options);
        //}
        protected string connectionString { get; private set; }
            = $"Data Source=ahenka.local;Initial Catalog=DynamicFilter_TestDB_{Guid.NewGuid()};User ID=sa;Password=sa@Sql2019;MultipleActiveResultSets=True;TrustServerCertificate=True;";
        protected virtual TestDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                                .UseSqlServer(connectionString)
                                .Options;
            
            //var builder = new DbContextOptionsBuilder<TestDbContext>();
            //builder.UseSqlServer($"Data Source=ahenka.local;Initial Catalog=MFM;User ID=sa;Password=sa@Sql2019;MultipleActiveResultSets=True;")
            //        .UseInternalServiceProvider(serviceProvider);

            return new TestDbContext(options);
        }
    }
}

