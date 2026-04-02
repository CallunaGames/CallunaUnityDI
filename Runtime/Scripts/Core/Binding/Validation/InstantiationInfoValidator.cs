using System;

namespace Calluna.DI
{
    internal class InstantiationInfoValidator : Injectable
    {
        private InstanceCreationModeValidator _fromValidator;

		void Injectable.Inject(Resolver resolver)
		{
            _fromValidator = resolver.Resolve<InstanceCreationModeValidator>();
        }

		public void Validate(InstantiationInfo info)
		{
            if (info == null)
                throw new ArgumentNullException();
            _fromValidator.Validate(info);
        }
	}
}