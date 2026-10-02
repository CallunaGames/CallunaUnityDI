namespace Calluna.DI
{
    internal interface DIContext
    {
	    Resolver Resolver { get; }
	    Binder Binder { get; }
        void ValidateBindings();
        void CreateNonLazyInstances();
        void PostInit();
        TContract GetInstance<TContract>(Binding binding);
        void Clear();
		void Reset();
		/// <summary>Calls <see cref="QuitHandler.HandleQuit"/> on the context's instances, in reverse creation order.</summary>
		void HandleQuit();
		void TransferInstancesOf(DIContainers containers);
    }
}