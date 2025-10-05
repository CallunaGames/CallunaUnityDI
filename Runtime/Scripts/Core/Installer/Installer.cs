using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    public interface Installer
    {
        public void InstallBindings(Binder binder);
    }
}