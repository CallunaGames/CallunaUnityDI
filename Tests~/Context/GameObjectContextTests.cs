using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Calluna.DI.Tests
{
    public class GameObjectContextTests
    {
        private TestApp _app;
        private GameObjectInjector _injector;

        [SetUp]
        public void SetUp()
        {
            _app = new TestApp();
            _injector = new GameObjectInjector();
        }

        [TearDown]
        public void TearDown()
        {
            _app.Dispose();
        }

        [Test]
        public void Inject_HierarchyWithContext_ResolvesTheContextsBindings()
        {
            GameObject root = CreateContext("Context");
            ServiceConsumer consumer = CreateConsumer(root.transform);

            _injector.InjectIntoContextHierarchy(root.transform, _app.Resolver);

            Assert.IsNotNull(consumer.Service);
        }

        [Test]
        public void Inject_HierarchyWithContext_ResolvesTheParentsBindings()
        {
            AppService appService = new AppService();
            _app.Binder.BindInstance(appService);
            GameObject root = CreateContext("Context");
            ServiceConsumer consumer = CreateConsumer(root.transform);

            _injector.InjectIntoContextHierarchy(root.transform, _app.Resolver);

            Assert.AreSame(appService, consumer.AppService);
        }

        [Test]
        public void Inject_HierarchyWithContext_DoesNotExposeTheContextsBindingsToTheParent()
        {
            GameObject root = CreateContext("Context");

            _injector.InjectIntoContextHierarchy(root.transform, _app.Resolver);

            Assert.IsNull(_app.Resolver.ResolveOptional<ContextService>());
        }

        [Test]
        public void Inject_NestedContext_ChildrenResolveTheirClosestContext()
        {
            GameObject outer = CreateContext("Outer");
            ServiceConsumer outerConsumer = CreateConsumer(outer.transform);
            GameObject inner = CreateContext("Inner");
            inner.transform.SetParent(outer.transform);
            ServiceConsumer innerConsumer = CreateConsumer(inner.transform);

            _injector.InjectIntoContextHierarchy(outer.transform, _app.Resolver);

            Assert.IsNotNull(outerConsumer.Service);
            Assert.IsNotNull(innerConsumer.Service);
            Assert.AreNotSame(outerConsumer.Service, innerConsumer.Service);
            Assert.AreEqual(1, innerConsumer.InjectCount);
        }

        [Test]
        public void Init_Twice_Throws()
        {
            GameObject root = CreateContext("Context");
            _injector.InjectIntoContextHierarchy(root.transform, _app.Resolver);

            Assert.Throws<GameObjectContextAlreadyIsInitializedException>(
                () => _injector.InjectIntoContextHierarchy(root.transform, _app.Resolver));
        }

        [Test]
        public void Reset_CleansTheContextsInstances()
        {
            GameObject root = CreateContext("Context");
            ServiceConsumer consumer = CreateConsumer(root.transform);
            _injector.InjectIntoContextHierarchy(root.transform, _app.Resolver);

            ((Context)root.GetComponent<GameObjectContext>()).Reset();

            Assert.IsTrue(consumer.Service.Cleaned);
        }

        // Pooled objects with a context are reset on return and initialized again on the next take.
        [Test]
        public void Init_AfterReset_InstallsAndInjectsAgainWithNewInstances()
        {
            GameObject root = CreateContext("Context");
            ServiceConsumer consumer = CreateConsumer(root.transform);
            _injector.InjectIntoContextHierarchy(root.transform, _app.Resolver);
            ContextService firstService = consumer.Service;
            ((Context)root.GetComponent<GameObjectContext>()).Reset();

            _injector.InjectIntoContextHierarchy(root.transform, _app.Resolver);

            Assert.AreEqual(2, consumer.InjectCount);
            Assert.IsNotNull(consumer.Service);
            Assert.AreNotSame(firstService, consumer.Service);
            Assert.IsFalse(consumer.Service.Cleaned);
        }

        private GameObject CreateContext(string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(_app.Root.transform);
            GameObjectContext context = gameObject.AddComponent<GameObjectContext>();
            ContextServiceInstaller installer = gameObject.AddComponent<ContextServiceInstaller>();
            typeof(MonoContext)
                .GetField("_monoInstallers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(context, new MonoInstaller[] { installer });
            return gameObject;
        }

        private static ServiceConsumer CreateConsumer(Transform parent)
        {
            GameObject gameObject = new GameObject(nameof(ServiceConsumer));
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<ServiceConsumer>();
        }
    }
}
