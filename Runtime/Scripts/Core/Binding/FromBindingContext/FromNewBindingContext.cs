namespace Calluna.DI
{

	public class FromNewBindingContext<TConcrete> : AllowInjectionBindingContext where TConcrete : new()
	{
		public FromNewBindingContext(BindingArguments arguments) : base(arguments)
		{
			_binding.ConcreteType = typeof(TConcrete);
			_binding.CreationMode = InstanceCreationMode.FromNew;
			_binding.ProvideInstanceFunction = () => new TConcrete();
		}
	}
}
