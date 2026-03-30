using UnityEngine;

namespace Calluna.DI
{
    public class ObjectCreationModeBindingContext<TConcrete> : CreationModeBindingContext<TConcrete> where TConcrete : UnityEngine.Object
    {
        public ObjectCreationModeBindingContext(BindingArguments arguments) : base(arguments) { }

        public AllowInjectionBindingContext FromResources(string path)
        {
            ValidateResource(path);
            _binding.CreationMode = InstanceCreationMode.FromResources;
            _binding.ProvideInstanceFunction = () => Resources.Load<TConcrete>(path);
            return new AllowInjectionBindingContext(_arguments);
        }

        private static void ValidateResource(string path)
        {
            if (Resources.Load<TConcrete>(path) == null)
                throw new MissingComponentException($"There is no resource of type {typeof(TConcrete)} at path {path}");
        }
    }
}
