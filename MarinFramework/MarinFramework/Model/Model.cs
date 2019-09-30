using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MarinFramework
{
    public class Model : INotifyPropertyChanged, IEnumerable
    {
        Dictionary<string, object> values;
        public dynamic this[string pname]
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

        protected Model Create(Dictionary<string, object> keyValues)
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

        public virtual void Write(Dictionary<string, object> keyValues)
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
