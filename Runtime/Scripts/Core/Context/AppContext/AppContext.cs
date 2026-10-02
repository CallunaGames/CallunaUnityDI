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
            InstallAppContextBindings();
            _sceneContextProvider = _resolver.Resolve<SceneContextProvider>();
        }

        private void InstallAppContextBindings()
        {
            AppContextInstaller installer = new AppContextInstaller(gameObject);
            installer.InstallBindings(_binder);
        }

        protected override void DoInjection() { }

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
                TryRun(sceneContext, sceneContext.HandleQuit);
            TryRun(this, HandleQuit);

            foreach (SceneContext sceneContext in sceneContexts)
                TryRun(sceneContext, ((Context)sceneContext).Reset);
            ((Context)this).Reset();
        }

#pragma warning disable CS0618 // QuitDetector stays functional until it's removed in 2.0.0.
        private void NotifyQuitDetector()
        {
            TryRun(this, () => _resolver.Resolve<QuitDetector>().NotifyQuit());
        }
#pragma warning restore CS0618

        private static void TryRun(MonoContext context, Action action)
        {
            if (context == null)
                return;
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e, context);
            }
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
