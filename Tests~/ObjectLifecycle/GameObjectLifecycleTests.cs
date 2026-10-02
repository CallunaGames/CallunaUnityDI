using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Calluna.DI.Tests
{
    public class GameObjectLifecycleTests
    {
        private readonly List<string> _log = new List<string>();
        private GameObject _root;

        // root
        // ├── a
        // │   └── a1
        // └── b
        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _root = Create("root", null);
            GameObject a = Create("a", _root.transform);
            Create("a1", a.transform);
            Create("b", _root.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Initializer_InitializesChildrenBeforeTheirParents()
        {
            new GameObjectInitializer().PerformActionOnHierarchy(_root.transform);

            CollectionAssert.AreEqual(
                new[] { "initialize a1", "initialize a", "initialize b", "initialize root" }, _log);
        }

        [Test]
        public void Cleaner_CleansParentsBeforeTheirChildren()
        {
            new GameObjectCleaner().PerformActionOnHierarchy(_root.transform);

            CollectionAssert.AreEqual(new[] { "clean root", "clean a", "clean a1", "clean b" }, _log);
        }

        [Test]
        public void Initializer_OneComponentThrows_LogsAndInitializesTheOthers()
        {
            Find("a").Throw = true;

            LogAssert.Expect(LogType.Exception, new Regex("a initialize failed"));
            new GameObjectInitializer().PerformActionOnHierarchy(_root.transform);

            CollectionAssert.AreEqual(new[] { "initialize a1", "initialize b", "initialize root" }, _log);
        }

        [Test]
        public void Injector_InjectsTheWholeHierarchyFromTheRoot()
        {
            new GameObjectInjector().InjectIntoHierarchy(_root.transform, new BasicInstanceResolver());

            CollectionAssert.AreEqual(new[] { "inject root", "inject a", "inject a1", "inject b" }, _log);
        }

        [Test]
        public void Injector_SkipsInstallers()
        {
            SpyMonoInstaller installer = _root.AddComponent<SpyMonoInstaller>();

            new GameObjectInjector().InjectIntoHierarchy(_root.transform, new BasicInstanceResolver());

            Assert.IsFalse(installer.Injected);
        }

        private LifecycleLogComponent Find(string objectName)
        {
            foreach (LifecycleLogComponent component in _root.GetComponentsInChildren<LifecycleLogComponent>())
                if (component.name == objectName)
                    return component;
            return null;
        }

        private GameObject Create(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent);
            gameObject.AddComponent<LifecycleLogComponent>().Log = _log;
            return gameObject;
        }
    }
}
