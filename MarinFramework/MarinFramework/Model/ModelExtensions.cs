using System.Collections.Generic;

namespace MarinFramework
{
    public static class ModelExtensions
    {
        public static TModel Browse<TModel>(this TModel model, params int[] ids) where TModel : Model
        {
            return model;
        }

        public static TModel Search<TModel>(this TModel model, params (string property, string opt, object value)[] searchDomain) where TModel : Model
        {
            return model;
        }

        /// <summary>
        /// Select the records in self such that func(rec) is true, and return them as a recordset.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="model"></param>
        /// <param name="searchDomain"></param>
        /// <returns></returns>
        public static TModel Filtered<TModel>(this TModel model, params (string property, string opt, object value)[] searchDomain) where TModel : Model
        {
            return model;
        }

        public static TModel Create<TModel>(this TModel model, Dictionary<string, object> keyValues) where TModel : Model
        {
            return model;
        }
        public static TModel Sorted<TModel>(this TModel model, string key, bool reverse = false) where TModel : Model
        {
            return model;
        }

        public static TModel Mapped<TModel>(this TModel model, string key) where TModel : Model
        {
            return model;
        }
    }

}
