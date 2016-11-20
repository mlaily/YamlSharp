using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YamlSharp.Serialization
{
    internal class ObjectActivator
    {
        Dictionary<Type, Func<object>> activators = new Dictionary<Type, Func<object>>();

        public void Add<T>(Func<object> activator)
            where T : class
        {
            activators.Add(typeof(T), activator);
        }

        public T Activate<T>() where T : class => (T)Activate(typeof(T));

        public object Activate(Type type)
        {
            if (!activators.ContainsKey(type))
                return Activator.CreateInstance(type);
            return activators[type].Invoke();
        }
    }
}
