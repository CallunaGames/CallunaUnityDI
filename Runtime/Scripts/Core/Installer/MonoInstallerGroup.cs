using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    /// <summary>
    /// A <see cref="MonoInstaller"/> that delegates to an ordered list of child installers.
    /// Each child is injected (if <see cref="Injectable"/>) before its
    /// <see cref="MonoInstaller.InstallBindings"/> is called, mirroring the sequencing guarantee
    /// that <c>MonoContext</c> provides for top-level installers. Useful when a context accumulates
    /// many installers; group them under a single reference instead.
    /// </summary>
    public class MonoInstallerGroup : MonoInstaller, Injectable
    {
        [SerializeField] private List<MonoInstaller> _installers = new();

        private Resolver _resolver;

        void Injectable.Inject(Resolver resolver) => _resolver = resolver;

        public override void InstallBindings(Binder binder)
        {
            foreach (MonoInstaller installer in _installers)
            {
                (installer as Injectable)?.Inject(_resolver);
                installer.InstallBindings(binder);
            }
        }
    }
}
