using DynamicFilter.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicFilter.Integration.Tests
{
    [TestClass]
    public class TableMetadataProviderTests : DbTestBase
    {
        [TestMethod]
        public void Get_Table_Metadat()
        {
            // creating db context to ensure that the db is created.
            using (var context = GetDbContext())
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var sqlServerProvider = new TableMetadataProvider(connection);
                    var testTableMetadata = sqlServerProvider.GetTablesMetadata("testTable")[0];

                    Assert.AreEqual("testTable", testTableMetadata.TableName);
                    Assert.AreEqual(7, testTableMetadata.Columns.Count);

                    var ID = testTableMetadata.Columns.FirstOrDefault(x => x.Name == "ID");
                    var StringProperty = testTableMetadata.Columns.FirstOrDefault(x => x.Name == "StringProperty");
                    var DecimalProperty = testTableMetadata.Columns.FirstOrDefault(x => x.Name == "DecimalProperty");
                    var DateTimeProperty = testTableMetadata.Columns.FirstOrDefault(x => x.Name == "DateTimeProperty");
                    var DoubleProperty = testTableMetadata.Columns.FirstOrDefault(x => x.Name == "DoubleProperty");
                    var BooleanProperty = testTableMetadata.Columns.FirstOrDefault(x => x.Name == "BooleanProperty");
                    var NullableIntegerProperty = testTableMetadata.Columns.FirstOrDefault(x => x.Name == "NullableIntegerProperty");

                    Assert.IsNotNull(ID);
                    Assert.IsNotNull(StringProperty);
                    Assert.IsNotNull(DecimalProperty);
                    Assert.IsNotNull(DateTimeProperty);
                    Assert.IsNotNull(DoubleProperty);
                    Assert.IsNotNull(BooleanProperty);
                    Assert.IsNotNull(NullableIntegerProperty);

                    Assert.AreEqual(typeof(int), ID.Type);
                    Assert.AreEqual(typeof(string), StringProperty.Type);
                    Assert.AreEqual(typeof(decimal), DecimalProperty.Type);
                    Assert.AreEqual(typeof(DateTime), DateTimeProperty.Type);
                    Assert.AreEqual(typeof(double), DoubleProperty.Type);
                    Assert.AreEqual(typeof(bool), BooleanProperty.Type);
                    Assert.AreEqual(typeof(int?), NullableIntegerProperty.Type); 

                }
            }
        }

        [TestMethod]
        public void Get_Table_Metadat_MultipleTables_With_Names()
        {
            // creating db context to ensure that the db is created.
            using (var context = GetDbContext())
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var sqlServerProvider = new TableMetadataProvider(connection);
                    var tablesMetadata = sqlServerProvider.GetTablesMetadata("testTable", "users");

                    Assert.IsNotNull(tablesMetadata);
                    Assert.AreEqual(2, tablesMetadata.Length);
                }
            }
        }
    }

    [TestClass]
    public class QueryIntegrationTests : DbTestBase
    {
        [TestMethod]
        public void Test_Select_Single_Item_By_ID()
        {
            using (var context = this.GetDbContext())
            {
                var testModel = new TestModel
                {
                    StringProperty = $"String1",
                    DateTimeProperty = DateTime.Now.AddYears(-30),
                    DecimalProperty = 1m,
                    BooleanProperty = true,
                    DoubleProperty = 2.33365,
                    NullableIntegerProperty = 1
                };

                var testModel2 = new TestModel
                {
                    StringProperty = $"String2",
                    DateTimeProperty = DateTime.Now.AddYears(-30),
                    DecimalProperty = 1m,
                    BooleanProperty = true,
                    DoubleProperty = 2.33365
                };
                context.Models.Add(testModel);
                context.Models.Add(testModel2);
                context.SaveChanges();

                Assert.AreNotEqual(0, testModel.ID);
                Assert.AreNotEqual(0, testModel2.ID);

                var domain = new SearchDomain(("ID", "=", testModel.ID));
                var expression = new Expression(domain, model);
                var query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);
                var sqlParams = query.parameters.Select(p => new SqlParameter(p.Name, p.Value)).ToArray();
                var atualResult = context.Models.FromSqlRaw(query.queryStr, sqlParams).ToArray();

                Assert.AreEqual(1, atualResult.Length);

                Assert.AreEqual(testModel.ID, atualResult[0].ID);
                Assert.AreEqual(testModel.StringProperty, atualResult[0].StringProperty);

                domain = new SearchDomain("|", ("ID", "=", testModel.ID), ("ID", "=", testModel2.ID));
                expression = new Expression(domain, model);
                query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);
                sqlParams = query.parameters.Select(p => new SqlParameter(p.Name, p.Value)).ToArray();
                atualResult = context.Models.FromSqlRaw(query.queryStr, sqlParams).ToArray();

                Assert.AreEqual(2, atualResult.Length);

                Assert.AreEqual(testModel.ID, atualResult[0].ID);
                Assert.AreEqual(testModel.StringProperty, atualResult[0].StringProperty);
                Assert.AreEqual(testModel.DecimalProperty, atualResult[0].DecimalProperty);
                Assert.AreEqual(testModel.DateTimeProperty, atualResult[0].DateTimeProperty);
                Assert.AreEqual(testModel.BooleanProperty, atualResult[0].BooleanProperty);

                Assert.AreEqual(testModel2.ID, atualResult[1].ID);
                Assert.AreEqual(testModel2.StringProperty, atualResult[1].StringProperty);
                Assert.AreEqual(testModel2.DecimalProperty, atualResult[1].DecimalProperty);
                Assert.AreEqual(testModel2.DateTimeProperty, atualResult[1].DateTimeProperty);
                Assert.AreEqual(testModel2.BooleanProperty, atualResult[1].BooleanProperty);

                // test comparing dates : date only
                domain = new SearchDomain(("DateTimeProperty", "=", testModel.DateTimeProperty.Date));
                expression = new Expression(domain, model);
                query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);
                sqlParams = query.parameters.Select(p => new SqlParameter(p.Name, p.Value)).ToArray();
                atualResult = context.Models.FromSqlRaw(query.queryStr, sqlParams).ToArray();

                Assert.AreEqual(2, atualResult.Length);

                // test comparing dates : date and time
                domain = new SearchDomain(("DateTimeProperty", "=", testModel.DateTimeProperty));
                expression = new Expression(domain, model);
                query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);

                Assert.AreEqual(1, query.parameters.Length);
                atualResult = context.Models.FromSqlRaw(query.queryStr, new SqlParameter(query.parameters[0].Name, System.Data.SqlDbType.DateTime2) { Value = query.parameters[0].Value }).ToArray();

                Assert.AreEqual(1, atualResult.Length);
                Assert.AreEqual(testModel.ID, atualResult[0].ID);

                // test in operator
                // in IDs as int list
                domain = new SearchDomain(("ID", "in", new int[] { testModel.ID, int.MaxValue, int.MinValue}));
                expression = new Expression(domain, model);
                query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);

                Assert.AreEqual(3, query.parameters.Length);
                sqlParams = query.parameters.Select(p => new SqlParameter(p.Name, p.Value)).ToArray();
                atualResult = context.Models.FromSqlRaw(query.queryStr, sqlParams).ToArray();

                Assert.AreEqual(1, atualResult.Length);
                Assert.AreEqual(testModel.ID, atualResult[0].ID);

                // not in IDs as int list
                domain = new SearchDomain(("ID", "not in", new int[] { testModel.ID, int.MaxValue, int.MinValue }));
                expression = new Expression(domain, model);
                query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);

                Assert.AreEqual(3, query.parameters.Length);
                sqlParams = query.parameters.Select(p => new SqlParameter(p.Name, p.Value)).ToArray();
                atualResult = context.Models.FromSqlRaw(query.queryStr, sqlParams).ToArray();

                Assert.AreEqual(1, atualResult.Length);
                Assert.AreEqual(testModel2.ID, atualResult[0].ID);

                // nullable integer
                domain = new SearchDomain((nameof(TestModel.NullableIntegerProperty), "=", default(int?)));
                expression = new Expression(domain, model);
                query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);

                Assert.AreEqual(0, query.parameters.Length);
                sqlParams = query.parameters.Select(p => new SqlParameter(p.Name, p.Value)).ToArray();
                atualResult = context.Models.FromSqlRaw(query.queryStr, sqlParams).ToArray();

                Assert.AreEqual(1, atualResult.Length);
                Assert.AreEqual(testModel2.ID, atualResult[0].ID);

                // nullabel 2
                domain = new SearchDomain("|", (nameof(TestModel.NullableIntegerProperty), "=", default(int?)), (nameof(TestModel.NullableIntegerProperty), "=", testModel.NullableIntegerProperty));
                expression = new Expression(domain, model);
                query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);

                Assert.AreEqual(1, query.parameters.Length);
                sqlParams = query.parameters.Select(p => new SqlParameter(p.Name, p.Value)).ToArray();
                atualResult = context.Models.FromSqlRaw(query.queryStr, sqlParams).ToArray();

                Assert.AreEqual(2, atualResult.Length);
                Assert.AreEqual(testModel.ID, atualResult[0].ID);
                Assert.AreEqual(testModel2.ID, atualResult[1].ID);

                // like operator
                domain = new SearchDomain((nameof(TestModel.StringProperty), "like", "String"));
                expression = new Expression(domain, model);
                query = expression.Query.Select();

                Assert.IsNotNull(query.queryStr);

                Assert.AreEqual(1, query.parameters.Length);
                sqlParams = query.parameters.Select(p => new SqlParameter(p.Name, p.Value)).ToArray();
                atualResult = context.Models.FromSqlRaw(query.queryStr, sqlParams).ToArray();

                Assert.AreEqual(2, atualResult.Length);
                Assert.AreEqual(testModel.ID, atualResult[0].ID);
                Assert.AreEqual(testModel2.ID, atualResult[1].ID);
            }
        }
    }
}

