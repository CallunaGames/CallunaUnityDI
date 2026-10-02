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

        // --- Clean order ---

        [Test]
        public void Reset_CleansInReverseCreationOrder_DependentsBeforeTheirDependencies()
        {
            List<string> log = new List<string>();
            _context.Binder.BindInstance(log);
            _context.Binder.BindToNewSelf<CleanLogDependency>().AsSingle();
            _context.Binder.BindToNewSelf<CleanLogDependent>().AsSingle();
            _context.Resolver.Resolve<CleanLogDependent>();

            Context.Reset();

            CollectionAssert.AreEqual(new[] { nameof(CleanLogDependent), nameof(CleanLogDependency) }, log);
        }

        // --- Quit handlers ---

        [Test]
        public void HandleQuit_CallsQuitHandlersInReverseCreationOrder()
        {
            List<string> log = new List<string>();
            BindLoggingQuitHandler("first", log);
            BindLoggingQuitHandler("second", log);
            _context.Resolver.Resolve<LoggingQuitHandler>("first");
            _context.Resolver.Resolve<LoggingQuitHandler>("second");

            Context.HandleQuit();

            CollectionAssert.AreEqual(new[] { "second", "first" }, log);
        }

        [Test]
        public void HandleQuit_HandlerThrows_CallsTheOthersAndLogsTheException()
        {
            List<string> log = new List<string>();
            BindLoggingQuitHandler("first", log);
            _context.Binder.BindToNewSelf<ThrowingQuitHandler>().AsSingle();
            _context.Resolver.Resolve<LoggingQuitHandler>("first");
            _context.Resolver.Resolve<ThrowingQuitHandler>();

            LogAssert.Expect(LogType.Exception, new Regex(nameof(ThrowingQuitHandler)));
            Context.HandleQuit();

            CollectionAssert.AreEqual(new[] { "first" }, log);
        }

        [Test]
        public void HandleQuit_DoesNotResetTheContext()
        {
            _context.Binder.BindToNewSelf<TrackedObject>().AsSingle();
            TrackedObject instance = _context.Resolver.Resolve<TrackedObject>();

            Context.HandleQuit();

            Assert.IsTrue(instance.QuitHandled);
            Assert.IsFalse(instance.Cleaned);
            Assert.AreSame(instance, _context.Resolver.Resolve<TrackedObject>());
        }

        [Test]
        public void HandleQuit_PerRequestUntracked_IsNotCalled()
        {
            _context.Binder.BindToNewSelf<TrackedObject>().PerRequest().Untracked();
            TrackedObject instance = _context.Resolver.Resolve<TrackedObject>();

            Context.HandleQuit();

            Assert.IsFalse(instance.QuitHandled);
        }

        [Test]
        public void HandleQuit_FromInstance_IsNotCalled()
        {
            TrackedObject instance = new TrackedObject();
            _context.Binder.BindInstance(instance);
            _context.Resolver.Resolve<TrackedObject>();

            Context.HandleQuit();

            Assert.IsFalse(instance.QuitHandled);
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

        // The GameObject's other components and children belong to their own context and were injected
        // already - e.g. pooled items below the AppContext object, which hosts the QuitDetector.
        [Test]
        public void Resolve_NewComponentOnExistingGameObject_InjectsOnlyTheNewComponent()
        {
            GameObject target = new GameObject("Target");
            _created.Add(target);
            ServiceConsumer existing = target.AddComponent<ServiceConsumer>();
            GameObject child = new GameObject("Child");
            child.transform.SetParent(target.transform);
            ServiceConsumer existingChild = child.AddComponent<ServiceConsumer>();
            _context.Binder.BindComponent<PoolItem>().FromNewComponentOn(target);

            PoolItem added = _context.Resolver.Resolve<PoolItem>();

            Assert.AreEqual(1, added.InjectCount);
            Assert.AreEqual(0, existing.InjectCount);
            Assert.AreEqual(0, existingChild.InjectCount);
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

        private void BindLoggingQuitHandler(string name, List<string> log)
        {
            _context.Binder.Bind<LoggingQuitHandler>(name)
                .ToNew<LoggingQuitHandler>()
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

        private class LoggingQuitHandler : QuitHandler, Injectable
        {
            private List<string> _log;
            private string _name;

            public void Inject(Resolver resolver)
            {
                _log = resolver.Resolve<List<string>>();
                _name = resolver.Resolve<string>();
            }

            public void HandleQuit() => _log.Add(_name);
        }

        private class CleanLogDependency : Injectable, Cleanable
        {
            private List<string> _log;

            public void Inject(Resolver resolver) => _log = resolver.Resolve<List<string>>();

            public void Clean() => _log.Add(nameof(CleanLogDependency));
        }

        // Resolved first, but stored after its dependency, which it creates during its injection.
        private class CleanLogDependent : Injectable, Cleanable
        {
            private List<string> _log;

            public void Inject(Resolver resolver)
            {
                _log = resolver.Resolve<List<string>>();
                resolver.Resolve<CleanLogDependency>();
            }

            public void Clean() => _log.Add(nameof(CleanLogDependent));
        }

        private class ThrowingQuitHandler : QuitHandler
        {
            public void HandleQuit() => throw new InvalidOperationException(nameof(ThrowingQuitHandler));
        }

        private class TrackedObject : Injectable, Initializable, Cleanable, IDisposable, QuitHandler
        {
            public bool QuitHandled { get; private set; }
            public void HandleQuit() => QuitHandled = true;

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
