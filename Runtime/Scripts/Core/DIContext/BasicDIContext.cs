namespace Calluna.DI
{
	internal class BasicDIContext : DIContextBase
	{
		protected override Resolver CreateResolver(BindingsContainer container)
		{
			return new DIContainerResolver(container, this);
		}
	}
}