using DynamicFilter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DynamicFilterTests
{
    [TestClass]
    public class ExpressionTests : DynamicFilterTestsBase
    {
        [TestMethod]
        public void Test_Expression_With_Simple_Search_Domain()
        {
            var domain = new SearchDomain("|", ("Name", "=", "henka"), ("ID", "in", new int[] { 10, 13, 2 }));

            var expression = new Expression(domain, model!, model!.Table);
            var (queryStr, parameters) = expression.Query.Select();
            Assert.AreEqual("SELECT * FROM [users] WHERE (([users].[Name] = @pName) OR ([users].[ID] in (@pID1, @pID2, @pID3)))", queryStr);
            Assert.AreEqual(4, parameters.Length);

            //check parameters names and values
            Assert.AreEqual("pName", parameters[0].Name);
            Assert.AreEqual("henka", parameters[0].Value);
            Assert.AreEqual(typeof(string), parameters[0].Type);

            Assert.AreEqual("pID1", parameters[1].Name);
            Assert.AreEqual(10, parameters[1].Value);
            Assert.AreEqual(typeof(int), parameters[1].Type);

            Assert.AreEqual("pID2", parameters[2].Name);
            Assert.AreEqual(13, parameters[2].Value);
            Assert.AreEqual(typeof(int), parameters[2].Type);

            Assert.AreEqual("pID3", parameters[3].Name);
            Assert.AreEqual(2, parameters[3].Value);
            Assert.AreEqual(typeof(int), parameters[3].Type);

        }

        [TestMethod]
        public void Test_Expression_With_Datetime_Search_Domain()
        {
            var dateTime = new DateTime(1990, 01, 01, 5, 10, 0);
            var domain = new SearchDomain(("Birthday", "=", dateTime));
            var expression = new Expression(domain, model!, model!.Table);

            var (queryStr, parameters) = expression.Query.Select();

            Assert.AreEqual("SELECT * FROM [users] WHERE [users].[Birthday] = @pBirthday", queryStr);
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual("pBirthday", parameters[0].Name);
            Assert.AreEqual(typeof(DateTime), parameters[0].Type);
            Assert.AreEqual(dateTime, parameters[0].Value);

        }

        [TestMethod]
        public void Test_Expression_With_DateOnly_Search_Domain()
        {
            var dateOnly = new DateTime(1990, 01, 01);
            var domain = new SearchDomain(("Birthday", "=", dateOnly));
            var expression = new Expression(domain, model!, model!.Table);

            var (queryStr, parameters) = expression.Query.Select();
            Assert.AreEqual("SELECT * FROM [users] WHERE CONVERT(DATE, [users].[Birthday]) = @pBirthday", queryStr);
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual("pBirthday", parameters[0].Name);
            Assert.AreEqual(typeof(DateTime), parameters[0].Type);
            Assert.AreEqual(dateOnly, parameters[0].Value);
        }


        /*
         (1) Start with the outermost operator and move it to the start of the expression.

"(A operator B)"  becomes  "operator (A B)"
(2) Repeat step 1 for each sub expression with an operator to move.

"A operator (B operator C)"  becomes  "operator A (B operator C)"  then "operator A (operator B C)"
(3) Remove all brackets.

"A operator (B operator C)"  becomes  "operator A operator B C"
So for my example:

( A or B ) AND ( C or D or E )

First simplification:

AND ( A or B ) ( C or D or E )

left side

AND ( or A B ) ( C or D or E )

right side outer

AND ( or A B ) ( or C ( D or E ) )

right side inner

AND ( or A B ) ( or C ( or D E ) )

remove brackets

AND or A B or C or D E

        (A AND B) OR (C AND D)
        (A AND B) OR & C D
        OR (A AND B) & C D
        | (A AND B) & C D
        | & A B & C D
         */
        [TestMethod]
        public void Test_Expression_With_Complex_Condition()
        {
            var date1 = new DateTime(1991, 1, 1, 1, 1, 1);
            var date2 = new DateTime(1992, 2, 2, 2, 2, 2);
            var domain = new SearchDomain("|", "&", ("Birthday", "=", date1), ("Name", "=", "Henka"), "&" ,("Birthday", "=", date2), ("Name", "=", "Henka2"));
            var expression = new Expression(domain, model!, model!.Table);

            var (queryStr, parameters) = expression.Query.Select();

            Assert.AreEqual("SELECT * FROM [users] WHERE (([users].[Birthday] = @pBirthday2 AND ([users].[Name] = @pName2)) OR ([users].[Birthday] = @pBirthday AND ([users].[Name] = @pName)))", queryStr);
            Assert.AreEqual(4, parameters.Length);
            Assert.AreEqual(date1, parameters[0].Value);
            Assert.AreEqual("Henka", parameters[1].Value);
            Assert.AreEqual(date2, parameters[2].Value);
            Assert.AreEqual("Henka2", parameters[3].Value);
        }
    }
}
