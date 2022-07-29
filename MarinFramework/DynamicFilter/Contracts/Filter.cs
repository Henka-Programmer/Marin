using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicFilter.Contracts
{
    public static class Filter
    {
        public static SearchDomain Deserialize(string json)
        {
            var jObject = JObject.Parse(json); 
            return ParseDomain(jObject);
        }

        private static SearchDomain ParseDomain(JObject domainObject)
        {
            var typeToken = domainObject.Get("type");
            var valueToken = domainObject.Get("value");

            if (typeToken == null || typeToken.ToString() != "domain")
            {
                throw new InvalidOperationException("Invalid json domain object. the type token not exists or not a domain");
            }

            if (valueToken == null || valueToken.Type != JTokenType.Array)
            {
                throw new InvalidOperationException("Invalid json domain object. the value token not exists or not an array");
            }

            var domain = new SearchDomain();
            foreach (var token in valueToken)
            {
                if (!IsValideToken(token))
                {
                    continue;
                }
                switch (token["type"].ToString())
                {
                    case "operator": { domain.Add(token["value"].ToString()); break; }
                    case "term": { domain.Add(ParseTerm(token)); break; }
                    case "domain": { domain.AddRange(ParseDomain((JObject)token)); break; }
                    default:
                        break;
                }
            }

            return domain;
        }

        private static Term ParseTerm(JToken termToken)
        {
            if (!IsValideToken(termToken))
            {
                throw new InvalidOperationException("Invalid token");
            }

            var typeToken = termToken["type"];
            var valueToken = termToken["value"];

            if (typeToken.ToString() != "term")
            {
                throw new InvalidOperationException("Invalid json domain object. the type token not exists or not a term");
            }

            if (valueToken.Type != JTokenType.Object)
            {
                throw new InvalidOperationException("Invalid json domain object. the value token not exists or not an array");
            }

            var left = valueToken["left"].ToString();
            var op = valueToken["operator"].ToString();
            var rightToken = valueToken["right"];
            object? right = null;

            var rightType = rightToken["type"].ToString();
            var rightValue = rightToken["value"].ToString();

            switch (rightType)
            {
                case "string": { right = rightValue?.ToString(); break; }
                case "date": { right = DateTime.Parse(rightValue.ToString(), null, System.Globalization.DateTimeStyles.AdjustToUniversal); break; }
                case "number": { right = Convert.ToDouble(rightValue.ToString()); break; }
                case "undefined": { right = null; break; }
                case "boolean": { right = Convert.ToBoolean(rightValue); break; }
                default:
                    break;
            }

            return new Term(left, op, right);
        }

        private static bool IsValideToken(JToken? token)
        {
            try
            {
                if (token == null)
                {
                    return false;
                }

                if (token.Count() != 2)
                {
                    return false;
                }
                var typeToken = token["type"];
                var valueToken = token["value"];
                if (!string.IsNullOrEmpty(typeToken.ToString()))
                { 
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
