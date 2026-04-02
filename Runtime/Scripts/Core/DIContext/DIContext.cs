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
		void TransferInstancesOf(DIContainers containers);
    }
}