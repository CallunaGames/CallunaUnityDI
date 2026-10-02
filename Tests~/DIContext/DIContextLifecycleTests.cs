using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Calluna.DI.Tests
{
    public class DIContextLifecycleTests
    {
        private BasicDIContext _context;
        private DIContext Context => _context;
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _context = new Bootstrapper().Resolver.Resolve<BasicDIContext>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            _created.Clear();
        }

        // --- Dispose order and isolation ---

        [Test]
        public void Reset_DisposesInReverseCreationOrder()
        {
            List<string> log = new List<string>();
            BindLoggingDisposable("first", log);
            BindLoggingDisposable("second", log);
            _context.Resolver.Resolve<LoggingDisposable>("first");
            _context.Resolver.Resolve<LoggingDisposable>("second");

            Context.Reset();

            CollectionAssert.AreEqual(new[] { "second", "first" }, log);
        }

        [Test]
        public void Reset_DisposeThrows_DisposesTheOthersAndLogsTheException()
        {
            List<string> log = new List<string>();
            BindLoggingDisposable("first", log);
            _context.Binder.BindToNewSelf<ThrowingDisposable>().AsSingle();
            _context.Resolver.Resolve<LoggingDisposable>("first");
            _context.Resolver.Resolve<ThrowingDisposable>();

            LogAssert.Expect(LogType.Exception, new Regex(nameof(ThrowingDisposable)));
            Context.Reset();

            CollectionAssert.AreEqual(new[] { "first" }, log);
        }

        // --- Per-request tracking ---

        [Test]
        public void Reset_PerRequestByDefault_CleansAndDisposesInstances()
        {
            _context.Binder.BindToNewSelf<TrackedObject>().PerRequest();
            TrackedObject instance = _context.Resolver.Resolve<TrackedObject>();

            Context.Reset();

            Assert.IsTrue(instance.Cleaned);
            Assert.IsTrue(instance.Disposed);
        }

        [Test]
        public void Reset_PerRequestTracked_CleansAndDisposesInstances()
        {
            _context.Binder.BindToNewSelf<TrackedObject>().PerRequest().Tracked();
            TrackedObject instance = _context.Resolver.Resolve<TrackedObject>();

            Context.Reset();

            Assert.IsTrue(instance.Cleaned);
            Assert.IsTrue(instance.Disposed);
        }

        [Test]
        public void Reset_PerRequestUntracked_LeavesInstancesToTheRequester()
        {
            _context.Binder.BindToNewSelf<TrackedObject>().PerRequest().Untracked();
            TrackedObject instance = _context.Resolver.Resolve<TrackedObject>();

            Context.Reset();

            Assert.IsFalse(instance.Cleaned);
            Assert.IsFalse(instance.Disposed);
        }

        [Test]
        public void Resolve_PerRequestUntracked_StillInjectsAndInitializes()
        {
            _context.Binder.BindToNewSelf<TrackedObject>().PerRequest().Untracked();
            TrackedObject instance = _context.Resolver.Resolve<TrackedObject>();

            Assert.IsTrue(instance.Injected);
            Assert.IsTrue(instance.Initialized);
        }

        // --- Destroyed objects ---

        [Test]
        public void GameObjectsContainerDestroy_SkipsAlreadyDestroyedObjects()
        {
            GameObjectsContainer container = new GameObjectsContainer(new GameObjectContextsReseter());
            GameObject gameObject = new GameObject("Destroyed");
            container.Add(gameObject);
            Object.DestroyImmediate(gameObject);

            Assert.DoesNotThrow(() => container.Destroy());
        }

        [Test]
        public void ObjectsContainerDestroy_SkipsAlreadyDestroyedObjects()
        {
            ObjectsContainer container = new ObjectsContainer();
            GameObject gameObject = new GameObject("Destroyed");
            container.Add(gameObject.AddComponent<InitializableComponent>());
            Object.DestroyImmediate(gameObject);

            Assert.DoesNotThrow(() => container.Destroy());
        }

        // --- Prefab instances ---

        [Test]
        public void Resolve_PrefabInstanceAfterPostInit_InitializesTheWholeHierarchy()
        {
            InitializableComponent prefab = CreateHierarchy();
            _context.Binder.BindComponent<InitializableComponent>().FromNewPrefabInstance(prefab);
            _context.PostInit();

            InitializableComponent instance = _context.Resolver.Resolve<InitializableComponent>();
            _created.Add(instance.gameObject);

            Assert.AreEqual(1, instance.InitializeCount);
            InitializableComponent child = instance.transform.GetChild(0).GetComponent<InitializableComponent>();
            Assert.AreEqual(1, child.InitializeCount);
        }

        [Test]
        public void Resolve_PrefabInstanceBeforePostInit_LeavesInitializationToTheTraversers()
        {
            InitializableComponent prefab = CreateHierarchy();
            _context.Binder.BindComponent<InitializableComponent>().FromNewPrefabInstance(prefab);

            InitializableComponent instance = _context.Resolver.Resolve<InitializableComponent>();
            _created.Add(instance.gameObject);

            Assert.AreEqual(0, instance.InitializeCount);
        }

        [Test]
        public void Resolve_ComponentOnNewGameObjectWithoutName_NamesTheObjectAfterTheType()
        {
            _context.Binder.BindComponent<InitializableComponent>().FromNewComponentOnNewGameObject();

            InitializableComponent instance = _context.Resolver.Resolve<InitializableComponent>();
            _created.Add(instance.gameObject);

            Assert.AreEqual(nameof(InitializableComponent) + "Object", instance.gameObject.name);
        }

        private void BindLoggingDisposable(string name, List<string> log)
        {
            _context.Binder.Bind<LoggingDisposable>(name)
                .ToNew<LoggingDisposable>()
                .WithArgument(log)
                .WithArgument(name)
                .AsSingle();
        }

        private InitializableComponent CreateHierarchy()
        {
            GameObject root = new GameObject("Prefab");
            GameObject child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            child.AddComponent<InitializableComponent>();
            _created.Add(root);
            return root.AddComponent<InitializableComponent>();
        }

        private class LoggingDisposable : IDisposable, Injectable
        {
            private List<string> _log;
            private string _name;

            public void Inject(Resolver resolver)
            {
                _log = resolver.Resolve<List<string>>();
                _name = resolver.Resolve<string>();
            }

            public void Dispose() => _log.Add(_name);
        }

        private class ThrowingDisposable : IDisposable
        {
            public void Dispose() => throw new InvalidOperationException(nameof(ThrowingDisposable));
        }

        private class TrackedObject : Injectable, Initializable, Cleanable, IDisposable
        {
            public bool Injected { get; private set; }
            public bool Initialized { get; private set; }
            public bool Cleaned { get; private set; }
            public bool Disposed { get; private set; }

            public void Inject(Resolver resolver) => Injected = true;
            public void Initialize() => Initialized = true;
            public void Clean() => Cleaned = true;
            public void Dispose() => Disposed = true;
        }
    }
}
