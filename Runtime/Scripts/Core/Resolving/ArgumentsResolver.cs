using System;
using System.Collections.Generic;

namespace Calluna.DI
{
    internal class ArgumentsResolver : ResolverBase
    {
        private Resolver _baseResolver;
        private Dictionary<BindingKey, object> _arguments;

        public ArgumentsResolver(Resolver context, int size = 0)
		{
            _baseResolver = context;
            _arguments = new Dictionary<BindingKey, object>(size);
        }

        public void AddArgument<TContract>(TContract argument, IComparable iD = default)
		{
            _arguments.Add(CreateKey<TContract>(iD), argument);
        }

        public void SetArgument<TContract>(TContract argument, IComparable iD = default)
        {
            _arguments[CreateKey<TContract>(iD)] = argument;
        }

        public void AddArguments(Dictionary<BindingKey, object> arguments)
		{
			foreach (KeyValuePair<BindingKey, object> pair in arguments)
				_arguments.Add(pair.Key, pair.Value);
		}

		protected override TContract DoResolve<TContract>(BindingKey key)
		{
			return _arguments.TryGetValue(key, out object value)
				? (TContract)value
				: _baseResolver.Resolve<TContract>(key);
		}

		public override bool IsResolvable(BindingKey key)
		{
			return _arguments.ContainsKey(key) || _baseResolver.IsResolvable(key);
		}
	}
}
