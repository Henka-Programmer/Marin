using DynamicFilter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DynamicFilterTests
{
    [TestClass]
    public class QueryTests : DynamicFilterTestsBase
    {
        [TestMethod]
        public void Test_Simple_Query_Select_All()
        {
            var query = new Query(model!.Table);

            var (queryStr, parameters) = query.Select();
            Assert.AreEqual("SELECT * FROM [users] WHERE TRUE", queryStr);
            Assert.AreEqual(0, parameters.Length);

            (queryStr, parameters) = query.Select("Name");
            Assert.AreEqual("SELECT [Name] FROM [users] WHERE TRUE", queryStr);
            Assert.AreEqual(0, parameters.Length);

            // model with alias
            model!.TableQuery = "users_table";
            query = new Query(model.Table, model!.TableQuery);
            (queryStr, parameters) = query.Select("Name");
            Assert.AreEqual("SELECT [Name] FROM [users_table] AS [users] WHERE TRUE", queryStr);
            Assert.AreEqual(0, parameters.Length);
        }
    }
}
