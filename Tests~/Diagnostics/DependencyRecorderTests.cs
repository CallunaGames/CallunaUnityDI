using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Calluna.DI.Tests
{
    public class DependencyRecorderTests
    {
        private const string ContextName = "Test";
        private BasicDIContext _context;

        [SetUp]
        public void SetUp()
        {
            DependencyRecorder.Clear();
            DependencyRecorder.Start();
            _context = new Bootstrapper().Resolver.Resolve<BasicDIContext>();
            ((DIContext)_context).Name = ContextName;
        }

        [TearDown]
        public void TearDown()
        {
            DependencyRecorder.Stop();
            DependencyRecorder.Clear();
        }

        [Test]
        public void Resolve_RecordsTheRequesterTheContractAndTheProvidingContext()
        {
            _context.Binder.BindToNewSelf<Service>().AsSingle();
            _context.Binder.BindToNewSelf<Consumer>();

            _context.Resolver.Resolve<Consumer>();

            DependencyEdge edge = Edge(Name<Consumer>(), Name<Service>());
            Assert.IsNotNull(edge);
            Assert.AreEqual(ContextName, edge.ProviderContext);
            Assert.AreEqual(DependencyKind.Binding, edge.Kind);
            Assert.AreEqual(1, edge.Count);
            Assert.IsNotNull(Edge(DependencyRecorder.OutsideInjection, Name<Consumer>()), "The test's own resolve.");
        }

        [Test]
        public void Resolve_SameDependencyAgain_CountsTheEdge()
        {
            _context.Binder.BindToNewSelf<Service>().AsSingle();
            _context.Binder.BindToNewSelf<Consumer>();

            _context.Resolver.Resolve<Consumer>();
            _context.Resolver.Resolve<Consumer>();

            Assert.AreEqual(2, Edge(Name<Consumer>(), Name<Service>()).Count);
        }

        [Test]
        public void Resolve_ArgumentAndMissingOptional_AreRecordedAsSuch()
        {
            _context.Binder.BindToNewSelf<Service>().AsSingle();
            _context.Binder.BindToNewSelf<Consumer>().WithArgument("argument");

            _context.Resolver.Resolve<Consumer>();

            Assert.AreEqual(DependencyKind.Argument, Edge(Name<Consumer>(), "String").Kind);
            Assert.AreEqual(DependencyKind.MissingOptional, Edge(Name<Consumer>(), Name<MissingService>()).Kind);
        }

        [Test]
        public void RecordBindings_BindingNeverResolved_HasNoUses()
        {
            _context.Binder.BindToNewSelf<Service>().AsSingle();
            _context.Binder.BindToNewSelf<Consumer>();
            ((DIContext)_context).RecordBindings();

            _context.Resolver.Resolve<Service>();

            DependencyContext context = DependencyRecorder.Graph.Contexts.Single(c => c.Name == ContextName);
            Assert.AreEqual(1, Binding(context, Name<Service>()).ResolveCount);
            Assert.AreEqual(0, Binding(context, Name<Consumer>()).ResolveCount);
        }

        [Test]
        public void CreateNonLazyInstances_CountsAsUse_ByTheContext()
        {
            _context.Binder.BindToNewSelf<Service>().AsSingle().NonLazy();
            ((DIContext)_context).RecordBindings();

            _context.CreateNonLazyInstances();

            DependencyContext context = DependencyRecorder.Graph.Contexts.Single(c => c.Name == ContextName);
            Assert.AreEqual(1, Binding(context, Name<Service>()).ResolveCount);
            Assert.IsNotNull(Edge(DependencyRecorder.NonLazy, Name<Service>()));
        }

        [Test]
        public void RecordBindings_DescribesTheBinding()
        {
            _context.Binder.Bind<List<string>>().ToNew<List<string>>().PerRequest().Untracked();
            ((DIContext)_context).RecordBindings();

            DependencyBinding binding = Binding(DependencyRecorder.Graph.Contexts.Single(), "List<String>");

            Assert.AreEqual("List<String>", binding.Concrete);
            Assert.AreEqual(InstanceAmountMode.PerRequest, binding.AmountMode);
            Assert.AreEqual(InstanceCreationMode.FromNew, binding.CreationMode);
            Assert.IsFalse(binding.Tracked);
        }

        [Test]
        public void Resolve_NotRecording_RecordsNothing()
        {
            DependencyRecorder.Stop();
            _context.Binder.BindToNewSelf<Service>().AsSingle();
            _context.Binder.BindToNewSelf<Consumer>();

            _context.Resolver.Resolve<Consumer>();

            CollectionAssert.IsEmpty(DependencyRecorder.Graph.Edges);
        }

        [Test]
        public void GameObjectContext_IsNamedWithoutCloneAndKnowsItsParent()
        {
            using TestApp app = new TestApp();
            ((DIContext)app.Context).Name = "App";
            GameObject pooled = new GameObject("Pooled(Clone)");
            pooled.transform.SetParent(app.Root.transform);
            GameObjectContext context = pooled.AddComponent<GameObjectContext>();
            typeof(MonoContext).GetField("_monoInstallers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(context, new MonoInstaller[0]);

            new GameObjectInjector().InjectIntoContextHierarchy(pooled.transform, app.Resolver);

            DependencyContext recorded = DependencyRecorder.Graph.Contexts.Single(c => c.Name == "GameObjectContext Pooled");
            Assert.AreEqual("App", recorded.Parent);
        }

        [Test]
        public void Export_WritesMermaidAndDot_AndFilters()
        {
            _context.Binder.BindToNewSelf<Service>().AsSingle();
            _context.Binder.BindToNewSelf<Consumer>();
            _context.Binder.Bind<List<string>>().ToNew<List<string>>();
            ((DIContext)_context).RecordBindings();
            _context.Resolver.Resolve<Consumer>();

            string mermaid = DependencyRecorder.ToMermaid();
            string dot = DependencyRecorder.ToDot();
            string filtered = DependencyRecorder.ToMermaid(nameof(Service));

            StringAssert.StartsWith("flowchart LR", mermaid);
            StringAssert.Contains("List#lt;String#gt;", mermaid);
            StringAssert.StartsWith("digraph DI {", dot);
            StringAssert.Contains("->", dot);
            StringAssert.Contains(nameof(Service), filtered);
            StringAssert.DoesNotContain("List#lt;String#gt;", filtered);
        }

        private static string Name<T>() => $"{nameof(DependencyRecorderTests)}.{typeof(T).Name}";

        private static DependencyEdge Edge(string requester, string contract) =>
            DependencyRecorder.Graph.Edges.FirstOrDefault(e => e.Requester == requester && e.Contract == contract);

        private static DependencyBinding Binding(DependencyContext context, string contract) =>
            context.Bindings.Single(b => b.Contract == contract);

        private class Service { }

        private class MissingService { }

        private class Consumer : Injectable
        {
            public void Inject(Resolver resolver)
            {
                resolver.Resolve<Service>();
                resolver.ResolveOptional<string>();
                resolver.ResolveOptional<MissingService>();
            }
        }
    }
}
