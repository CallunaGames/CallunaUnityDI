using UnityEngine;

namespace Calluna.DI
{
    public class ObjectCreationModeBindingContext<TConcrete> : CreationModeBindingContext<TConcrete> where TConcrete : UnityEngine.Object
    {
        public ObjectCreationModeBindingContext(BindingArguments arguments) : base(arguments) { }

        public AllowInjectionBindingContext FromResources(string path)
        {
            ValidateRessource(path);
            _binding.CreationMode = InstanceCreationMode.FromResources;
            _binding.ProvideInstanceFunction = () => Resources.Load<TConcrete>(path);
            return new AllowInjectionBindingContext(_arguments);
        }

        private void ValidateRessource(string path)
        {
            if (Resources.Load<TConcrete>(path) == null)
                throw new MissingComponentException($"There is no ressource of type {typeof(TConcrete)} at path {path}");
        }
    }
}
