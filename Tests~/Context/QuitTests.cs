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
    /// Quitting with real scene contexts: a parent scene and a child scene depending on it (the child
    /// also holds a GameObjectContext). Each context binds a <see cref="QuitLogService"/>, each scene has
    /// a <see cref="QuitLogComponent"/>; both log their HandleQuit and Clean calls.
    /// </summary>
    public class QuitTests
    {
        private const string ParentId = "QuitTestsParent";

        private static readonly FieldInfo _idField =
            typeof(SceneContext).GetField("_iD", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _parentIdField =
            typeof(SceneContext).GetField("_parentContextID", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _installersField =
            typeof(MonoContext).GetField("_monoInstallers", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<string> _log = new List<string>();
        private readonly List<Scene> _scenes = new List<Scene>();
        private GameObject _parentContext;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _log.Clear();
            _scenes.Clear();
            _parentContext = CreateScene("parent", ParentId, string.Empty, withGameObjectContext: false);
            _parentContext.SetActive(true);
            GameObject childContext = CreateScene("child", string.Empty, ParentId, withGameObjectContext: true);
            childContext.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AppContext appContext = Object.FindFirstObjectByType<AppContext>();
            if (appContext != null)
            {
                // Resets every context in dependency order, so destroying them afterwards is safe.
                appContext.Quit();
                Object.DestroyImmediate(appContext.gameObject);
            }
            foreach (Scene scene in _scenes)
                if (scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
        }

        [Test]
        public void Quit_HandlesQuitInAllContextsBeforeResettingThem_ChildrenFirst()
        {
            Object.FindFirstObjectByType<AppContext>().Quit();

            CollectionAssert.AreEqual(new[]
            {
                "quit child-component", "quit child-go", "quit child", "quit parent-component", "quit parent",
                "clean child-component", "clean child-go", "clean child", "clean parent-component", "clean parent"
            }, _log);
        }

        [Test]
        public void Quit_CalledTwice_RunsOnce()
        {
            AppContext appContext = Object.FindFirstObjectByType<AppContext>();

            appContext.Quit();
            int count = _log.Count;
            appContext.Quit();

            Assert.AreEqual(count, _log.Count);
        }

        // Unity calls OnApplicationQuit on the contexts in no reliable order - a scene context must not
        // reset itself (e.g. the parent before its child) but leave quitting to the AppContext.
        [Test]
        public void SceneContextOnApplicationQuit_DoesNotResetTheContext()
        {
            _parentContext.SendMessage("OnApplicationQuit", SendMessageOptions.DontRequireReceiver);

            CollectionAssert.IsEmpty(_log);
            Assert.IsTrue(_parentContext.GetComponent<SceneContext>().IsInitialized);
        }

#pragma warning disable CS0618 // The obsolete QuitDetector must keep working until 2.0.0.
        [Test]
        public void Quit_FlagsTheQuitDetector()
        {
            AppContext appContext = Object.FindFirstObjectByType<AppContext>();
            QuitDetector quitDetector = appContext.GetResolver().Resolve<QuitDetector>();

            appContext.Quit();

            Assert.IsTrue(quitDetector.IsQuitting);
        }
#pragma warning restore CS0618

        // Returns the inactive scene context object; activating it initializes the scene context.
        private GameObject CreateScene(string name, string id, string parentId, bool withGameObjectContext)
        {
            Scene scene = SceneManager.CreateScene($"{nameof(QuitTests)}-{name}-{Guid.NewGuid():N}");
            _scenes.Add(scene);

            GameObject contextObject = new GameObject($"{name}-context");
            contextObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(contextObject, scene);
            SceneContext context = contextObject.AddComponent<SceneContext>();
            _idField.SetValue(context, id);
            _parentIdField.SetValue(context, parentId);
            _installersField.SetValue(context, new MonoInstaller[] { CreateInstaller(contextObject, name) });

            GameObject componentObject = new GameObject($"{name}-component");
            SceneManager.MoveGameObjectToScene(componentObject, scene);
            componentObject.AddComponent<QuitLogComponent>().Log = _log;

            if (withGameObjectContext)
            {
                GameObject gameObjectContextObject = new GameObject($"{name}-go");
                SceneManager.MoveGameObjectToScene(gameObjectContextObject, scene);
                GameObjectContext gameObjectContext = gameObjectContextObject.AddComponent<GameObjectContext>();
                _installersField.SetValue(gameObjectContext,
                    new MonoInstaller[] { CreateInstaller(gameObjectContextObject, $"{name}-go") });
            }
            return contextObject;
        }

        private QuitLogInstaller CreateInstaller(GameObject gameObject, string name)
        {
            QuitLogInstaller installer = gameObject.AddComponent<QuitLogInstaller>();
            installer.Log = _log;
            installer.Name = name;
            return installer;
        }
    }
}
