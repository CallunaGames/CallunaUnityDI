namespace Calluna.DI
{
    public class ChildDIContext : DIContextBase
    {
	    private Resolver _resolver;
		public DIContext Parent { get; private set; }

		protected override void DoInjection(Resolver resolver)
		{
			base.DoInjection(resolver);
			_resolver = resolver;
			Parent = resolver.Resolve<DIContext>();
		}

		protected override Resolver CreateResolver(BindingsContainer container)
		{
			Resolver containerResolver = new DIContainerResolver(container, this);
			Resolver detector = new CircularDependencyDetector(containerResolver);
			return new ChildResolver(_resolver, detector);
		}

		public void MoveInstancesToParent()
		{
			Parent.TransferInstancesOf(_containers);
		}
    }
}