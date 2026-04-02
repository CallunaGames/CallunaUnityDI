namespace Calluna.DI
{
    internal class ChildDIContext : DIContextBase
    {
	    private Resolver _resolver;
	    private DIContext _parent;

		protected override void DoInjection(Resolver resolver)
		{
			base.DoInjection(resolver);
			_resolver = resolver;
			_parent = resolver.Resolve<DIContext>();
		}

		protected override Resolver CreateResolver(BindingsContainer container)
		{
			return new ChildResolver(_resolver, new DIContainerResolver(container, this));
		}

		public void MoveInstancesToParent()
		{
			_parent.TransferInstancesOf(_containers);
		}
    }
}
