namespace Calluna.DI
{
	public class BasicDIContext : DIContextBase
	{
		protected override Resolver CreateResolver(BindingsContainer container)
		{
			return new DIContainerResolver(container, this);
		}
	}
}