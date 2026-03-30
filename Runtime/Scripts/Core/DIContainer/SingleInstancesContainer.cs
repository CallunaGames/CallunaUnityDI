using System;
using System.Collections.Generic;

namespace Calluna.DI
{
    public class SingleInstancesContainer
    {
        private readonly Dictionary<Binding, object> _singleInstances = 
            new Dictionary<Binding, object>(new BindingComparer());

        public bool TryGet<TContract>(Binding binding, out TContract instance)
        {
            if (_singleInstances.TryGetValue(binding, out object value))
            {
                instance = (TContract)value;
                return true;
            }
            instance = default;
            return false;
        }

        public void Store<TContract>(Binding key, TContract instance)
        {
            ValidateHasNoSingleInstance(key);
            _singleInstances.Add(key, instance);
        }

        private void ValidateHasNoSingleInstance(Binding key)
        {
            if (_singleInstances.ContainsKey(key))
                throw new MissingSingleInstanceException();
        }

		internal void Clear()
		{
            _singleInstances.Clear();
        }
	}
}
