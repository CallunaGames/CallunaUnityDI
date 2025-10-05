using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    public interface ObjectActivator
    {
        void Activate(GameObject gameObject);
        void Disable(GameObject gameObject);
    }
}
