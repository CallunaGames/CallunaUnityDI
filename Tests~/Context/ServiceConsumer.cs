using UnityEngine;

namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script.
    public class ServiceConsumer : MonoBehaviour, Injectable
    {
        public int InjectCount { get; private set; }
        public ContextService Service { get; private set; }
        public AppService AppService { get; private set; }

        public void Inject(Resolver resolver)
        {
            InjectCount++;
            Service = resolver.ResolveOptional<ContextService>();
            AppService = resolver.ResolveOptional<AppService>();
        }
    }

    public class AppService { }
}
