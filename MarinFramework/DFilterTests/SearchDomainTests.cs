using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DFilter;

namespace DFilterTests
{
    [TestClass]
    public class SearchDomainTests
    {
        [TestMethod]
        public void Test_Domain_Normalize()
        {
            var domain = new SearchDomain();
            domain.AddRange(new object[] { 
            "&", new Term(1, "=", 1), new Term("a", "=", "b")
            });

            var result = domain.Normalize();
            Assert.IsTrue(result.Equals(domain));

            domain = new SearchDomain(("x", "in", new[] { "y", "z"}), ("a.v", "=", "e"), "|", "|", ("a", "=", "b"), "!", ("c", ">", "d"), ("e", "!=", "f"), ("g", "=", "h"));

            var norm_domain = new SearchDomain("&", "&", "&");
            norm_domain.AddRange(domain);

            result = domain.Normalize();

            Assert.IsTrue(result.Equals(norm_domain));
        }

        /// <summary>
        /// Check that when using expression.OR on a list of domains with at least one
        /// implicit '&' the returned domain is the expected result.
        /// </summary>
        [TestMethod]
        public void Test_Domain_Implicit_AND()
        {
            var d1 = new SearchDomain(("foo", "=", 1), ("bar", "=", 1));
            var d2 = new SearchDomain("&", ("foo", "=", 2), ("bar", "=", 2));

            var expected = new SearchDomain("|", "&", ("foo", "=", 1), ("bar", "=", 1), "&", ("foo", "=", 2), ("bar", "=", 2));

            Assert.IsTrue(expected.Equals(SearchDomain.OR(d1, d2)));
        }

        /// <summary>
        /// Test that unit leaves (TRUE_LEAF, FALSE_LEAF) are properly handled in specific cases
        /// </summary>
        [TestMethod]
        public void Test_Proper_Combine_Unit_Leaves()
        {
            var _false = DomainOperators.FALSE_DOMAIN;
            var _true = DomainOperators.TRUE_DOMAIN;

            // OR with single FALSE_LEAF
            var expr = SearchDomain.OR(_false);
            Assert.IsTrue(expr.Equals(_false));

            // OR with multiple FALSE_LEAF
            expr = SearchDomain.OR(_false, _false);
            Assert.IsTrue(expr.Equals(_false));

            // OR with FALSE_LEAF and a normal leaf
            var normal = new SearchDomain(("foo", "=", "bar"));
            expr = SearchDomain.OR(_false, normal);
            Assert.IsTrue(expr.Equals(normal));

            // OR with AND of single TRUE_LEAF and normal leaf
            expr = SearchDomain.OR(SearchDomain.AND(_true), normal);
            Assert.IsTrue(expr.Equals(_true));

            // AND with single TRUE_LEAF
            expr = SearchDomain.AND(_true);
            Assert.IsTrue(expr.Equals(_true));

            //AND with multiple TRUE_LEAF
            expr = SearchDomain.AND(_true, _true);
            Assert.IsTrue(expr.Equals(_true));

            // AND with TRUE_LEAF and normal leaves
            expr = SearchDomain.AND(_true, normal);
            Assert.IsTrue(expr.Equals(normal));

            // AND with OR with single FALSE_LEAF and normal leaf
            expr = SearchDomain.AND(SearchDomain.OR(_false), normal);
            Assert.IsTrue(expr.Equals(_false));
        }

    }
}
