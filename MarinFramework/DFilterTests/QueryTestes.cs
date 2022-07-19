using DFilter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DFilterTests
{
    [TestClass]
    public class QueryTestes
    {
        [TestMethod]
        public void Test_Simple_Query_Select_All()
        {
            var model = new Model { Table = "users"};
            model.Columns["Name"] = new Column { Name = "Name", Type = typeof(string) };
            model.Columns["ID"] = new Column { Name = "ID", Type = typeof(int) };
            var query = new Query(model.Table);

            var (queryStr, parameters) = query.Select();
            Assert.AreEqual("SELECT * FROM [users] WHERE TRUE", queryStr);
            Assert.AreEqual(0, parameters.Length);

            (queryStr, parameters) = query.Select("Name");
            Assert.AreEqual("SELECT [Name] FROM [users] WHERE TRUE", queryStr);
            Assert.AreEqual(0, parameters.Length);

            // model with alias
            model.TableQuery = "users_table";
            query = new Query(model.Table, model.TableQuery);
            (queryStr, parameters) = query.Select("Name");
            Assert.AreEqual("SELECT [Name] FROM [users_table] AS [users] WHERE TRUE", queryStr);
            Assert.AreEqual(0, parameters.Length);
        }
    }
}
