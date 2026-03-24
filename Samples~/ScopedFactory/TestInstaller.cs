using UnityEngine;

namespace Calluna.DI.Samples.ScopedFactory
{
    public class TestInstaller : MonoInstaller
    {
        [SerializeField] private int _numbersAmount = 5;
        [SerializeField] private Vector2Int _range = new Vector2Int(0, 100);
        
        public override void InstallBindings(Binder binder)
        {
            binder.Bind<Factory<Foo>>()
                .ToNew<FooFactory>()
                .WithArgument(_numbersAmount)
                .WithArgument(_range)
                .AsSingle();

            binder.BindToSelf<Foo>()
                .FromFactory()
                .WithoutInjection()
                .PerRequest();
            
            binder.Bind<Factory<Bar, Foo>>()
                .ToNew<BarFactory>()
                .AsSingle();
        }
    }
}