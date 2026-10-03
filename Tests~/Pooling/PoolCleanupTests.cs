using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Calluna.DI.Tests
{
    /// <summary>
    /// Stored pool items are shared by prefab in the MonoPoolCache. Pools register as users of their
    /// prefabs and unregister when disposed; a scene context's reset destroys the items no live pool uses.
    /// PlayMode only: MonoPoolCache sets up its hooks in Awake.
    /// </summary>
    public class PoolCleanupTests
    {
        private static readonly FieldInfo s_installersField =
            typeof(MonoContext).GetField("_monoInstallers", BindingFlags.Instance | BindingFlags.NonPublic);

        private TestApp _app;
        private readonly List<Object> _created = new List<Object>();
        private readonly List<Scene> _scenes = new List<Scene>();

        [SetUp]
        public void SetUp()
        {
            if (!Application.isPlaying)
                Assert.Ignore("Pools need play mode (MonoPoolCache.Awake).");
            _app = new TestApp();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AppContext appContext = Object.FindFirstObjectByType<AppContext>();
            if (appContext != null)
            {
                appContext.Quit();
                Object.DestroyImmediate(appContext.gameObject);
            }
            foreach (Object obj in _created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            _created.Clear();
            _app?.Dispose();
            foreach (Scene scene in _scenes)
                if (scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
            _scenes.Clear();
        }

        // --- Cache and pools ---

        [UnityTest]
        public IEnumerator DestroyUnused_AfterThePoolWasDisposed_DestroysItsStoredItems()
        {
            PoolItem prefab = CreatePrefab();
            Pool<PoolItem> pool = CreatePool(prefab);
            PoolItem item = pool.Request();
            pool.Return(item);

            ((IDisposable)pool).Dispose();
            Cache.DestroyUnused();

            Assert.IsFalse(Cache.HasObjects(prefab.GetHashCode()));
            yield return null;
            Assert.IsTrue(item == null, "The stored item should be destroyed.");
        }

        [Test]
        public void DestroyUnused_ThePoolIsAlive_KeepsItsStoredItems()
        {
            PoolItem prefab = CreatePrefab();
            Pool<PoolItem> pool = CreatePool(prefab);
            pool.Return(Track(pool.Request()));

            Cache.DestroyUnused();

            Assert.IsTrue(Cache.HasObjects(prefab.GetHashCode()));
        }

        [Test]
        public void DestroyUnused_AnotherPoolStillUsesThePrefab_KeepsTheStoredItems()
        {
            PoolItem prefab = CreatePrefab();
            Pool<PoolItem> disposed = CreatePool(prefab);
            CreatePool(prefab);
            disposed.Return(Track(disposed.Request()));

            ((IDisposable)disposed).Dispose();
            ((IDisposable)disposed).Dispose(); // twice must not unregister the other pool
            Cache.DestroyUnused();

            Assert.IsTrue(Cache.HasObjects(prefab.GetHashCode()));
        }

        [Test]
        public void DestroyUnused_AbstractPoolDisposed_DestroysTheItemsOfAllItsPrefabs()
        {
            IdPoolItem first = CreatePrefab<IdPoolItem>();
            first.Id = 1;
            IdPoolItem second = CreatePrefab<IdPoolItem>();
            second.Id = 2;
            AbstractMonoPool<IdPoolItem, int> pool = new AbstractMonoPool<IdPoolItem, int>();
            pool.AddPrefab(first);
            pool.AddPrefab(second);
            _app.Binder.BindToNewSelf<PrefabFactory>();
            ((Injectable)pool).Inject(_app.Resolver);
            pool.Return(Track(pool.Request(1)));
            pool.Return(Track(pool.Request(2)));

            pool.Dispose();
            Cache.DestroyUnused();

            Assert.IsFalse(Cache.HasObjects(first.GetHashCode()));
            Assert.IsFalse(Cache.HasObjects(second.GetHashCode()));
        }

        // --- Contexts ---

        [Test]
        public void SceneContextReset_DestroysTheStoredItemsOfItsPools()
        {
            PoolItem prefab = CreatePrefab();
            GameObject sceneContext = CreateSceneContext(prefab);
            Pool<PoolItem> pool = sceneContext.GetComponent<SceneContext>().GetResolver().Resolve<Pool<PoolItem>>();
            pool.Return(pool.Request());
            MonoPoolCache cache = AppCache();
            Assert.IsTrue(cache.HasObjects(prefab.GetHashCode()));

            ((Context)sceneContext.GetComponent<SceneContext>()).Reset();

            Assert.IsFalse(cache.HasObjects(prefab.GetHashCode()));
        }

        // A pooled object with its own context is reset on every return; the next pool created for it
        // takes the stored items again.
        [Test]
        public void GameObjectContextReset_KeepsTheStoredItemsUntilTheSceneContextResets()
        {
            PoolItem prefab = CreatePrefab();
            GameObject sceneContext = CreateSceneContext(null, out Scene scene);
            GameObject gameObjectContextObject = new GameObject("PooledObjectWithContext");
            SceneManager.MoveGameObjectToScene(gameObjectContextObject, scene);
            GameObjectContext gameObjectContext = gameObjectContextObject.AddComponent<GameObjectContext>();
            s_installersField.SetValue(gameObjectContext, new MonoInstaller[] { CreateInstaller(gameObjectContextObject, prefab) });
            sceneContext.SetActive(true);

            Pool<PoolItem> pool = gameObjectContext.GetResolver().Resolve<Pool<PoolItem>>();
            pool.Return(pool.Request());
            MonoPoolCache cache = AppCache();
            ((Context)gameObjectContext).Reset();

            Assert.IsTrue(cache.HasObjects(prefab.GetHashCode()), "A GameObjectContext's reset must keep the items.");

            ((Context)sceneContext.GetComponent<SceneContext>()).Reset();

            Assert.IsFalse(cache.HasObjects(prefab.GetHashCode()));
        }

        private MonoPoolCache Cache => _app.Resolver.Resolve<MonoPoolCache>();

        private static MonoPoolCache AppCache() =>
            Object.FindFirstObjectByType<AppContext>().GetResolver().Resolve<MonoPoolCache>();

        // A pool in its own child context of the test app, like a pool of a scene.
        private Pool<PoolItem> CreatePool(PoolItem prefab)
        {
            ChildDIContext context = _app.Resolver.Resolve<Factory<ChildDIContext, Resolver>>().Create(_app.Resolver);
            PoolTestInstaller.Bind(context.Binder, prefab);
            return context.Resolver.Resolve<Pool<PoolItem>>();
        }

        private GameObject CreateSceneContext(PoolItem prefab)
        {
            GameObject contextObject = CreateSceneContext(prefab, out _);
            contextObject.SetActive(true);
            return contextObject;
        }

        // Returns the inactive scene context object; activating it initializes the context.
        private GameObject CreateSceneContext(PoolItem prefab, out Scene scene)
        {
            scene = SceneManager.CreateScene($"{nameof(PoolCleanupTests)}-{Guid.NewGuid():N}");
            _scenes.Add(scene);
            GameObject contextObject = new GameObject("SceneContext");
            contextObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(contextObject, scene);
            SceneContext context = contextObject.AddComponent<SceneContext>();
            s_installersField.SetValue(context, prefab == null
                ? new MonoInstaller[0]
                : new MonoInstaller[] { CreateInstaller(contextObject, prefab) });
            return contextObject;
        }

        private static PoolTestInstaller CreateInstaller(GameObject gameObject, PoolItem prefab)
        {
            PoolTestInstaller installer = gameObject.AddComponent<PoolTestInstaller>();
            installer.Prefab = prefab;
            return installer;
        }

        private PoolItem CreatePrefab() => CreatePrefab<PoolItem>();

        // A scene object as template, outside the test scenes so their contexts don't inject it.
        private TComponent CreatePrefab<TComponent>() where TComponent : Component
        {
            GameObject prefab = new GameObject(typeof(TComponent).Name + "Prefab");
            _created.Add(prefab);
            return prefab.AddComponent<TComponent>();
        }

        private T Track<T>(T obj) where T : Component
        {
            _created.Add(obj.gameObject);
            return obj;
        }
    }
}
