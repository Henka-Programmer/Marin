using System;
using System.Collections.Generic;
using System.Text;

namespace MarinFramework
{
    public abstract class Field
    {
        private Type _ModelType;
        public string ModelName { get => _ModelType.Name; }
        public Collections.Dictionary<object, object> DependsContext { get; set; }
        public string Name { get; protected set; }

        public object Read(Model model)
        {
            throw new NotImplementedException();
        }

        internal object GetCashKey(Environment env)
        {
            throw new NotImplementedException();
        }

        internal object ConvertToRecord(object value, Model record)
        {
            throw new NotImplementedException();
        }
    }
}
