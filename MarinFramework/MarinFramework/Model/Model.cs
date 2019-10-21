﻿using MarinFramework.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MarinFramework
{
    public class Model : INotifyPropertyChanged, IEnumerable
    {
        internal string _Table;
        internal Cursor _cr { get => cr; }
        protected Cursor cr;

        internal System.Collections.Generic.Dictionary<string, Field> _Fields;

        internal List<int> _Ids { get; private set; } = new List<int>();
        public MarinFramework.Environment Env { get; set; }
        public int Id { get; set; }

        protected virtual object GetValue(Field field)
        {
            return field.Read(this);
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly Field MyPropertyProperty =
            Field.Register("MyProperty", typeof(int), typeof(Model));


        System.Collections.Generic.Dictionary<string, object> values;
        public object this[string pname]
        {
            get => values[pname];
            set => values[pname] = value;
        }

        protected Model Search(params int[] ids)
        {
            return this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void SetField<T>(T value, string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected virtual T GetField<T>(string name = "")
        {
            return default(T);
        }

        public virtual void Unlink()
        {
        }

        protected Model Create(System.Collections.Generic.Dictionary<string, object> keyValues)
        {
            return Create(keyValues);
        }

        public Model Create(object obj)
        {
            return Create(obj);
        }

        public virtual void Write(object obj)
        {

        }

        public virtual void Write(System.Collections.Generic.Dictionary<string, object> keyValues)
        {

        }

        public Model Read(string[] properties)
        {
            return this;
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
