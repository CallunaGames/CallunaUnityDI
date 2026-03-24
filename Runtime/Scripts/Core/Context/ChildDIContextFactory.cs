namespace Calluna.DI
{
    public class ChildDIContextFactory : Factory<ChildDIContext, Resolver>
    {
        public ChildDIContext Create(Resolver resolver)
        {
            ChildDIContext result = new ChildDIContext();
            (result as Injectable).Inject(resolver);
            return result;
        }
    }
}

