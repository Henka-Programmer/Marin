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

            var normal = new SearchDomain(("foo", "=", "bar"));
            // OR with single FALSE_LEAF
            var expr = SearchDomain.OR(_false);
            Assert.IsTrue(expr.Equals(_false));
        }
    }
}
