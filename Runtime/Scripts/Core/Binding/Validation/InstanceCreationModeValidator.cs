using UnityEngine;

namespace Calluna.DI
{
    public abstract class InstanceCreationModeValidator 
    {
        public abstract void Validate(InstantiationInfo instantiationInfo);
    }
}
