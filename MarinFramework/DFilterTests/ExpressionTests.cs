using DFilter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DFilterTests
{
    [TestClass]
    public class ExpressionTests
    {
        [TestMethod]
        public void Test_Expression_With_Simple_Search_Domain()
        {
            var model = new Model { Table = "users" };
            model.Columns["Name"] = new Column { Name = "Name", Type = typeof(string) };
            model.Columns["ID"] = new Column { Name = "ID", Type = typeof(int) };

            var domain = new SearchDomain("|", ("Name", "=", "henka"), ("ID", "in", new int[] { 10, 13, 2 }));
            var expression = new Expression(domain, model, model.Table);
            var (queryStr, parameters) = expression.Query.Select();
            Assert.AreEqual("SELECT * FROM [users] WHERE (([users].[Name] = @pName) OR ([users].[ID] in (@pID1, @pID2, @pID3)))", queryStr);
            Assert.AreEqual(4, parameters.Length);

            //check parameters names and values
            Assert.AreEqual("pName", parameters[0].Name);
            Assert.AreEqual("henka", parameters[0].Value);

            Assert.AreEqual("pID1", parameters[1].Name);
            Assert.AreEqual(10, parameters[1].Value);

            Assert.AreEqual("pID2", parameters[2].Name);
            Assert.AreEqual(13, parameters[2].Value);

            Assert.AreEqual("pID3", parameters[3].Name);
            Assert.AreEqual(2, parameters[3].Value);

        }
    }
}
