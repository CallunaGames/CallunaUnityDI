using NUnit.Framework;
using UnityEngine;

namespace Calluna.DI.Tests
{
    public class MissingBindingExceptionTests
    {
        private BasicDIContext _context;

        [SetUp]
        public void SetUp()
        {
            _context = new Bootstrapper().Resolver.Resolve<BasicDIContext>();
        }

        // ServiceA injects ServiceB, ServiceB injects ServiceC, ServiceC requests an unbound contract.
        // The exception message should identify each requester in the chain, innermost first.

        [Test]
        public void Resolve_MissingBindingInNestedInjection_MessageContainsDirectRequester()
        {
            GivenAChainOfThreeServices();
            TestDelegate test = WhenResolvingRootService();
            MissingBindingException exception = Assert.Throws<MissingBindingException>(test);
            StringAssert.Contains("requested by", exception.Message);
            StringAssert.Contains(nameof(ServiceC), exception.Message);
        }

        [Test]
        public void Resolve_MissingBindingInNestedInjection_MessageContainsFullInjectionChain()
        {
            GivenAChainOfThreeServices();
            TestDelegate test = WhenResolvingRootService();
            MissingBindingException exception = Assert.Throws<MissingBindingException>(test);
            StringAssert.Contains(nameof(ServiceC), exception.Message);
            StringAssert.Contains(nameof(ServiceB), exception.Message);
            StringAssert.Contains(nameof(ServiceA), exception.Message);
        }

        [Test]
        public void Resolve_MissingBindingInNestedInjection_InnerMostRequesterAppearsFirst()
        {
            GivenAChainOfThreeServices();
            TestDelegate test = WhenResolvingRootService();
            MissingBindingException exception = Assert.Throws<MissingBindingException>(test);
            int indexC = exception.Message.IndexOf(nameof(ServiceC));
            int indexB = exception.Message.IndexOf(nameof(ServiceB));
            int indexA = exception.Message.IndexOf(nameof(ServiceA));
            Assert.Less(indexC, indexB, "ServiceC (direct requester) should appear before ServiceB");
            Assert.Less(indexB, indexA, "ServiceB should appear before ServiceA");
        }

        // ServiceA is bound in a child context; ServiceB and ServiceC are bound in the parent.
        // The full injection chain spans both contexts, so all three type names should appear
        // in the message, with the innermost requester (ServiceC) first.

        [Test]
        public void Resolve_MissingBindingAcrossNestedContexts_MessageContainsAllRequesters()
        {
            ChildDIContext child = CreateChildContextFor(_context);
            GivenCrossContextChain(_context, child);
            MissingBindingException exception = Assert.Throws<MissingBindingException>(
                () => child.Resolver.Resolve<ServiceA>());
            StringAssert.Contains(nameof(ServiceC), exception.Message);
            StringAssert.Contains(nameof(ServiceB), exception.Message);
            StringAssert.Contains(nameof(ServiceA), exception.Message);
        }

        [Test]
        public void Resolve_MissingBindingAcrossNestedContexts_InnermostRequesterAppearsFirst()
        {
            ChildDIContext child = CreateChildContextFor(_context);
            GivenCrossContextChain(_context, child);
            MissingBindingException exception = Assert.Throws<MissingBindingException>(
                () => child.Resolver.Resolve<ServiceA>());
            int indexC = exception.Message.IndexOf(nameof(ServiceC));
            int indexB = exception.Message.IndexOf(nameof(ServiceB));
            int indexA = exception.Message.IndexOf(nameof(ServiceA));
            Assert.Less(indexC, indexB, "ServiceC (direct requester of IUnbound) should appear before ServiceB");
            Assert.Less(indexB, indexA, "ServiceB (parent-context requester) should appear before ServiceA (child-context requester)");
        }

        private void GivenAChainOfThreeServices()
        {
            _context.Binder.BindToNewSelf<ServiceA>().AsSingle();
            _context.Binder.BindToNewSelf<ServiceB>().AsSingle();
            _context.Binder.BindToNewSelf<ServiceC>().AsSingle();
            // IUnbound is intentionally not registered
        }

        // ServiceA bound in child, ServiceB and ServiceC in parent — chain crosses context boundary.
        private static void GivenCrossContextChain(BasicDIContext parent, ChildDIContext child)
        {
            parent.Binder.BindToNewSelf<ServiceB>().AsSingle();
            parent.Binder.BindToNewSelf<ServiceC>().AsSingle();
            child.Binder.BindToNewSelf<ServiceA>().AsSingle();
            // IUnbound is intentionally not registered
        }

        private TestDelegate WhenResolvingRootService()
        {
            return () => _context.Resolver.Resolve<ServiceA>();
        }

        private static ChildDIContext CreateChildContextFor(BasicDIContext parent)
        {
            BasicInstanceResolver infraResolver = new BasicInstanceResolver();
            GameObjectContextsReseter contextReseter = new GameObjectContextsReseter();
            InstantiationInfoValidator validator = new InstantiationInfoValidator();
            BasicInstanceResolver validatorInfra = new BasicInstanceResolver();
            validatorInfra.Add<InstanceCreationModeValidator>(new InstanceCreationModeValidatorImpl());
            (validator as Injectable).Inject(validatorInfra);

            infraResolver.Add<DIContext>(parent);
            infraResolver.Add(validator);
            infraResolver.Add(new GameObjectInjector());
            infraResolver.Add(new DIInstanceFactory());
            infraResolver.Add(new DIContainers(
                new BindingsContainer(),
                new SingleInstancesContainer(),
                new NonLazyContainer(),
                new DisposablesContainer(),
                new ObjectsContainer(),
                new GameObjectsContainer(contextReseter),
                new CleanablesContainer()));

            ChildResolver setupResolver = new ChildResolver(parent.Resolver, infraResolver);
            return new ChildDIContextFactory().Create(setupResolver);
        }

        // --- Test types ---

        private class ServiceA : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<ServiceB>();
        }

        private class ServiceB : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<ServiceC>();
        }

        private class ServiceC : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<IUnbound>();
        }

        private interface IUnbound { }
    }
}
