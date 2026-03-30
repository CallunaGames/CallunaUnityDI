using System.Collections.Generic;

namespace Calluna.DI
{
	public class CircularDependencyDetector : ResolverBase
	{
		// Tracks keys currently being resolved; re-entry on the same key signals a cycle.
	private HashSet<BindingKey> _resolveStack = new HashSet<BindingKey>(new BindingKeyComparer());
		private Resolver _baseResolver;

		public CircularDependencyDetector(Resolver baseResolver)
		{
			_baseResolver = baseResolver;
		}

		public override bool IsResolvable(BindingKey key)
		{
			return _baseResolver.IsResolvable(key);
		}

		protected override TContract DoResolve<TContract>(BindingKey key)
		{
			ValidateIsNoCircularDependency(key);
			_resolveStack.Add(key);
			TContract result = _baseResolver.Resolve<TContract>(key);
			_resolveStack.Remove(key);
			return result;
		}

		private void ValidateIsNoCircularDependency(BindingKey key)
		{
			if (_resolveStack.Contains(key))
			{
				_resolveStack.Clear();
				throw new CircularDependencyException(key);
			}
		}
	}
}
