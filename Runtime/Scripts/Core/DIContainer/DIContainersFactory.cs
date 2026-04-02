namespace Calluna.DI
{
    internal class DIContainersFactory : Factory<DIContainers>, Injectable
    {
        private GameObjectContextsReseter _contextsReseter;

        public void Inject(Resolver resolver)
        {
            _contextsReseter = resolver.Resolve<GameObjectContextsReseter>();
        }

        public DIContainers Create()
        {
            return new DIContainers(
                new BindingsContainer(),
                new SingleInstancesContainer(),
                new NonLazyContainer(),
                new DisposablesContainer(),
                new ObjectsContainer(),
                new GameObjectsContainer(_contextsReseter),
                new CleanablesContainer());
        }
	} 
}

