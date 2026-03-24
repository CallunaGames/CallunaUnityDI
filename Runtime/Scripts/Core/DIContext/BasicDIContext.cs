namespace Calluna.DI
{
	public class BasicDIContext : DIContextBase
	{
		protected override Resolver CreateResolver(BindingsContainer container)
		{
			Resolver containerResolver = new DIContainerResolver(container, this);
			return new CircularDependencyDetector(containerResolver);
		}
	}
}