using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Calluna.DI.Tests
{
    // PlayMode only: MonoPoolCache sets up its hooks in Awake, which doesn't run in edit mode.
    public class MonoPoolTests
    {
        private TestApp _app;
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            if (!Application.isPlaying)
                Assert.Ignore("Pools need play mode (MonoPoolCache.Awake).");
            _app = new TestApp();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            _created.Clear();
            _app?.Dispose();
        }

        // --- MonoPool<TItem> ---

        [Test]
        public void Request_EmptyPool_CreatesAnInjectedAndInitializedItem()
        {
            Pool<PoolItem> pool = BindPool();

            PoolItem item = Track(pool.Request());

            Assert.AreEqual(1, item.InjectCount);
            Assert.AreEqual(1, item.InitializeCount);
            Assert.IsTrue(item.gameObject.activeSelf);
        }

        [Test]
        public void Return_CleansAndDeactivatesTheItem()
        {
            Pool<PoolItem> pool = BindPool();
            PoolItem item = Track(pool.Request());

            pool.Return(item);

            Assert.AreEqual(1, item.CleanCount);
            Assert.IsFalse(item.gameObject.activeSelf);
        }

        [Test]
        public void Request_AfterReturn_ReusesTheItemAndInjectsAndInitializesItAgain()
        {
            Pool<PoolItem> pool = BindPool();
            PoolItem item = Track(pool.Request());
            pool.Return(item);

            PoolItem reused = pool.Request();

            Assert.AreSame(item, reused);
            Assert.AreEqual(2, reused.InjectCount);
            Assert.AreEqual(2, reused.InitializeCount);
            Assert.IsTrue(reused.gameObject.activeSelf);
        }

        [Test]
        public void Request_WithParent_ParentsTheReusedItem()
        {
            Pool<PoolItem> pool = BindPool();
            PoolItem item = Track(pool.Request());
            pool.Return(item);
            Transform parent = Track(new GameObject("Parent")).transform;

            PoolItem reused = ((Pool<PoolItem, PrefabInstantiationArguments>)pool)
                .Request(new PrefabInstantiationArguments { Parent = parent });

            Assert.AreSame(parent, reused.transform.parent);
        }

        [Test]
        public void Request_AfterWarmUp_InjectsTheBareItemOnlyOnce()
        {
            Pool<PoolItem> pool = BindPool();
            ((WarmablePool<PoolItem>)pool).WarmUp(1);

            PoolItem item = Track(pool.Request());

            Assert.AreEqual(1, item.InjectCount);
            Assert.AreEqual(1, item.InitializeCount);
        }

        [Test]
        public void Request_StoredItemWasDestroyed_CreatesANewItem()
        {
            Pool<PoolItem> pool = BindPool();
            PoolItem item = pool.Request();
            pool.Return(item);
            Object.DestroyImmediate(item.gameObject);

            PoolItem next = Track(pool.Request());

            Assert.IsTrue(next != null);
            Assert.AreEqual(1, next.InjectCount);
        }

        // --- MonoPoolCache ---

        [Test]
        public void CacheHasObjects_DoesNotRemoveDestroyedObjects()
        {
            MonoPoolCache cache = _app.Resolver.Resolve<MonoPoolCache>();
            PoolItem item = CreatePrefab<PoolItem>();
            cache.Store(1, item);
            Object.DestroyImmediate(item.gameObject);

            Assert.IsTrue(cache.HasObjects(1));
        }

        [Test]
        public void CacheRemoveDestroyed_RemovesTheDestroyedObjectsToBeTakenNext()
        {
            MonoPoolCache cache = _app.Resolver.Resolve<MonoPoolCache>();
            PoolItem kept = CreatePrefab<PoolItem>();
            PoolItem destroyed = CreatePrefab<PoolItem>();
            cache.Store(1, kept);
            cache.Store(1, destroyed);
            Object.DestroyImmediate(destroyed.gameObject);

            cache.RemoveDestroyed(1);

            Assert.IsTrue(cache.HasObjects(1));
            Assert.AreSame(kept, cache.Take<PoolItem>(1));
            Assert.IsFalse(cache.HasObjects(1));
        }

        // --- MonoPool<TItem, TArg> ---

        [Test]
        public void Request_WithArgument_InjectsTheArgument()
        {
            Pool<PoolItem, PoolItemArgument> pool = BindArgumentPool();
            PoolItemArgument argument = new PoolItemArgument("a");

            PoolItem item = Track(pool.Request(argument));

            Assert.AreSame(argument, item.Argument);
        }

        [Test]
        public void Request_ReusedItemsKeepingTheirResolver_ResolveTheirOwnArgument()
        {
            Pool<PoolItem, PoolItemArgument> pool = BindArgumentPool();
            ((WarmablePool<PoolItem>)pool).WarmUp(2);
            PoolItemArgument first = new PoolItemArgument("first");
            PoolItemArgument second = new PoolItemArgument("second");

            PoolItem firstItem = Track(pool.Request(first));
            PoolItem secondItem = Track(pool.Request(second));

            Assert.AreSame(first, firstItem.ResolveArgumentLater());
            Assert.AreSame(second, secondItem.ResolveArgumentLater());
        }

        // --- AbstractMonoPool ---

        [Test]
        public void AbstractPoolRequest_ReturnsAnItemOfTheRequestedPrefab()
        {
            Pool<IdPoolItem, int> pool = BindAbstractPool();

            IdPoolItem first = Track(pool.Request(1));
            IdPoolItem second = Track(pool.Request(2));

            Assert.AreEqual(1, first.ItemId);
            Assert.AreEqual(2, second.ItemId);
            Assert.AreEqual(1, first.InjectCount);
        }

        [Test]
        public void AbstractPoolRequest_AfterReturn_ReusesTheItemOfTheSamePrefab()
        {
            Pool<IdPoolItem, int> pool = BindAbstractPool();
            IdPoolItem first = Track(pool.Request(1));
            pool.Return(first);

            IdPoolItem other = Track(pool.Request(2));
            IdPoolItem reused = pool.Request(1);

            Assert.AreNotSame(first, other);
            Assert.AreSame(first, reused);
            Assert.AreEqual(2, reused.InjectCount);
        }

        private Pool<PoolItem> BindPool()
        {
            PoolItem prefab = CreatePrefab<PoolItem>();
            _app.Binder.Bind<Factory<PoolItem>>()
                .And<Factory<PoolItem, PrefabInstantiationArguments>>()
                .ToNew<PrefabFactory<PoolItem>>()
                .WithArgument(prefab);
            _app.Binder.Bind<Pool<PoolItem>>()
                .And<Pool<PoolItem, PrefabInstantiationArguments>>()
                .And<WarmablePool<PoolItem>>()
                .ToNew<MonoPool<PoolItem>>()
                .WithArgument(prefab)
                .AsSingle();
            return _app.Resolver.Resolve<Pool<PoolItem>>();
        }

        private Pool<PoolItem, PoolItemArgument> BindArgumentPool()
        {
            PoolItem prefab = CreatePrefab<PoolItem>();
            _app.Binder.Bind<Factory<PoolItem, PoolItemArgument>>()
                .And<Factory<PoolItem, PoolItemArgument, PrefabInstantiationArguments>>()
                .ToNew<PrefabFactory<PoolItem, PoolItemArgument>>()
                .WithArgument(prefab);
            _app.Binder.Bind<Pool<PoolItem, PoolItemArgument>>()
                .And<Pool<PoolItem, PoolItemArgument, PrefabInstantiationArguments>>()
                .And<WarmablePool<PoolItem>>()
                .ToNew<MonoPool<PoolItem, PoolItemArgument>>()
                .WithArgument(prefab)
                .AsSingle();
            return _app.Resolver.Resolve<Pool<PoolItem, PoolItemArgument>>();
        }

        private Pool<IdPoolItem, int> BindAbstractPool()
        {
            IdPoolItem first = CreatePrefab<IdPoolItem>();
            first.Id = 1;
            IdPoolItem second = CreatePrefab<IdPoolItem>();
            second.Id = 2;
            AbstractMonoPool<IdPoolItem, int> pool = new AbstractMonoPool<IdPoolItem, int>();
            pool.AddPrefab(first);
            pool.AddPrefab(second);
            _app.Binder.BindToNewSelf<PrefabFactory>();
            _app.Binder.Bind<Pool<IdPoolItem, int>>().ToInstance(pool).WithInjection();
            return _app.Resolver.Resolve<Pool<IdPoolItem, int>>();
        }

        // A scene object as template: instantiating it works like instantiating a prefab asset.
        private TComponent CreatePrefab<TComponent>() where TComponent : Component
        {
            GameObject prefab = Track(new GameObject(typeof(TComponent).Name + "Prefab"));
            return prefab.AddComponent<TComponent>();
        }

        private T Track<T>(T obj) where T : Object
        {
            _created.Add(obj is Component component ? component.gameObject : obj);
            return obj;
        }
    }
}
