using System.Collections;
using System.Text;

namespace DynamicFilter
{
    internal class QueryParameterList : IEnumerable<QueryParameter>
    {
        public const string COLUMN_NAME_TOKEN = "{columnName}";
        public const string COLUMN_NAME_COUNTER_TOKEN = "{counter}";
        public const string PARAMETER_NAME_FORMAT = "{columnName}{counter}";

        public int Count => _parameters.Count;

        private readonly List<QueryParameter> _parameters = new();
        private readonly Dictionary<string, int> _parametersCounter = new(StringComparer.InvariantCultureIgnoreCase);

        public QueryParameter[] Create(string name, Type type, in object[] values)
        {
            var result = new List<QueryParameter>();
            for (int i = 0; i < values.Length; i++)
            {
                object? value = values[i];
                result.Add(Create(name, $"{PARAMETER_NAME_FORMAT}", type, value, false));
            }

            return result.ToArray();
        }

        public QueryParameter Create(string name, Type type, object? value)
        {
            return Create(name, PARAMETER_NAME_FORMAT, type, value);
        }

        public void Append(IEnumerable<QueryParameter> queryParameters)
        {
            _parameters.AddRange(queryParameters);
        }

        public IEnumerator<QueryParameter> GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        private QueryParameter Create(string columnName, string parameterNameFormat, Type type, object? value, bool isZeroBased = true)
        {
            if (string.IsNullOrWhiteSpace(columnName) || columnName.Contains(' '))
            {
                throw new ArgumentException("Name can't be empty or null or invalid indentifier");
            }

            int counter = _parametersCounter.Get(columnName, 0);

            var parameterNameBuilder = new StringBuilder();

            if (string.IsNullOrWhiteSpace(parameterNameFormat))
            {
                parameterNameFormat = PARAMETER_NAME_FORMAT;
            }

            parameterNameBuilder.Append(parameterNameFormat);

            if (parameterNameFormat.Contains(COLUMN_NAME_TOKEN))
            {
                parameterNameBuilder = parameterNameBuilder.Replace(COLUMN_NAME_TOKEN, columnName);
            }

            if (parameterNameFormat.Contains(COLUMN_NAME_COUNTER_TOKEN))
            {
                if (counter > 0 || !isZeroBased)
                {
                    parameterNameBuilder = parameterNameBuilder.Replace(COLUMN_NAME_COUNTER_TOKEN, (counter + 1).ToString());
                }
                else
                {
                    parameterNameBuilder = parameterNameBuilder.Replace(COLUMN_NAME_COUNTER_TOKEN, string.Empty);
                }
            }

            counter += 1;

            var parameter = new QueryParameter(parameterNameBuilder.ToString(), type, value);

            _parametersCounter[columnName] = counter;

            _parameters.Add(parameter);

            return parameter;
        }
       
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }
    }
}
