using DynamicFilter.SqlServer;
using Microsoft.Data.SqlClient;

namespace DynamicFilter.Integration.Tests
{
    [TestClass]
    public class TableMetadataProviderTests : DbTestBase
    {
        [TestMethod]
        public void Get_Table_Metadat_Single_By_TableName()
        {
            // creating db context to ensure that the db is created.
            using (var context = GetDbContext())
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var sqlServerProvider = new TableMetadataProvider(connection);

                    var metadatas = sqlServerProvider.GetTablesMetadata("testTable");
                    var testTableMetadata = metadatas[0];

                    Assert.AreEqual(1, metadatas.Length);
                    AssertTestTableMetadata(testTableMetadata);
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

                    var usersMetadata = tablesMetadata.First(x => x.TableName == "users");
                    var testTableMetadata = tablesMetadata.First(x => x.TableName == "testTable");
                    AssertTestTableMetadata(testTableMetadata);
                    AssertUsersTableMetadata(usersMetadata);
                }
            }
        }

        private void AssertTestTableMetadata(ITableMetadata testTableMetadata)
        {
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
        private void AssertUsersTableMetadata(ITableMetadata testTableMetadata)
        {
            Assert.AreEqual("users", testTableMetadata.TableName);
            Assert.AreEqual(6, testTableMetadata.Columns.Count);

            var ID = testTableMetadata.Columns.First(x => x.Name == "ID");
            var Username = testTableMetadata.Columns.First(x => x.Name == "Username");
            var FirstName = testTableMetadata.Columns.First(x => x.Name == "FirstName");
            var LastLogin = testTableMetadata.Columns.First(x => x.Name == "LastLogin");
            var IsAdmin = testTableMetadata.Columns.First(x => x.Name == "IsAdmin");
            var SubscriptionType = testTableMetadata.Columns.First(x => x.Name == "SubscriptionType");

            Assert.AreEqual(typeof(int), ID.Type);
            Assert.AreEqual(typeof(string), Username.Type);
            Assert.AreEqual(typeof(string), FirstName.Type);
            Assert.AreEqual(typeof(DateTime), LastLogin.Type);
            Assert.AreEqual(typeof(bool), IsAdmin.Type);
            Assert.AreEqual(typeof(int), SubscriptionType.Type);
        }
    }
}

