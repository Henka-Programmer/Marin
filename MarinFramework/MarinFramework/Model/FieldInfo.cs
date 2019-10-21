using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MarinFramework
{
    public class FieldInfo : Dictionary<string, object>
    {
        internal FieldInfo()
        {

        }
        private T Get<T>(T @default, [CallerMemberName] string key = "")
        {
            if (ContainsKey(key))
            {
                return (T)this[key];
            }
            return (T)(this[key] = @default);
        }

        private T Get<T>([CallerMemberName] string key = "")
        {
            return Get(default(T), key);
        }

        private void Set<T>(T value, [CallerMemberName] string key = "")
        {
            this[key] = value;
        }

        /// <summary>
        /// the field's module name
        /// </summary>
        internal string Module { get => Get<string>(); set => Set(value); }
        /// <summary>
        /// modules that define this field
        /// </summary>
        internal List<string> Modules { get => Get(new List<string>()); set => Set(value); }

        /// <summary>
        /// the field's setup state: None, 'base' or 'full'
        /// </summary>
        internal string SetupDone { get => Get<string>(); set => Set(value); }

        /// <summary>
        /// absolute ordering of the field
        /// </summary>
        internal int Sequence { get => Get<int>(); set => Set(value); }

        /// <summary>
        /// Whether the field is automatically created ("magic" field
        /// </summary>
        internal bool Automatic { get => Get<bool>(); set => Set(value); }

        /// <summary>
        /// Whether the field is inherited
        /// </summary>
        internal bool Inherited { get => Get<bool>(); set => Set(value); }

        /// <summary>
        /// the corresponding inherited field
        /// </summary>
        public Field InheritedField { get => Get<Field>(); set => Set(value); }
        public string Name { get => Get<string>(); set => Set(value); }

        /// <summary>
        /// name of the model of this field
        /// </summary>
        public string ModelName { get => Get<string>(); set => Set(value); }

        /// <summary>
        /// Name of the model of values (if relational)
        /// </summary>
        public string CoModelName { get => Get<string>(); set => Set(value); }

        /// <summary>
        /// Whether the field is stored in database
        /// </summary>
        public bool Store { get => Get<bool>(true); set => Set(value); }

        /// <summary>
        ///  Whether the field is indexed in database
        /// </summary>
        public bool Index { get => Get<bool>(); set => Set(value); }

        /// <summary>
        /// whether the field is a custom field
        /// </summary>
        public bool Manual { get => Get<bool>(); set => Set(value); }

        /// <summary>
        ///  whether the field is copied over by Model.copy()
        /// </summary>
        public bool Copy { get => Get<bool>(true); set => Set(value); }

        /// <summary>
        /// collection of field dependencies
        /// </summary>
        public List<string> Depends { get => Get(new List<string>()); set => Set(value); }

        /// <summary>
        /// Whether self depends on itself
        /// </summary>
        public bool Recursive { get => Get<bool>(false); set => Set(value); }

        /// <summary>
        /// compute(recs) computes field on recs
        /// </summary>
        public Action<Model> Compute { get => Get<Action<Model>>(null); set => Set(value); }
        /// <summary>
        /// Whether field should be recomputed as admin
        /// </summary>
        public bool ComputeSudo { get => Get<bool>(false); set => Set(value); }
        /// <summary>
        /// inverse(recs) inverses field on recs
        /// </summary>
        public Action<Model> Inverse { get => Get<Action<Model>>(null); set => Set(value); }

        /// <summary>
        /// search(recs, operator, value) searches on self
        /// </summary>
        public Action<Model, string, object> Search { get => Get<Action<Model, string, object>>(null); set => Set(value); }

        /// <summary>
        /// sequence of field names, for related fields
        /// </summary>
        public string Related { get => Get<string>(); set => Set(value); }

        /// <summary>
        /// Whether related fields should be read as admin
        /// </summary>
        public bool RelatedSudo { get => Get<bool>(true); set => Set(value); }

        /// <summary>
        /// Whether this is company-dependent (property field)
        /// </summary>
        public bool CompanyDependent { get => Get<bool>(false); set => Set(value); }

        /// <summary>
        /// default(recs) returns the default value
        /// </summary>
        public object Default { get => Get<object>(null); set => Set(value); }

        /// <summary>
        ///  field label
        /// </summary>
        public string String { get => Get<string>(null); set => Set(value); }

        /// <summary>
        /// field tooltip
        /// </summary>
        public string Help { get => Get<string>(null); set => Set(value); }

        /// <summary>
        ///  whether the field is readonly
        /// </summary>
        public bool ReadOnly { get => Get<bool>(false); set => Set(value); }

        /// <summary>
        /// whether the field is required
        /// </summary>
        public bool Required { get => Get<bool>(false); set => Set(value); }

        /// <summary>
        /// set readonly and required depending on state
        /// </summary>
        public object States { get => Get<object>(null); set => Set(value); }

        /// <summary>
        /// Csv list of group xml ids
        /// </summary>
        public string Groups { get => Get<string>(string.Empty); set => Set(value); }

        /// <summary>
        /// Corresponding related field
        /// </summary>
        public Field RelatedField { get => Get<Field>(null); set => Set(value); }

        /// <summary>
        /// whether the field is prefetched
        /// </summary>
        public bool Prefetch { get => Get<bool>(true); set => Set(value); }

        /// <summary>
        /// whether the field's value depends on context
        /// </summary>
        public bool ContextDependent { get => Get<bool>(true); set => Set(value); }
        public Collections.Dictionary<string, object> Args { get => Get(new Collections.Dictionary<string, object>()); set => Set(value); }
        public string OldName { get => Get<string>(); internal set => Set(value); }

        public FieldInfo(Dictionary<string, object> args)
        {
            Args.Update(args);
        }
    }
}
