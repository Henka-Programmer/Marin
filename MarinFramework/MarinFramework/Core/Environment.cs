using MarinFramework.Sql;
using System;
using System.Linq;

namespace MarinFramework
{ 
    /// <summary>
    /// An environment wraps data for ORM records:
    /// It provides access to the registry by implementing a mapping from model names orm models.
    /// t also holds a cache for records, and a data structure to manage recomputation
    /// </summary>
    public class Environment
    {
        /// <summary>
        ///  the current database cursor
        /// </summary>
        protected Cursor Cr;
        /// <summary>
        /// the current user id
        /// </summary>
        protected int UId;
        /// <summary>
        /// the current context dictionary
        /// </summary>
        protected Collections.Dictionary<object, object> Context;
        /// <summary>
        /// whether in superuser mode.
        /// </summary>
        protected bool Su;
        public Model this[string modelName]
        {
            get => null;
        }

        public Model this[Type type]
        {
            get => this[type.Name];
        }
    }

    /*
      
class Environment(Mapping): 
    _local = Local()

    @classproperty
    def envs(cls):
        return getattr(cls._local, 'environments', ())

    @classmethod
    @contextmanager
    def manage(cls):
        """ Context manager for a set of environments. """
        if hasattr(cls._local, 'environments'):
            yield
        else:
            try:
                cls._local.environments = Environments()
                yield
            finally:
                release_local(cls._local)

    @classmethod
    def reset(cls):
        """ Clear the set of environments.
            This may be useful when recreating a registry inside a transaction.
        """
        cls._local.environments = Environments()

    def __new__(cls, cr, uid, context, su=False):
        if uid == SUPERUSER_ID:
            su = True
        assert context is not None
        args = (cr, uid, context, su)

        # if env already exists, return it
        env, envs = None, cls.envs
        for env in envs:
            if env.args == args:
                return env

        # otherwise create environment, and add it in the set
        self = object.__new__(cls)
        args = (cr, uid, frozendict(context), su)
        self.cr, self.uid, self.context, self.su = self.args = args
        self.registry = Registry(cr.dbname)
        self.cache = envs.cache
        self._protected = envs.protected        # proxy to shared data structure
        self.all = envs
        envs.add(self)
        return self

    #
    # Mapping methods
    #

    def __contains__(self, model_name):
        """ Test whether the given model exists. """
        return model_name in self.registry

    def __getitem__(self, model_name):
        """ Return an empty recordset from the given model. """
        return self.registry[model_name]._browse(self, (), ())

    def __iter__(self):
        """ Return an iterator on model names. """
        return iter(self.registry)

    def __len__(self):
        """ Return the size of the model registry. """
        return len(self.registry)

    def __eq__(self, other):
        return self is other

    def __ne__(self, other):
        return self is not other

    def __hash__(self):
        return object.__hash__(self)

    def __call__(self, cr=None, user=None, context=None, su=None):
        """ Return an environment based on ``self`` with modified parameters.

            :param cr: optional database cursor to change the current cursor
            :param user: optional user/user id to change the current user
            :param context: optional context dictionary to change the current context
            :param su: optional boolean to change the superuser mode
        """
        cr = self.cr if cr is None else cr
        uid = self.uid if user is None else int(user)
        context = self.context if context is None else context
        su = (user is None and self.su) if su is None else su
        return Environment(cr, uid, context, su)

    def ref(self, xml_id, raise_if_not_found=True):
        """ return the record corresponding to the given ``xml_id`` """
        return self['ir.model.data'].xmlid_to_object(xml_id, raise_if_not_found=raise_if_not_found)

    def is_superuser(self):
        """ Return whether the environment is in superuser mode. """
        return self.su

    def is_admin(self):
        """ Return whether the current user has group "Access Rights", or is in
            superuser mode. """
        return self.su or self.user._is_admin()

    def is_system(self):
        """ Return whether the current user has group "Settings", or is in
            superuser mode. """
        return self.su or self.user._is_system()

    @lazy_property
    def user(self):
        """ return the current user (as an instance) """
        return self(su=True)['res.users'].browse(self.uid)

    @lazy_property
    def company(self):
        """ return the company in which the user is logged in (as an instance) """
        company_ids = self.context.get('allowed_company_ids', False)
        if company_ids:
            company_id = int(company_ids[0])
            if company_id in self.user.company_ids.ids:
                return self['res.company'].browse(company_id)
        return self.user.company_id

    @lazy_property
    def companies(self):
        """ return a recordset of the enabled companies by the user """
        try:  # In case the user tries to bidouille the url (eg: cids=1,foo,bar)
            allowed_company_ids = self.context.get('allowed_company_ids')
            # Prevent the user to enable companies for which he doesn't have any access
            users_company_ids = self.user.company_ids.ids
            allowed_company_ids = [company_id for company_id in allowed_company_ids if company_id in users_company_ids]
        except Exception:
            # By setting the default companies to all user companies instead of the main one
            # we save a lot of potential trouble in all "out of context" calls, such as
            # /mail/redirect or /web/image, etc. And it is not unsafe because the user does
            # have access to these other companies. The risk of exposing foreign records
            # (wrt to the context) is low because all normal RPCs will have a proper
            # allowed_company_ids.
            # Examples:
            #   - when printing a report for several records from several companies
            #   - when accessing to a record from the notification email template
            #   - when loading an binary image on a template
            allowed_company_ids = self.user.company_ids.ids
        return self['res.company'].browse(allowed_company_ids)

    @property
    def lang(self):
        """ return the current language code """
        return self.context.get('lang')

    def clear(self):
        """ Clear all record caches, and discard all fields to recompute.
            This may be useful when recovering from a failed ORM operation.
        """
        self.cache.invalidate()
        self.all.tocompute.clear()
        self.all.towrite.clear()

    @contextmanager
    def clear_upon_failure(self):
        """ Context manager that clears the environments (caches and fields to
            recompute) upon exception.
        """
        tocompute = {
            field: set(ids)
            for field, ids in self.all.tocompute.items()
        }
        towrite = {
            model: {
                record_id: dict(values)
                for record_id, values in id_values.items()
            }
            for model, id_values in self.all.towrite.items()
        }
        try:
            yield
        except Exception:
            self.clear()
            self.all.tocompute.update(tocompute)
            for model, id_values in towrite.items():
                for record_id, values in id_values.items():
                    self.all.towrite[model][record_id].update(values)
            raise

    def is_protected(self, field, record):
        """ Return whether `record` is protected against invalidation or
            recomputation for `field`.
        """
        return record.id in self._protected.get(field, ())

    def protected(self, field):
        """ Return the recordset for which ``field`` should not be invalidated or recomputed. """
        return self[field.model_name].browse(self._protected.get(field, ()))

    @contextmanager
    def protecting(self, what, records=None):
        """ Prevent the invalidation or recomputation of fields on records.
            The parameters are either:
             - ``what`` a collection of fields and ``records`` a recordset, or
             - ``what`` a collection of pairs ``(fields, records)``.
        """
        protected = self._protected
        try:
            protected.pushmap()
            what = what if records is None else [(what, records)]
            for fields, records in what:
                for field in fields:
                    ids = protected.get(field, frozenset())
                    protected[field] = ids.union(records._ids)
            yield
        finally:
            protected.popmap()

    def fields_to_compute(self):
        """ Return a view on the field to compute. """
        return self.all.tocompute.keys()

    def records_to_compute(self, field):
        """ Return the records to compute for ``field``. """
        ids = self.all.tocompute.get(field, ())
        return self[field.model_name].browse(ids)

    def is_to_compute(self, field, record):
        """ Return whether ``field`` must be computed on ``record``. """
        return record.id in self.all.tocompute.get(field, ())

    def not_to_compute(self, field, records):
        """ Return the subset of ``records`` for which ``field`` must not be computed. """
        ids = self.all.tocompute.get(field, ())
        return records.browse(id_ for id_ in records._ids if id_ not in ids)

    def add_to_compute(self, field, records):
        """ Mark ``field`` to be computed on ``records``. """
        if not records:
            return records
        self.all.tocompute[field].update(records._ids)

    def remove_to_compute(self, field, records):
        """ Mark ``field`` as computed on ``records``. """
        if not records:
            return
        ids = self.all.tocompute.get(field, None)
        if ids is None:
            return
        ids.difference_update(records._ids)
        if not ids:
            del self.all.tocompute[field]

    @contextmanager
    def norecompute(self):
        """ Delay recomputations (deprecated: this is not the default behavior). """
        yield



# sentinel value for optional parameters
NOTHING = object()

    */


}
