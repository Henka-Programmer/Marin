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
    }
}
