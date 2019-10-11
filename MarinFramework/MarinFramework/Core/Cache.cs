using MarinFramework.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarinFramework
{
    /// <summary>
    /// Implementation of the cache of records.
    /// </summary>
    public class Cache
    {
        readonly System.Collections.Generic.Dictionary<Field, System.Collections.Generic.Dictionary<object, object>> _data = new System.Collections.Generic.Dictionary<Field, System.Collections.Generic.Dictionary<object, object>>();

        /// <summary>
        /// Return whether <paramref name="record"/> has a value for <paramref name="field"/>.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public bool Contains(Model record, Field field)
        {
            if (field.DependsContext)
            {
                var key = field.GetCashKey(record.Env);
                return _data.Get(field, @default: new System.Collections.Generic.Dictionary<object, object>()).ContainsKey(key);
            }
            return _data[field].ContainsKey(record.Id);
        }

        /// <summary>
        /// Return the value of field for record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="record"></param>
        /// <param name="field"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public T Get<T>(Model record, Field field, T @default = default(T))
        {
            /*
              def get(self, record, field, default=NOTHING):
        """ Return the value of ``field`` for ``record``. """
        try:
            value = self._data[field][record._ids[0]]
            if field.depends_context:
                value = value[field.cache_key(record.env)]
            return value
        except KeyError:
            if default is NOTHING:
                raise CacheMiss(record, field)
            return default
             */
            try
            {
                var v = _data[field][record._Ids[0]];
                //if (field.DependsContext)
                //{
                //    v = v[field.GetCashKey(record.Env)];
                //}
                return (T)v;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
        }

        /// <summary>
        /// Set the value of field for record
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Set<T>(Model record, Field field, object value)
        {
            /*
              def set(self, record, field, value):
        """ Set the value of ``field`` for ``record``. """
        if field.depends_context:
            key = field.cache_key(record.env)
            self._data[field].setdefault(record._ids[0], {})[key] = value
        else:
            self._data[field][record._ids[0]] = value
             */
            if (field.DependsContext)
            {
                var key = field.GetCashKey(record.Env);
                var d = (System.Collections.Generic.Dictionary<object, object>)_data[field][record._Ids[0]];
                d[key] = value;
            }
            else
            {
                _data[field][record._Ids[0]] = value;
            }
        }

        /// <summary>
        /// Set the values of field for several records.
        /// </summary>
        /// <param name="records"></param>
        /// <param name="field"></param>
        /// <param name="values"></param>
        public void Update(Model records, Field field, object[] values)
        {
            /*
              def update(self, records, field, values):
        """ Set the values of ``field`` for several ``records``. """
        if field.depends_context:
            key = field.cache_key(records.env)
            field_cache = self._data[field]
            for record_id, value in zip(records._ids, values):
                field_cache.setdefault(record_id, {})[key] = value
        else:
            self._data[field].update(zip(records._ids, values))

             */
            if (field.DependsContext)
            {
                var key = field.GetCashKey(records.Env);
                var fieldCashe = _data[field];
                foreach ((int recordId, object value) in records._Ids.Zip(values, (x, y) => (x, y)))
                {
                    var c = (System.Collections.Generic.Dictionary<object, object>)fieldCashe[recordId];
                    c[key] = value;
                }
            }
            else
            {
                _data[field].Update(records._Ids.Zip(values, (id, v) => { return (id, v); }).ToDictionary(k => (object)k.id, v => v.v));
            }
        }

        /// <summary>
        /// Remove the value of field for record
        /// </summary>
        /// <param name="record"></param>
        /// <param name="field"></param>
        public void Remove(Model record, Field field)
        {
            /*
             * def remove(self, record, field):
        """ Remove the value of ``field`` for ``record``. """
        try:
            del self._data[field][record.id]
        except KeyError:
            pass
             */
            try
            {
                _data[field]?.Remove(record.Id);
            }
            catch (KeyNotFoundException)
            {
                // swallow
            }
        }

        /// <summary>
        /// Return the cached values of field for records.
        /// </summary>
        /// <param name="records"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public IEnumerable<object> GetValues(Model records, Field field)
        {
            /*
             def get_values(self, records, field):
        """ Return the cached values of ``field`` for ``records``. """
        field_cache = self._data[field]
        key = field.cache_key(records.env) if field.depends_context else None
        for record_id in records._ids:
            try:
                if key is not None:
                    yield field_cache[record_id][key]
                else:
                    yield field_cache[record_id]
            except KeyError:
                pass
             */
            var field_cache = _data[field];
            var key = field.DependsContext ? field.GetCashKey(records.Env) : null;
            foreach (var recordId in records._Ids)
            {
                if (key != null)
                {
                    yield return ((System.Collections.Generic.Dictionary<object, object>)field_cache[recordId])[key];
                }
                else
                {
                    yield return field_cache[recordId];
                }

            }
        }

        /// <summary>
        ///  Return the subset of records that has not value for field. 
        /// </summary>
        /// <param name="records"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public TModel GetRecordsDifferentFrom<TModel>(TModel records, Field field, object value) where TModel : Model
        {
            /*
              def get_records_different_from(self, records, field, value):
        """ Return the subset of ``records`` that has not ``value`` for ``field``. """
        field_cache = self._data[field]
        key = field.cache_key(records.env) if field.depends_context else None
        ids = []
        for record_id in records._ids:
            try:
                if key is not None:
                    val = field_cache[record_id][key]
                else:
                    val = field_cache[record_id]
            except KeyError:
                ids.append(record_id)
            else:
                if val != value:
                    ids.append(record_id)
        return records.browse(ids)

             */
            var field_cache = _data[field];
            var key = field.DependsContext ? field.GetCashKey(records.Env) : null;
            var ids = new List<int>();
            foreach (var recordId in records._Ids)
            {
                object val = null;
                try
                {
                    if (key != null)
                    {
                        var c = (System.Collections.Generic.Dictionary<object, object>)field_cache;
                        if (c != null)
                        {
                            val = c[key];
                        }
                    }
                    else
                    {
                        val = field_cache[recordId];
                    }
                }
                catch (KeyNotFoundException)
                {
                    ids.Add(recordId);
                }
                catch
                {
                    if (val != value)
                    {
                        ids.Add(recordId);
                    }
                }
            }

            return records.Browse(ids.ToArray());
        }

        /// <summary>
        /// Return the fields with a value for record.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="records"></param>
        /// <returns></returns>
        public IEnumerable<Field> GetFields<TModel>(TModel record) where TModel : Model
        {
            /* 
            def get_fields(self, record):
        """ Return the fields with a value for ``record``. """
        for name, field in record._fields.items():
            if name == 'id':
                continue
            values = self._data.get(field, {})
            if record.id not in values:
                continue
            if field.depends_context and field.cache_key(record.env) not in values[record.id]:
                continue
            yield field
             */
            foreach ((string name, Field field) in record._Fields.Items())
            {
                if (name == "id")
                {
                    continue;
                }
                var values = _data.Get(field, @default: new System.Collections.Generic.Dictionary<object, object>());
                if (!values.ContainsKey(record.Id))
                {
                    continue;
                }
                if (field.DependsContext)
                {
                    var key = field.GetCashKey(record.Env);
                    var d = (System.Collections.Generic.Dictionary<object, object>)values[record.Id];
                    if (!d.ContainsKey(key))
                    {
                        continue;
                    }
                }

                yield return field;
            }
        }

        public TModel GetRecords<TModel>(TModel model, Field field) where TModel : Model
        {
            /*
              def get_records(self, model, field):
        """ Return the records of ``model`` that have a value for ``field``. """
        ids = list(self._data[field])
        return model.browse(ids)
             */
            var ids = _data[field].Values.Cast<int>().ToArray();
            return model.Browse(ids);
        }

        /// <summary>
        /// Return the ids of records that have no value for field.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="records"></param>
        /// <param name="field"></param>
        public IEnumerable<int> GetMissingIds<TModel>(TModel records, Field field) where TModel : Model
        {
            /*
                def get_missing_ids(self, records, field):
        """ Return the ids of ``records`` that have no value for ``field``. """
        field_cache = self._data[field]
        for record_id in records._ids:
            if record_id not in field_cache:
                yield record_id 
             */
            var fieldCache = _data[field];
            foreach (var recordId in records._Ids)
            {
                if (!fieldCache.ContainsKey(recordId))
                {
                    yield return recordId;
                }
            }
        }

        /// <summary>
        /// Invalidate the cache, partially or totally depending on spec. 
        /// </summary>
        /// <param name="spec"></param>
        public void Invalidate(System.Collections.Generic.Dictionary<Field, int[]> spec = null)
        {
            /*
             *   def invalidate(self, spec=None):
            """ Invalidate the cache, partially or totally depending on ``spec``. """
            if spec is None:
                self._data.clear()
            elif spec:
                for field, ids in spec:
                    if ids is None:
                        self._data.pop(field, None)
                    else:
                        field_cache = self._data.get(field)
                        if field_cache:
                            for id in ids:
                                field_cache.pop(id, None)

             */
            if (spec == null)
            {
                _data.Clear();
            }
            else if (spec != null)
            {
                foreach ((Field field, int[] ids) in spec.Items())
                {
                    if (ids == null || ids.Length == 0)
                    {
                        _data.Remove(field);
                    }
                    else
                    {
                        var fieldCache = _data.Get(field);
                        if (fieldCache != null)
                        {
                            foreach (var id in ids)
                            {
                                fieldCache.Remove(id);
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Check the consistency of the cache for the given environment.
        /// </summary>
        /// <param name="env"></param>
        public void Check(Environment env)
        {
            /*
              def check(self, env):
            """ Check the consistency of the cache for the given environment. """
            # flush fields to be recomputed before evaluating the cache
            env['res.partner'].recompute()

            # make a full copy of the cache, and invalidate it
            dump = defaultdict(dict)
            key_cache = self._data
            for field, field_cache in key_cache.items():
                for record_id, value in field_cache.items():
                    if record_id:
                        dump[field][record_id] = value

            self.invalidate()

            # re-fetch the records, and compare with their former cache
            invalids = []
            for field, field_dump in dump.items():
                records = env[field.model_name].browse(field_dump)
                for record in records:
                    try:
                        cached = field_dump[record.id]
                        if field.depends_context:
                            for context_keys, value in cached.items():
                                context = dict(zip(field.depends_context, context_keys))
                                value = field.convert_to_record(value, record)
                                fetched = record.with_context(context)[field.name]
                                if fetched != value:
                                    info = {'cached': value, 'fetched': fetched}
                                    invalids.append((record, field, info))
                        else:
                            cached = field_dump[record.id]
                            fetched = record[field.name]
                            value = field.convert_to_record(cached, record)
                            if fetched != value:
                                info = {'cached': value, 'fetched': fetched}
                                invalids.append((record, field, info))
                    except (AccessError, MissingError):
                        pass

            if invalids:
                raise UserError('Invalid cache for fields\n' + pformat(invalids))

             */
            // flush fields to be recomputed before evaluating the cache
            // env['res.partner'].recompute()

            // make a full copy of the cache, and invalidate it
            var dump = new System.Collections.Generic.Dictionary<Field, System.Collections.Generic.Dictionary<object, object>>();
            var keyCache = _data;
            foreach ((Field field, System.Collections.Generic.Dictionary<object, object> fieldCache) in keyCache.Items())
            {
                dump.SetDefault(field, new System.Collections.Generic.Dictionary<object, object>());
                foreach ((object recordId, object value) in fieldCache.Items())
                {
                    if (recordId != null)
                    {
                        dump[field][recordId] = value;
                    }
                }
            }

            Invalidate();
            List<(Model record, Field field, object info)> invalids = new List<(Model record, Field field, object info)>();

            // re-fetch the records, and compare with their former cache
            foreach ((Field field, System.Collections.Generic.Dictionary<object, object> fieldDump) in dump.Items())
            {
                var ids = fieldDump.Keys.Cast<int>().ToArray();
                var records = env[field.ModelName].Browse(ids);
                foreach (Model record in records)
                {
                    try
                    {
                        object info = null;
                        var cached = fieldDump[record.Id];
                        if (field.DependsContext)
                        {
                            foreach ((object contextKeys, object value) in ((Dictionary<object, object>)cached).Items())
                            {
                                //TODO: us the context dependency
                                //var context = field.DependsContext.Zip();
                                object v = field.ConvertToRecord(value, record);
                                var fetched = record[field.Name];
                                if (fetched != v)
                                {
                                    info = new { cached = v, fetched };
                                    invalids.Add((record, field, info));
                                }
                            }
                        }
                        else
                        {
                            cached = fieldDump[record.Id];
                            var fetched = record[field.Name];
                            var v = field.ConvertToRecord(cached, record);
                            if (fetched != v)
                            {
                                info = new { cached = v, fetched };
                                invalids.Add((record, field, info));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // TODO:
                    }
                }
            }

            if (invalids.Count > 1)
            {
                //TODO enhance printing the error detail
                throw new UserErrorException($"Invalid cache for fields\n{invalids.ToString()}");
            }

        }
    }
}