using UnityEngine;

namespace Calluna.DI
{
    internal class BasicObjectActivator : ObjectActivator
    {
        public void Activate(GameObject gameObject)
        {
            gameObject.SetActive(true);
        }
        
        public void Disable(GameObject gameObject)
        {
            gameObject.SetActive(false);
        }
    }
}
