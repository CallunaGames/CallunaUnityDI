namespace Calluna.DI
{
    internal interface Context
    {
        public void Init(Resolver baseResolver);
        public void Reset();
        public Resolver GetResolver();
    }
}

