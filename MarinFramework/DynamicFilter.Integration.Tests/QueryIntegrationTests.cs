using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DynamicFilter.Integration.Tests
{
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

