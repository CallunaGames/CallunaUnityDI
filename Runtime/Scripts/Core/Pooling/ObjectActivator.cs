using UnityEngine;

namespace Calluna.DI
{
    public interface ObjectActivator
    {
        void Activate(GameObject gameObject);
        void Disable(GameObject gameObject);
    }
}
