using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

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

        public FieldInfo(Dictionary<string, object> args)
        {
            Args.Update(args);
        }

        /// <summary>
        /// Base setup: things that do not depend on other models/fields. 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="name"></param>
        public void SetupBase(Model model, string name)
        {
            if (!string.IsNullOrEmpty(SetupDone) && string.IsNullOrEmpty(Related))
            {
                //optimization for regular fields: keep the base setup
                SetupDone = "BASE";
            }
            else
            {
                // do the base setup from scratch
                SetupInfo(model, name);
                if (string.IsNullOrEmpty(Related))
                {
                    SetupRegularBase(model);
                }
                SetupDone = "BASE";
            }
        }

        private void SetupRegularBase(Model model)
        {
            throw new NotImplementedException();
        }

        private void SetupInfo(Model model, string name)
        {
            throw new NotImplementedException();
        }
    }

    /*   
    ############################################################################
    #
    # Base field setup: things that do not depend on other models/fields
    #
     
    #
    # Setup field parameter attributes
    #

    def _can_setup_from(self, field):
        """ Return whether ``self`` can retrieve parameters from ``field``. """
        return isinstance(field, type(self))

    def _get_attrs(self, model, name):
        """ Return the field parameter attributes as a dictionary. """
        # determine all inherited field attributes
        modules = set()
        attrs = {}
        if not (self.args.get('automatic') or self.args.get('manual')):
            # magic and custom fields do not inherit from parent classes
            for field in reversed(resolve_mro(model, name, self._can_setup_from)):
                attrs.update(field.args)
                if '_module' in field.args:
                    modules.add(field.args['_module'])
        attrs.update(self.args)         # necessary in case self is not in class

        attrs['args'] = self.args
        attrs['model_name'] = model._name
        attrs['name'] = name
        attrs['_modules'] = modules

        # initialize ``self`` with ``attrs``
        if attrs.get('compute'):
            # by default, computed fields are not stored, not copied and readonly
            attrs['store'] = attrs.get('store', False)
            attrs['copy'] = attrs.get('copy', False)
            attrs['readonly'] = attrs.get('readonly', not attrs.get('inverse'))
            attrs['context_dependent'] = attrs.get('context_dependent', True)
        if attrs.get('related'):
            # by default, related fields are not stored and not copied
            attrs['store'] = attrs.get('store', False)
            attrs['copy'] = attrs.get('copy', False)
            attrs['readonly'] = attrs.get('readonly', True)
        if attrs.get('company_dependent'):
            # by default, company-dependent fields are not stored and not copied
            attrs['store'] = False
            attrs['copy'] = attrs.get('copy', False)
            attrs['default'] = self._default_company_dependent
            attrs['compute'] = self._compute_company_dependent
            if not attrs.get('readonly'):
                attrs['inverse'] = self._inverse_company_dependent
            attrs['search'] = self._search_company_dependent
            attrs['context_dependent'] = attrs.get('context_dependent', True)
        if attrs.get('translate'):
            # by default, translatable fields are context-dependent
            attrs['context_dependent'] = attrs.get('context_dependent', True)
        if 'depends' in attrs:
            attrs['depends'] = tuple(attrs['depends'])

        return attrs

    def _setup_attrs(self, model, name):
        """ Initialize the field parameter attributes. """
        attrs = self._get_attrs(model, name)
        self.set_all_attrs(attrs)

        # check for renamed attributes (conversion errors)
        for key1, key2 in RENAMED_ATTRS:
            if key1 in attrs:
                _logger.warning("Field %s: parameter %r is no longer supported; use %r instead.",
                                self, key1, key2)

        # prefetch only stored, column, non-manual and non-deprecated fields
        if not (self.store and self.column_type) or self.manual or self.deprecated:
            self.prefetch = False

        if not self.string and not self.related:
            # related fields get their string from their parent field
            self.string = (
                name[:-4] if name.endswith('_ids') else
                name[:-3] if name.endswith('_id') else name
            ).replace('_', ' ').title()

        # self.default must be a callable
        if self.default is not None:
            value = self.default
            self.default = value if callable(value) else lambda model: value

    ############################################################################
    #
    # Full field setup: everything else, except recomputation triggers
    #

    def setup_full(self, model):
        """ Full setup: everything else, except recomputation triggers. """
        if self._setup_done != 'full':
            if not self.related:
                self._setup_regular_full(model)
            else:
                self._setup_related_full(model)
            self._setup_done = 'full'

    #
    # Setup of non-related fields
    #

    def _setup_regular_base(self, model):
        """ Setup the attributes of a non-related field. """
        pass

    def _setup_regular_full(self, model):
        """ Determine the dependencies and inverse field(s) of ``self``. """
        if self.depends is not None:
            return

        def get_depends(func):
            deps = getattr(func, '_depends', ())
            return deps(model) if callable(deps) else deps

        if isinstance(self.compute, pycompat.string_types):
            # if the compute method has been overridden, concatenate all their _depends
            self.depends = tuple(
                dep
                for method in resolve_mro(model, self.compute, callable)
                for dep in get_depends(method)
            )
        else:
            self.depends = tuple(get_depends(self.compute))

    #
    # Setup of related fields
    #

    def _setup_related_full(self, model):
        """ Setup the attributes of a related field. """
        # fix the type of self.related if necessary
        if isinstance(self.related, pycompat.string_types):
            self.related = tuple(self.related.split('.'))

        # determine the chain of fields, and make sure they are all set up
        target = model
        for name in self.related:
            field = target._fields[name]
            field.setup_full(target)
            target = target[name]

        self.related_field = field

        # check type consistency
        if self.type != field.type:
            raise TypeError("Type of related field %s is inconsistent with %s" % (self, field))

        # determine dependencies, compute, inverse, and search
        if self.depends is None:
            self.depends = ('.'.join(self.related),)
        self.compute = self._compute_related
        if not (self.readonly or field.readonly):
            self.inverse = self._inverse_related
        if field._description_searchable:
            # allow searching on self only if the related field is searchable
            self.search = self._search_related

        # copy attributes from field to self (string, help, etc.)
        for attr, prop in self.related_attrs:
            if not getattr(self, attr):
                setattr(self, attr, getattr(field, prop))

        for attr, value in field._attrs.items():
            if attr not in self._attrs:
                setattr(self, attr, value)

        # special case for states: copy it only for inherited fields
        if not self.states and self.inherited:
            self.states = field.states

        # special case for inherited required fields
        if self.inherited and field.required:
            self.required = True

        if self.inherited:
            self._modules.update(field._modules)

    def traverse_related(self, record):
        """ Traverse the fields of the related field `self` except for the last
        one, and return it as a pair `(last_record, last_field)`. """
        for name in self.related[:-1]:
            record = record[name][:1].with_prefetch(record._prefetch)
        return record, self.related_field

    def _compute_related(self, records):
        """ Compute the related field ``self`` on ``records``. """
        # when related_sudo, bypass access rights checks when reading values
        others = records.sudo() if self.related_sudo else records
        # copy the cache of draft records into others' cache
        if records.env.in_onchange and records.env != others.env:
            copy_cache(records - records.filtered('id'), others.env)
        #
        # Traverse fields one by one for all records, in order to take advantage
        # of prefetching for each field access. In order to clarify the impact
        # of the algorithm, consider traversing 'foo.bar' for records a1 and a2,
        # where 'foo' is already present in cache for a1, a2. Initially, both a1
        # and a2 are marked for prefetching. As the commented code below shows,
        # traversing all fields one record at a time will fetch 'bar' one record
        # at a time.
        #
        #       b1 = a1.foo         # mark b1 for prefetching
        #       v1 = b1.bar         # fetch/compute bar for b1
        #       b2 = a2.foo         # mark b2 for prefetching
        #       v2 = b2.bar         # fetch/compute bar for b2
        #
        # On the other hand, traversing all records one field at a time ensures
        # maximal prefetching for each field access.
        #
        #       b1 = a1.foo         # mark b1 for prefetching
        #       b2 = a2.foo         # mark b2 for prefetching
        #       v1 = b1.bar         # fetch/compute bar for b1, b2
        #       v2 = b2.bar         # value already in cache
        #
        # This difference has a major impact on performance, in particular in
        # the case where 'bar' is a computed field that takes advantage of batch
        # computation.
        #
        values = list(others)
        for name in self.related[:-1]:
            values = [first(value[name]) for value in values]
        # assign final values to records
        for record, value in pycompat.izip(records, values):
            record[self.name] = value[self.related_field.name]

    def _inverse_related(self, records):
        """ Inverse the related field ``self`` on ``records``. """
        # store record values, otherwise they may be lost by cache invalidation!
        record_value = {record: record[self.name] for record in records}
        for record in records:
            other, field = self.traverse_related(record)
            if other:
                other[field.name] = record_value[record]

    def _search_related(self, records, operator, value):
        """ Determine the domain to search on field ``self``. """
        return [('.'.join(self.related), operator, value)]

    # properties used by _setup_related_full() to copy values from related field
    _related_comodel_name = property(attrgetter('comodel_name'))
    _related_string = property(attrgetter('string'))
    _related_help = property(attrgetter('help'))
    _related_groups = property(attrgetter('groups'))
    _related_group_operator = property(attrgetter('group_operator'))
    _related_context_dependent = property(attrgetter('context_dependent'))

    @property
    def base_field(self):
        """ Return the base field of an inherited field, or ``self``. """
        return self.inherited_field.base_field if self.inherited_field else self

    #
    # Company-dependent fields
    #

    def _default_company_dependent(self, model):
        return model.env['ir.property'].get(self.name, self.model_name)

    def _compute_company_dependent(self, records):
        # read property as superuser, as the current user may not have access
        context = records.env.context
        if 'force_company' not in context:
            field_id = records.env['ir.model.fields']._get_id(self.model_name, self.name)
            company = records.env['res.company']._company_default_get(self.model_name, field_id)
            context = dict(context, force_company=company.id)
        Property = records.env(user=SUPERUSER_ID, context=context)['ir.property']
        values = Property.get_multi(self.name, self.model_name, records.ids)
        for record in records:
            record[self.name] = values.get(record.id)

    def _inverse_company_dependent(self, records):
        # update property as superuser, as the current user may not have access
        context = records.env.context
        if 'force_company' not in context:
            field_id = records.env['ir.model.fields']._get_id(self.model_name, self.name)
            company = records.env['res.company']._company_default_get(self.model_name, field_id)
            context = dict(context, force_company=company.id)
        Property = records.env(user=SUPERUSER_ID, context=context)['ir.property']
        values = {
            record.id: self.convert_to_write(record[self.name], record)
            for record in records
        }
        Property.set_multi(self.name, self.model_name, values)

    def _search_company_dependent(self, records, operator, value):
        Property = records.env['ir.property']
        return Property.search_multi(self.name, self.model_name, operator, value)

    #
    # Setup of field triggers
    #
    # The triggers of ``self`` are a collection of pairs ``(field, path)`` of
    # fields that depend on ``self``. When ``self`` is modified, it invalidates
    # the cache of each ``field``, and determines the records to recompute based
    # on ``path``. See method ``modified`` below for details.
    #

    def resolve_deps(self, model, path0=[], seen=frozenset()):
        """ Return the dependencies of ``self`` as tuples ``(model, field, path)``,
            where ``path`` is an optional list of field names.
        """
        model0 = model
        result = []

        # add self's own dependencies
        for dotnames in self.depends:
            if dotnames == self.name:
                _logger.warning("Field %s depends on itself; please fix its decorator @api.depends().", self)
            model, path = model0, path0
            for fname in dotnames.split('.'):
                field = model._fields[fname]
                result.append((model, field, path))
                model = model0.env.get(field.comodel_name)
                path = None if path is None else path + [fname]

        # add self's model dependencies
        for mname, fnames in model0._depends.items():
            model = model0.env[mname]
            for fname in fnames:
                field = model._fields[fname]
                result.append((model, field, None))

        # add indirect dependencies from the dependencies found above
        seen = seen.union([self])
        for model, field, path in list(result):
            for inv_field in model._field_inverses[field]:
                inv_model = model0.env[inv_field.model_name]
                inv_path = None if path is None else path + [field.name]
                result.append((inv_model, inv_field, inv_path))
            if not field.store and field not in seen:
                result += field.resolve_deps(model, path, seen)

        return result

    def setup_triggers(self, model):
        """ Add the necessary triggers to invalidate/recompute ``self``. """
        for model, field, path in self.resolve_deps(model):
            if self.store and not field.store:
                _logger.debug(
                    "Field %s depends on non-stored field %s, this operation is sub-optimal"
                    % (self, field)
                )
            if field is not self:
                path_str = None if path is None else ('.'.join(path) or 'id')
                model._field_triggers.add(field, (self, path_str))
            elif path:
                self.recursive = True
                model._field_triggers.add(field, (self, '.'.join(path)))

    ############################################################################
    #
    # Field description
    #

    def get_description(self, env):
        """ Return a dictionary that describes the field ``self``. """
        desc = {'type': self.type}
        for attr, prop in self.description_attrs:
            value = getattr(self, prop)
            if callable(value):
                value = value(env)
            if value is not None:
                desc[attr] = value

        return desc

    # properties used by get_description()
    _description_store = property(attrgetter('store'))
    _description_manual = property(attrgetter('manual'))
    _description_depends = property(attrgetter('depends'))
    _description_related = property(attrgetter('related'))
    _description_company_dependent = property(attrgetter('company_dependent'))
    _description_readonly = property(attrgetter('readonly'))
    _description_required = property(attrgetter('required'))
    _description_states = property(attrgetter('states'))
    _description_groups = property(attrgetter('groups'))
    _description_change_default = property(attrgetter('change_default'))
    _description_deprecated = property(attrgetter('deprecated'))
    _description_group_operator = property(attrgetter('group_operator'))

    @property
    def _description_searchable(self):
        return bool(self.store or self.search)

    @property
    def _description_sortable(self):
        return self.store or (self.inherited and self.related_field._description_sortable)

    def _description_string(self, env):
        if self.string and env.lang:
            model_name = self.base_field.model_name
            field_string = env['ir.translation'].get_field_string(model_name)
            return field_string.get(self.name) or self.string
        return self.string

    def _description_help(self, env):
        if self.help and env.lang:
            model_name = self.base_field.model_name
            field_help = env['ir.translation'].get_field_help(model_name)
            return field_help.get(self.name) or self.help
        return self.help

    ############################################################################
    #
    # Conversion of values
    #

    def null(self, record):
        """ Return the null value for this field in the record format. """
        return False

    def convert_to_column(self, value, record, values=None, validate=True):
        """ Convert ``value`` from the ``write`` format to the SQL format. """
        if value is None or value is False:
            return None
        return pycompat.to_native(value)

    def convert_to_cache(self, value, record, validate=True):
        """ Convert ``value`` to the cache format; ``value`` may come from an
        assignment, or have the format of methods :meth:`BaseModel.read` or
        :meth:`BaseModel.write`. If the value represents a recordset, it should
        be added for prefetching on ``record``.
        :param bool validate: when True, field-specific validation of ``value``
            will be performed
        """
        return value

    def convert_to_record(self, value, record):
        """ Convert ``value`` from the cache format to the record format.
        If the value represents a recordset, it should share the prefetching of
        ``record``.
        """
        return value

    def convert_to_read(self, value, record, use_name_get=True):
        """ Convert ``value`` from the record format to the format returned by
        method :meth:`BaseModel.read`.
        :param bool use_name_get: when True, the value's display name will be
            computed using :meth:`BaseModel.name_get`, if relevant for the field
        """
        return False if value is None else value

    def convert_to_write(self, value, record):
        """ Convert ``value`` from the record format to the format of method
        :meth:`BaseModel.write`.
        """
        return self.convert_to_read(value, record)

    def convert_to_onchange(self, value, record, names):
        """ Convert ``value`` from the record format to the format returned by
        method :meth:`BaseModel.onchange`.
        :param names: a tree of field names (for relational fields only)
        """
        return self.convert_to_read(value, record)

    def convert_to_export(self, value, record):
        """ Convert ``value`` from the record format to the export format. """
        if not value:
            return ''
        return value if record._context.get('export_raw_data') else ustr(value)

    def convert_to_display_name(self, value, record):
        """ Convert ``value`` from the record format to a suitable display name. """
        return ustr(value)

    ############################################################################
    #
    # Update database schema
    #

    def update_db(self, model, columns):
        """ Update the database schema to implement this field.
            :param model: an instance of the field's model
            :param columns: a dict mapping column names to their configuration in database
            :return: ``True`` if the field must be recomputed on existing rows
        """
        if not self.column_type:
            return

        column = columns.get(self.name)
        if not column and hasattr(self, 'oldname'):
            # column not found; check whether it exists under its old name
            column = columns.get(self.oldname)
            if column:
                sql.rename_column(model._cr, model._table, self.oldname, self.name)

        # create/update the column, not null constraint, indexes
        self.update_db_column(model, column)
        self.update_db_notnull(model, column)
        self.update_db_index(model, column)

        return not column

    def update_db_column(self, model, column):
        """ Create/update the column corresponding to ``self``.
            :param model: an instance of the field's model
            :param column: the column's configuration (dict) if it exists, or ``None``
        """
        if not column:
            # the column does not exist, create it
            sql.create_column(model._cr, model._table, self.name, self.column_type[1], self.string)
            return
        if column['udt_name'] == self.column_type[0]:
            return
        if column['udt_name'] in self.column_cast_from:
            sql.convert_column(model._cr, model._table, self.name, self.column_type[1])
        else:
            newname = (self.name + '_moved{}').format
            i = 0
            while sql.column_exists(model._cr, model._table, newname(i)):
                i += 1
            if column['is_nullable'] == 'NO':
                sql.drop_not_null(model._cr, model._table, self.name)
            sql.rename_column(model._cr, model._table, self.name, newname(i))
            sql.create_column(model._cr, model._table, self.name, self.column_type[1], self.string)

    def update_db_notnull(self, model, column):
        """ Add or remove the NOT NULL constraint on ``self``.
            :param model: an instance of the field's model
            :param column: the column's configuration (dict) if it exists, or ``None``
        """
        has_notnull = column and column['is_nullable'] == 'NO'

        if not column or (self.required and not has_notnull):
            # the column is new or it becomes required; initialize its values
            if model._table_has_rows():
                model._init_column(self.name)

        if self.required and not has_notnull:
            sql.set_not_null(model._cr, model._table, self.name)
        elif not self.required and has_notnull:
            sql.drop_not_null(model._cr, model._table, self.name)

    def update_db_index(self, model, column):
        """ Add or remove the index corresponding to ``self``.
            :param model: an instance of the field's model
            :param column: the column's configuration (dict) if it exists, or ``None``
        """
        indexname = '%s_%s_index' % (model._table, self.name)
        if self.index:
            try:
                with model._cr.savepoint():
                    sql.create_index(model._cr, indexname, model._table, ['"%s"' % self.name])
            except psycopg2.OperationalError:
                _schema.error("Unable to add index for %s", self)
        else:
            sql.drop_index(model._cr, indexname, model._table)

    ############################################################################
    #
    # Read from/write to database
    #

    def read(self, records):
        """ Read the value of ``self`` on ``records``, and store it in cache. """
        return NotImplementedError("Method read() undefined on %s" % self)

    def create(self, record_values):
        """ Write the value of ``self`` on the given records, which have just
        been created.
        :param record_values: a list of pairs ``(record, value)``, where
            ``value`` is in the format of method :meth:`BaseModel.write`
        """
        for record, value in record_values:
            self.write(record, value)

    def write(self, records, value):
        """ Write the value of ``self`` on ``records``.
        :param value: a value in the format of method :meth:`BaseModel.write`
        """
        return NotImplementedError("Method write() undefined on %s" % self)

    ############################################################################
    #
    # Descriptor methods
    #

    def __get__(self, record, owner):
        """ return the value of field ``self`` on ``record`` """
        if record is None:
            return self         # the field is accessed through the owner class

        if record:
            # only a single record may be accessed
            record.ensure_one()
            try:
                value = record.env.cache.get(record, self)
            except KeyError:
                # cache miss, determine value and retrieve it
                if record.id:
                    self.determine_value(record)
                else:
                    self.determine_draft_value(record)
                value = record.env.cache.get(record, self)
        else:
            # null record -> return the null value for this field
            value = self.convert_to_cache(False, record, validate=False)

        return self.convert_to_record(value, record)

    def __set__(self, record, value):
        """ set the value of field ``self`` on ``record`` """
        env = record.env

        # only a single record may be updated
        record.ensure_one()

        # adapt value to the cache level
        value = self.convert_to_cache(value, record)

        if env.in_draft or not record.id:
            # determine dependent fields
            spec = self.modified_draft(record)

            # set value in cache, inverse field, and mark record as dirty
            record.env.cache.set(record, self, value)
            if env.in_onchange:
                for invf in record._field_inverses[self]:
                    invf._update(record[self.name], record)
                env.dirty[record].add(self.name)

            # determine more dependent fields, and invalidate them
            if self.relational:
                spec += self.modified_draft(record)
            env.cache.invalidate(spec)

        else:
            # Write to database
            write_value = self.convert_to_write(self.convert_to_record(value, record), record)
            record.write({self.name: write_value})
            # Update the cache unless value contains a new record
            if not (self.relational and not all(value)):
                record.env.cache.set(record, self, value)

    ############################################################################
    #
    # Computation of field values
    #

    def _compute_value(self, records):
        """ Invoke the compute method on ``records``. """
        # initialize the fields to their corresponding null value in cache
        fields = records._field_computed[self]
        cache = records.env.cache
        for field in fields:
            for record in records:
                cache.set(record, field, field.convert_to_cache(False, record, validate=False))
        if isinstance(self.compute, pycompat.string_types):
            getattr(records, self.compute)()
        else:
            self.compute(records)

    def compute_value(self, records):
        """ Invoke the compute method on ``records``; the results are in cache. """
        fields = records._field_computed[self]
        with records.env.do_in_draft(), records.env.protecting(fields, records):
            try:
                self._compute_value(records)
            except (AccessError, MissingError):
                # some record is forbidden or missing, retry record by record
                for record in records:
                    try:
                        self._compute_value(record)
                    except Exception as exc:
                        record.env.cache.set_failed(record, [self], exc)

    def determine_value(self, record):
        """ Determine the value of ``self`` for ``record``. """
        env = record.env

        if self.store and not (self.compute and env.in_onchange):
            # this is a stored field or an old-style function field
            if self.compute:
                # this is a stored computed field, check for recomputation
                recs = record._recompute_check(self)
                if recs:
                    # recompute the value (only in cache)
                    if self.recursive:
                        recs = record
                    self.compute_value(recs)
                    # HACK: if result is in the wrong cache, copy values
                    if recs.env != env:
                        computed = record._field_computed[self]
                        for source, target in pycompat.izip(recs, recs.with_env(env)):
                            try:
                                values = {f.name: source[f.name] for f in computed}
                                target._cache.update(target._convert_to_cache(values, validate=False))
                            except MissingError as exc:
                                target._cache.set_failed(target._fields, exc)
                    # the result is saved to database by BaseModel.recompute()
                    return

            # read the field from database
            record._prefetch_field(self)

        elif self.compute:
            # this is either a non-stored computed field, or a stored computed
            # field in onchange mode
            if self.recursive:
                self.compute_value(record)
            else:
                recs = record._in_cache_without(self)
                recs = recs.with_prefetch(record._prefetch)
                self.compute_value(recs)

        else:
            # this is a non-stored non-computed field
            record.env.cache.set(record, self, self.convert_to_cache(False, record, validate=False))

    def determine_draft_value(self, record):
        """ Determine the value of ``self`` for the given draft ``record``. """
        if self.compute:
            fields = record._field_computed[self]
            with record.env.protecting(fields, record):
                self._compute_value(record)
        else:
            null = self.convert_to_cache(False, record, validate=False)
            record.env.cache.set_special(record, self, lambda: null)

    def determine_inverse(self, records):
        """ Given the value of ``self`` on ``records``, inverse the computation. """
        if isinstance(self.inverse, pycompat.string_types):
            getattr(records, self.inverse)()
        else:
            self.inverse(records)

    def determine_domain(self, records, operator, value):
        """ Return a domain representing a condition on ``self``. """
        if isinstance(self.search, pycompat.string_types):
            return getattr(records, self.search)(operator, value)
        else:
            return self.search(records, operator, value)

    ############################################################################
    #
    # Notification when fields are modified
    #

    def modified_draft(self, records):
        """ Same as :meth:`modified`, but in draft mode. """
        env = records.env

        # invalidate the fields on the records in cache that depend on
        # ``records``, except fields currently being computed
        spec = []
        for field, path in records._field_triggers[self]:
            if not field.compute:
                # Note: do not invalidate non-computed fields. Such fields may
                # require invalidation in general (like *2many fields with
                # domains) but should not be invalidated in this case, because
                # we would simply lose their values during an onchange!
                continue

            target = env[field.model_name]
            protected = env.protected(field)
            if path == 'id' and field.model_name == records._name:
                target = records - protected
            elif path and env.in_onchange:
                target = (env.cache.get_records(target, field) - protected).filtered(
                    lambda rec: rec if path == 'id' else rec._mapped_cache(path) & records
                )
            else:
                target = env.cache.get_records(target, field) - protected

            if target:
                spec.append((field, target._ids))

        return spec 
         */

    /// <summary>
    /// The field descriptor contains the field definition, and manages accesses 
    /// and assignments of the corresponding field on records.
    /// </summary>
    public abstract class Field
    {
        private readonly FieldInfo _Slots = new FieldInfo();
        /// <summary>
        /// Type of the field
        /// </summary>
        public Type Type { get; protected set; }

        /// <summary>
        /// Whether the field is a relational one
        /// </summary>
        public bool Relational { get; protected set; }

        /// <summary>
        /// Whether the field is translated
        /// </summary>
        public bool Translate { get; protected set; } = false;

        public object ColumnType { get; protected set; }
        public string ColumnFormat { get; protected set; } = "@{0}";
        public Type[] ColumnCastFrom { get; set; }

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
