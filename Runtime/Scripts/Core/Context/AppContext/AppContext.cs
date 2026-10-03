using System;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    internal class AppContext : MonoContext
    {
        private BasicDIContext _dIContext;
        protected override DIContext DIContext => _dIContext;
        private SceneContextProvider _sceneContextProvider;
        private bool _isQuitting;
#pragma warning disable CS0618 // QuitDetector stays functional until it's removed in 2.0.0.
        private QuitDetector _quitDetector;
#pragma warning restore CS0618

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.quitting += Quit;
            Init(new Bootstrapper().Resolver);
        }

        protected override void OnDestroy()
        {
            Application.quitting -= Quit;
            base.OnDestroy();
        }

        // Unity doesn't document whether Application.quitting comes before or after the OnApplicationQuit
        // calls - whichever comes first quits; both mean the quit can't be cancelled anymore (unlike
        // Application.wantsToQuit).
        private void OnApplicationQuit() => Quit();

        protected override void DoInit(Resolver resolver)
        {
            _dIContext = resolver.Resolve<BasicDIContext>();
            ((DIContext)_dIContext).Name = ContextName;
            InstallAppContextBindings();
            _sceneContextProvider = _resolver.Resolve<SceneContextProvider>();
            // Created right away, as before 1.6.0 - not lazily while quitting.
#pragma warning disable CS0618
            _quitDetector = _resolver.Resolve<QuitDetector>();
#pragma warning restore CS0618
        }

        private void InstallAppContextBindings()
        {
            AppContextInstaller installer = new AppContextInstaller(gameObject);
            installer.InstallBindings(_binder);
        }

        protected override void DoInjection() { }

        protected override string ContextName => nameof(AppContext);

        /// <summary>
        /// Shuts all contexts down in two phases, children before their parents: first every
        /// <see cref="QuitHandler"/> while all contexts are intact, then the reset of every context.
        /// Runs once; the contexts don't react to quitting themselves.
        /// </summary>
        internal void Quit()
        {
            if (_isQuitting || !IsInitialized)
                return;
            _isQuitting = true;
            _sceneContextProvider.IsQuitting = true;
            NotifyQuitDetector();

            IReadOnlyList<SceneContext> sceneContexts = _sceneContextProvider.GetInDependencyOrder();
            foreach (SceneContext sceneContext in sceneContexts)
                if (sceneContext != null)
                    SafeInvoker.Invoke(sceneContext, context => context.HandleQuit(), sceneContext);
            SafeInvoker.Invoke(this, context => context.HandleQuit(), this);

            foreach (SceneContext sceneContext in sceneContexts)
                if (sceneContext != null)
                    SafeInvoker.Invoke<Context>(sceneContext, context => context.Reset(), sceneContext);
            ((Context)this).Reset();
        }

        private void NotifyQuitDetector()
        {
            if (_quitDetector != null)
                SafeInvoker.Invoke(_quitDetector, detector => detector.NotifyQuit(), this);
        }

        public Resolver GetResolverFor(SceneContext sceneContext)
        {
            string parentID = sceneContext.ParentContextID;
            return string.IsNullOrEmpty(parentID) ? _resolver : _sceneContextProvider.Get(parentID).GetResolver();
        }

		protected override ContextAlreadyIsInitializedException CreateContextAlreadyIsInitializedException()
		{
            return new AppContextAlreadyIsInitializedException(name);
        }
	}
}
