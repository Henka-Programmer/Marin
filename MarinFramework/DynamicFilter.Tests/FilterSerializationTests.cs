using DynamicFilter.Contracts;
using DynamicFilterTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFilter.Tests
{
    [TestClass]
    public class FilterSerializationTests : DynamicFilterTestsBase
    {
        [TestMethod]
        public void Test_Deserialize_Filter()
        {
            var filterJsonStr = @"{""type"":""domain"",""value"":[{""type"":""operator"",""value"":""|""},{""type"":""domain"",""value"":[{""type"":""operator"",""value"":""&""},{""type"":""operator"",""value"":""|""},{""type"":""term"",""value"":{""left"":""name"",""operator"":""="",""right"":{""type"":""string"",""value"":""amine""}}},{""type"":""term"",""value"":{""left"":""age"",""operator"":"">"",""right"":{""type"":""string"",""value"":25}}},{""type"":""term"",""value"":{""left"":""birthday"",""operator"":""="",""right"":{""type"":""date"",""value"":""2022-02-28T23:00:00.000Z""}}}]},{""type"":""domain"",""value"":[{""type"":""operator"",""value"":""&""},{""type"":""operator"",""value"":""&""},{""type"":""term"",""value"":{""left"":""name"",""operator"":""="",""right"":{""type"":""string"",""value"":""amine""}}},{""type"":""term"",""value"":{""left"":""age"",""operator"":"">"",""right"":{""type"":""string"",""value"":25}}},{""type"":""term"",""value"":{""left"":""city"",""operator"":""="",""right"":{""type"":""string"",""value"":""Malaga""}}}]}]}";

            /* const term1: Term = ['name', '=', 'amine'];
             const term2: Term = ['age', '>', 25];
             const term3: Term = ['birthday', '=', new Date(2022, 2, 1)];

             const domain1: Domain = ['|', term1, term2, '&', term3];
             const domain2: Domain = ['&', term1, term2, '&', ['city', '=', 'Malaga']];
             const domain3: Domain = ['|', domain1, domain2];*/

            var term1 = ("name", "=", "amine");
            var term2 = ("age", ">", 25);
            var term3 = ("birthday", "=", new DateTime(2022, 2, 1));
            var domain1 = new SearchDomain("&", "|", term1, term2, term3);
            var domain2 = new SearchDomain("&", "&", term1, term2, ("city", "=", "Malaga"));
            var expectedDomain = SearchDomain.OR(domain1, domain2);
             
            var serializedDomain = Filter.Deserialize(filterJsonStr);

            Assert.IsTrue(expectedDomain.Equals(serializedDomain));
        } 
    }
}
