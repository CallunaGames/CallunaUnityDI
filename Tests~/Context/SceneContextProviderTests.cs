using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Calluna.DI.Tests
{
    /// <summary>
    /// Tests for SceneContextProvider.
    ///
    /// SceneContext is a MonoBehaviour with a private Awake that spins up the full DI stack.
    /// To avoid triggering Awake, every SceneContext is added to an inactive GameObject so
    /// Unity defers the Awake call indefinitely. The private _iD and _parentContextID fields
    /// are then set via reflection before the provider sees the context.
    /// </summary>
    public class SceneContextProviderTests
    {
        private SceneContextProvider _provider;
        private TestQuitDetector _quitDetector;
        private List<GameObject> _gameObjects;

        private static readonly FieldInfo s_idField =
            typeof(SceneContext).GetField("_iD", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_parentIdField =
            typeof(SceneContext).GetField("_parentContextID", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            _gameObjects = new List<GameObject>();
            _provider = new SceneContextProvider();

            GameObject quitGo = new GameObject("TestQuitDetector");
            _gameObjects.Add(quitGo);
            _quitDetector = quitGo.AddComponent<TestQuitDetector>();

            Mock<Resolver> resolverMock = new Mock<Resolver>();
            resolverMock.Setup(r => r.Resolve<QuitDetector>()).Returns(_quitDetector);
            _provider.Inject(resolverMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _gameObjects)
            {
                if (go != null)
                    GameObject.DestroyImmediate(go);
            }
            _gameObjects.Clear();
        }

        // --- Add ---

        [UnityTest]
        [Description("Add(SceneContext) with non-empty ID => Get returns that context?")]
        public IEnumerator Add_NonEmptyID_GetReturnsContext(
            [ValueSource(nameof(NonEmptyIDs))] string id)
        {
            SceneContext context = GivenASceneContext(id);
            yield return null;
            _provider.Add(context);
            SceneContext result = _provider.Get(id);
            Assert.AreSame(context, result);
        }

        [UnityTest]
        [Description("Add(SceneContext) with empty ID => does not register; subsequent Get throws GetException?")]
        public IEnumerator Add_EmptyID_DoesNotRegister(
            [ValueSource(nameof(EmptyIDs))] string emptyId)
        {
            SceneContext context = GivenASceneContext(emptyId);
            yield return null;
            _provider.Add(context);
            Assert.Throws<SceneContextProvider.GetException>(() => _provider.Get("any-id"));
        }

        [UnityTest]
        [Description("Add same non-empty ID twice => throws AlreadyAddedException?")]
        public IEnumerator Add_SameIDTwice_ThrowsAlreadyAddedException(
            [ValueSource(nameof(NonEmptyIDs))] string id)
        {
            SceneContext contextA = GivenASceneContext(id);
            SceneContext contextB = GivenASceneContext(id);
            yield return null;
            _provider.Add(contextA);
            Assert.Throws<SceneContextProvider.AlreadyAddedException>(() => _provider.Add(contextB));
        }

        // --- Remove ---

        [UnityTest]
        [Description("Remove after Add with no child dependency => subsequent Get throws GetException?")]
        public IEnumerator Remove_AfterAdd_NoChild_GetThrowsGetException(
            [ValueSource(nameof(NonEmptyIDs))] string id)
        {
            SceneContext context = GivenASceneContext(id);
            yield return null;
            _provider.Add(context);
            _provider.Remove(context);
            Assert.Throws<SceneContextProvider.GetException>(() => _provider.Get(id));
        }

        [UnityTest]
        [Description("Remove context not previously added => throws NotAddedException?")]
        public IEnumerator Remove_NotAdded_ThrowsNotAddedException(
            [ValueSource(nameof(NonEmptyIDs))] string id)
        {
            SceneContext context = GivenASceneContext(id);
            yield return null;
            Assert.Throws<SceneContextProvider.NotAddedException>(() => _provider.Remove(context));
        }

        [UnityTest]
        [Description("Remove parent while child is registered and app is not quitting => throws ActiveSceneDependenciesException?")]
        public IEnumerator Remove_ParentWithActiveChild_AppNotQuitting_ThrowsActiveDependenciesException()
        {
            SceneContext parent = GivenASceneContext("parent-scene");
            SceneContext child = GivenASceneContextWithParent("child-scene", "parent-scene");
            yield return null;
            _provider.Add(parent);
            _provider.Add(child);
            Assert.Throws<SceneContextProvider.ActiveSceneDependenciesException>(
                () => _provider.Remove(parent));
        }

        [UnityTest]
        [Description("Remove parent while child is registered but app IS quitting => does not throw?")]
        public IEnumerator Remove_ParentWithActiveChild_AppIsQuitting_DoesNotThrow()
        {
            SceneContext parent = GivenASceneContext("parent-scene-q");
            SceneContext child = GivenASceneContextWithParent("child-scene-q", "parent-scene-q");
            yield return null;
            _provider.Add(parent);
            _provider.Add(child);
            _quitDetector.SetQuitting();
            Assert.DoesNotThrow(() => _provider.Remove(parent));
        }

        // --- Get ---

        [UnityTest]
        [Description("Get with unregistered ID => throws GetException?")]
        public IEnumerator Get_UnregisteredID_ThrowsGetException(
            [ValueSource(nameof(NonEmptyIDs))] string id)
        {
            yield return null;
            Assert.Throws<SceneContextProvider.GetException>(() => _provider.Get(id));
        }

        // --- GetInDependencyOrder ---

        [UnityTest]
        [Description("GetInDependencyOrder with no contexts => returns empty list?")]
        public IEnumerator GetInDependencyOrder_NoContexts_ReturnsEmptyList()
        {
            yield return null;
            Assert.IsEmpty(_provider.GetInDependencyOrder());
        }

        [UnityTest]
        [Description("GetInDependencyOrder with a single context => returns that context?")]
        public IEnumerator GetInDependencyOrder_SingleContext_ReturnsThatContext()
        {
            SceneContext ctx = GivenASceneContext("a");
            yield return null;
            _provider.Add(ctx);
            IReadOnlyList<SceneContext> result = _provider.GetInDependencyOrder();
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(ctx, result[0]);
        }

        [UnityTest]
        [Description("GetInDependencyOrder with a parent-child pair => child appears before parent?")]
        public IEnumerator GetInDependencyOrder_ParentAndChild_ChildBeforeParent()
        {
            SceneContext parent = GivenASceneContext("parent");
            SceneContext child = GivenASceneContextWithParent("child", "parent");
            yield return null;
            _provider.Add(parent);
            _provider.Add(child);
            IReadOnlyList<SceneContext> result = _provider.GetInDependencyOrder();
            Assert.AreEqual(2, result.Count);
            Assert.Less(IndexOf(result,child), IndexOf(result,parent));
        }

        [UnityTest]
        [Description("GetInDependencyOrder with two children of same parent => both children before parent?")]
        public IEnumerator GetInDependencyOrder_TwoChildren_BothBeforeParent()
        {
            SceneContext parent = GivenASceneContext("parent");
            SceneContext childA = GivenASceneContextWithParent("childA", "parent");
            SceneContext childB = GivenASceneContextWithParent("childB", "parent");
            yield return null;
            _provider.Add(parent);
            _provider.Add(childA);
            _provider.Add(childB);
            IReadOnlyList<SceneContext> result = _provider.GetInDependencyOrder();
            Assert.AreEqual(3, result.Count);
            Assert.Less(IndexOf(result,childA), IndexOf(result,parent));
            Assert.Less(IndexOf(result,childB), IndexOf(result,parent));
        }

        [UnityTest]
        [Description("GetInDependencyOrder with a three-level chain (C->B->A) => returns [C, B, A]?")]
        public IEnumerator GetInDependencyOrder_ThreeLevelChain_LeafFirst()
        {
            SceneContext a = GivenASceneContext("a");
            SceneContext b = GivenASceneContextWithParent("b", "a");
            SceneContext c = GivenASceneContextWithParent("c", "b");
            yield return null;
            _provider.Add(a);
            _provider.Add(b);
            _provider.Add(c);
            IReadOnlyList<SceneContext> result = _provider.GetInDependencyOrder();
            Assert.AreEqual(3, result.Count);
            Assert.Less(IndexOf(result,c), IndexOf(result,b));
            Assert.Less(IndexOf(result,b), IndexOf(result,a));
        }

        [UnityTest]
        [Description("GetInDependencyOrder with a named parent and an anonymous child (empty ID) => child appears before parent?")]
        public IEnumerator GetInDependencyOrder_AnonymousChildAndNamedParent_ChildBeforeParent()
        {
            SceneContext parent = GivenASceneContext("parent");
            SceneContext anonymousChild = GivenASceneContextWithParent(string.Empty, "parent");
            yield return null;
            _provider.Add(parent);
            _provider.Add(anonymousChild);
            IReadOnlyList<SceneContext> result = _provider.GetInDependencyOrder();
            Assert.AreEqual(2, result.Count);
            Assert.Less(IndexOf(result, anonymousChild), IndexOf(result, parent));
        }

        [UnityTest]
        [Description("GetInDependencyOrder with two independent contexts => both are returned?")]
        public IEnumerator GetInDependencyOrder_TwoIndependentContexts_BothReturned()
        {
            SceneContext x = GivenASceneContext("x");
            SceneContext y = GivenASceneContext("y");
            yield return null;
            _provider.Add(x);
            _provider.Add(y);
            IReadOnlyList<SceneContext> result = _provider.GetInDependencyOrder();
            Assert.AreEqual(2, result.Count);
            Assert.Contains(x, (System.Collections.ICollection)result);
            Assert.Contains(y, (System.Collections.ICollection)result);
        }

        [UnityTest]
        [Description("GetInDependencyOrder does not mutate the provider's live state?")]
        public IEnumerator GetInDependencyOrder_DoesNotMutateLiveState()
        {
            SceneContext parent = GivenASceneContext("parent");
            SceneContext child = GivenASceneContextWithParent("child", "parent");
            yield return null;
            _provider.Add(parent);
            _provider.Add(child);
            _provider.GetInDependencyOrder();
            // Removing child first (no active deps) must still work after the sort ran
            Assert.DoesNotThrow(() => _provider.Remove(child));
            Assert.DoesNotThrow(() => _provider.Remove(parent));
        }

        // --- ValueSource data ---

        private static IEnumerable<string> NonEmptyIDs()
        {
            yield return "scene-a";
            yield return "scene-b";
            yield return "scene-c";
        }

        private static IEnumerable<string> EmptyIDs()
        {
            yield return string.Empty;
            yield return null;
        }

        // --- Helpers ---

        /// <summary>
        /// Creates a SceneContext on an inactive GameObject so Awake never fires,
        /// then sets the private _iD field via reflection.
        /// </summary>
        private SceneContext GivenASceneContext(string id)
        {
            return GivenASceneContextWithParent(id, string.Empty);
        }

        private static int IndexOf(IReadOnlyList<SceneContext> list, SceneContext item)
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], item)) return i;
            return -1;
        }

        private SceneContext GivenASceneContextWithParent(string id, string parentId)
        {
            GameObject go = new GameObject($"SceneContext_{id ?? "null"}");
            go.SetActive(false);
            _gameObjects.Add(go);
            SceneContext context = go.AddComponent<SceneContext>();
            s_idField.SetValue(context, id ?? string.Empty);
            s_parentIdField.SetValue(context, parentId ?? string.Empty);
            return context;
        }
    }
}
