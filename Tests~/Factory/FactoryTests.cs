using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Calluna.DI.Tests
{
    public class FactoryTests
    {
        private TestApp _app;
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _app = new TestApp();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            _created.Clear();
            _app.Dispose();
        }

        // --- BasicFactory ---

        [Test]
        public void BasicFactoryCreate_InjectsAndInitializesANewInstance()
        {
            _app.Binder.Bind<Factory<Product>>().ToNew<BasicFactory<Product>>();
            Factory<Product> factory = _app.Resolver.Resolve<Factory<Product>>();

            Product first = factory.Create();
            Product second = factory.Create();

            Assert.AreNotSame(first, second);
            Assert.IsTrue(first.Injected);
            Assert.IsTrue(first.Initialized);
        }

        [Test]
        public void BasicFactoryCreate_WithArgument_InjectsTheArgument()
        {
            _app.Binder.Bind<Factory<Product, string>>().ToNew<BasicFactory<Product, string>>();
            Factory<Product, string> factory = _app.Resolver.Resolve<Factory<Product, string>>();

            Product product = factory.Create("argument");

            Assert.AreEqual("argument", product.Argument);
        }

        // --- PrefabFactory ---

        [Test]
        public void PrefabFactoryCreate_InjectsAndInitializesTheWholeHierarchy()
        {
            PoolItem prefab = CreatePrefabWithChild();
            _app.Binder.Bind<Factory<PoolItem>>().ToNew<PrefabFactory<PoolItem>>().WithArgument(prefab);

            PoolItem instance = Track(_app.Resolver.Resolve<Factory<PoolItem>>().Create());

            PoolItem child = instance.transform.GetChild(0).GetComponent<PoolItem>();
            Assert.AreEqual(1, instance.InjectCount);
            Assert.AreEqual(1, instance.InitializeCount);
            Assert.AreEqual(1, child.InjectCount);
            Assert.AreEqual(1, child.InitializeCount);
        }

        [Test]
        public void PrefabFactoryCreate_WithInstantiationArguments_ParentsAndPlacesTheInstance()
        {
            PoolItem prefab = CreatePrefabWithChild();
            _app.Binder.Bind<Factory<PoolItem, PrefabInstantiationArguments>>()
                .ToNew<PrefabFactory<PoolItem>>()
                .WithArgument(prefab);
            Transform parent = Track(new GameObject("Parent")).transform;
            Vector3 position = new Vector3(1, 2, 3);

            PoolItem instance = _app.Resolver.Resolve<Factory<PoolItem, PrefabInstantiationArguments>>()
                .Create(new PrefabInstantiationArguments { Parent = parent, Position = position });

            Assert.AreSame(parent, instance.transform.parent);
            Assert.AreEqual(position, instance.transform.position);
        }

        [Test]
        public void PrefabFactoryCreate_WithArgument_InjectsTheArgumentIntoTheHierarchy()
        {
            PoolItem prefab = CreatePrefabWithChild();
            _app.Binder.Bind<Factory<PoolItem, PoolItemArgument>>()
                .ToNew<PrefabFactory<PoolItem, PoolItemArgument>>()
                .WithArgument(prefab);
            PoolItemArgument argument = new PoolItemArgument("argument");

            PoolItem instance = Track(_app.Resolver.Resolve<Factory<PoolItem, PoolItemArgument>>().Create(argument));

            Assert.AreSame(argument, instance.Argument);
            Assert.AreSame(argument, instance.transform.GetChild(0).GetComponent<PoolItem>().Argument);
        }

        [Test]
        public void UntypedPrefabFactoryCreate_CreatesAnInstanceOfTheGivenPrefab()
        {
            PoolItem prefab = CreatePrefabWithChild();
            _app.Binder.BindToNewSelf<PrefabFactory>();
            PoolItemArgument argument = new PoolItemArgument("argument");

            PoolItem instance = Track(_app.Resolver.Resolve<PrefabFactory>().Create(prefab, argument));

            Assert.AreNotSame(prefab, instance);
            Assert.AreSame(argument, instance.Argument);
            Assert.AreEqual(1, instance.InitializeCount);
        }

        // --- ScopedFactory ---

        [Test]
        public void ScopedFactoryCreate_ResolvesTheProductFromItsOwnScope()
        {
            _app.Binder.Bind<Factory<Product>>().ToNew<ProductFactory>();
            Factory<Product> factory = _app.Resolver.Resolve<Factory<Product>>();

            Product first = factory.Create();
            Product second = factory.Create();

            Assert.IsNotNull(first.Helper);
            Assert.AreNotSame(first.Helper, second.Helper, "Each creation gets its own scope.");
            Assert.IsNull(_app.Resolver.ResolveOptional<ScopeHelper>(), "Scope bindings stay in the scope.");
        }

        [Test]
        public void ScopedFactoryCreate_WithArgument_BindsTheArgumentInTheScope()
        {
            _app.Binder.Bind<Factory<Product, string>>().ToNew<ProductWithArgumentFactory>();

            Product product = _app.Resolver.Resolve<Factory<Product, string>>().Create("argument");

            Assert.AreEqual("argument", product.Argument);
        }

        [Test]
        public void ScopedFactoryCreate_MovesTheScopesInstancesToTheParent_WhichCleansThemOnReset()
        {
            _app.Binder.Bind<Factory<Product>>().ToNew<ProductFactory>();
            Product product = _app.Resolver.Resolve<Factory<Product>>().Create();

            ((DIContext)_app.Context).Reset();

            Assert.IsTrue(product.Helper.Cleaned);
        }

        private PoolItem CreatePrefabWithChild()
        {
            GameObject root = Track(new GameObject("Prefab"));
            GameObject child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            child.AddComponent<PoolItem>();
            return root.AddComponent<PoolItem>();
        }

        private T Track<T>(T obj) where T : Object
        {
            _created.Add(obj is Component component ? component.gameObject : obj);
            return obj;
        }

        private class Product : Injectable, Initializable
        {
            public bool Injected { get; private set; }
            public bool Initialized { get; private set; }
            public string Argument { get; private set; }
            public ScopeHelper Helper { get; private set; }

            public void Inject(Resolver resolver)
            {
                Injected = true;
                Argument = resolver.ResolveOptional<string>();
                Helper = resolver.ResolveOptional<ScopeHelper>();
            }

            public void Initialize() => Initialized = true;
        }

        private class ScopeHelper : Cleanable
        {
            public bool Cleaned { get; private set; }

            public void Clean() => Cleaned = true;
        }

        private class ProductFactory : ScopedFactory<Product>
        {
            protected override void InitScope(Resolver resolver, Binder binder)
            {
                binder.BindToNewSelf<Product>();
                binder.BindToNewSelf<ScopeHelper>().AsSingle();
            }
        }

        private class ProductWithArgumentFactory : ScopedFactory<Product, string>
        {
            protected override void InitScope(Resolver resolver, Binder binder)
            {
                binder.BindToNewSelf<Product>();
            }
        }
    }
}
