namespace Calluna.DI
{
	internal class BindingValidationInstaller : Installer
	{
		public void InstallBindings(Binder binder)
		{
			binder.Bind<InstanceCreationModeValidator>().ToNew<InstanceCreationModeValidatorImpl>();
			binder.BindToNewSelf<InstantiationInfoValidator>();
		}
	}
}