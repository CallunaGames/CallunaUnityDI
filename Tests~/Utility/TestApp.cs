using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Calluna.DI.Tests
{
    /// <summary>
    /// An app-level context with the bindings AppContext installs (pool cache, child context factory,
    /// lifecycle callers, ...), without the AppContext MonoBehaviour and its scene handling.
    /// Disposing destroys the context object and everything parented to it (e.g. the pool cache).
    /// </summary>
    internal class TestApp : IDisposable
    {
        public GameObject Root { get; }
        public BasicDIContext Context { get; }
        public Resolver Resolver => Context.Resolver;
        public Binder Binder => Context.Binder;

        public TestApp()
        {
            Root = new GameObject(nameof(TestApp));
            Context = new Bootstrapper().Resolver.Resolve<BasicDIContext>();
            new AppContextInstaller(Root).InstallBindings(Context.Binder);
        }

        public void Dispose()
        {
            if (Root != null)
                Object.DestroyImmediate(Root);
        }
    }
}
