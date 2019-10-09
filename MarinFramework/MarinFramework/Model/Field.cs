using System;
using System.Collections.Generic;
using System.Text;

namespace MarinFramework
{
    public abstract class Field
    {
        public bool DependsContext { get; set; }
        public object Read(Model model)
        {
            throw new NotImplementedException();
        }

        internal object GetCashKey(Environment env)
        {
            throw new NotImplementedException();
        }
    }
}
