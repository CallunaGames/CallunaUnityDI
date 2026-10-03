namespace Calluna.DI
{
    internal interface DIContext
    {
	    Resolver Resolver { get; }
	    Binder Binder { get; }
	    /// <summary>Shown by the <see cref="DependencyRecorder"/>; contexts of the same name are merged there.</summary>
	    string Name { get; set; }
        void ValidateBindings();
        void CreateNonLazyInstances();
        void PostInit();
        TContract GetInstance<TContract>(Binding binding);
        void Clear();
		void Reset();
		/// <summary>Calls <see cref="QuitHandler.HandleQuit"/> on the context's instances, in reverse creation order.</summary>
		void HandleQuit();
		void TransferInstancesOf(DIContainers containers);
		/// <summary>Hands the context's bindings to the <see cref="DependencyRecorder"/> (if it's recording).</summary>
		void RecordBindings();
    }
}