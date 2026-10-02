using UnityEngine;

namespace Calluna.DI
{
    [DisallowMultipleComponent]
    internal abstract class MonoContext : MonoBehaviour, Context
    {
        [SerializeField] private MonoInstaller[] _monoInstallers = new MonoInstaller[0];

        [SerializeField]
        private ScriptableObjectInstaller[] _scriptableObjectInstallers = new ScriptableObjectInstaller[0];

        protected abstract DIContext DIContext { get; }
        public bool IsInitialized { get; private set; } = false;
        private bool _bindingsValidated;

        protected Resolver _resolver => DIContext.Resolver;
        protected Binder _binder => DIContext.Binder;


        public virtual void Init(Resolver baseResolver)
        {
            ValidateInitCall();
            IsInitialized = true;
            DoInit(baseResolver);
            InstallBindings();
            ValidateBindingsOnce();
            DoInjection();
            DIContext.CreateNonLazyInstances();
            InitializeObjects();
            DIContext.PostInit();
        }

        private void ValidateBindingsOnce()
        {
            if (_bindingsValidated) return;
            DIContext.ValidateBindings();
            _bindingsValidated = true;
        }

        // No OnApplicationQuit here: Unity calls it on the contexts in no reliable order, so a parent
        // could be reset before its children. The AppContext handles quitting for all contexts.

        protected virtual void OnDestroy()
        {
            TryReset();
        }

        void Context.Reset()
        {
            TryReset();
        }

        public Resolver GetResolver() => _resolver;

        /// <summary>
        /// Calls <see cref="QuitHandler.HandleQuit"/> on the context's objects: first the components it
        /// traverses, then its instances in reverse creation order.
        /// </summary>
        internal void HandleQuit()
        {
            if (!IsInitialized)
                return;
            HandleQuitOfObjects();
            DIContext.HandleQuit();
        }

        protected virtual void HandleQuitOfObjects()
        {
        }

        protected abstract void DoInit(Resolver resolver);
        protected abstract void DoInjection();

        protected virtual void InitializeObjects()
        {
        }

        protected virtual void CleanObjects()
        {
        }

        protected virtual void DoReset()
        {
            CleanObjects();
            DIContext.Reset();
            IsInitialized = false;
        }

        private void InstallBindings()
        {
            Resolver resolver = DIContext.Resolver;
            foreach (MonoInstaller installer in _monoInstallers)
                InstallBindings(installer, resolver);
            foreach (ScriptableObjectInstaller installer in _scriptableObjectInstallers)
                InstallBindings(installer, resolver);
        }

        private void InstallBindings(Installer installer, Resolver resolver)
        {
            try
            {
                (installer as Injectable)?.Inject(resolver);
            }
            catch (MissingBindingException e)
            {
                throw new MissingBindingException(e, installer.GetType());
            }
            installer.InstallBindings(_binder);
        }

        private void TryReset()
        {
            if (IsInitialized)
                DoReset();
        }

        private void ValidateInitCall()
        {
            if (IsInitialized)
                throw CreateContextAlreadyIsInitializedException();
        }

        protected abstract ContextAlreadyIsInitializedException CreateContextAlreadyIsInitializedException();
    }
}