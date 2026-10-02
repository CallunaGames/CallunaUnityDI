using NUnit.Framework;

namespace Calluna.DI.Tests
{
    public class SingleInstanceTests
    {
        private BasicDIContext _context;

        [SetUp]
        public void SetUp()
        {
            _context = new Bootstrapper().Resolver.Resolve<BasicDIContext>();
        }

        [Test]
        public void Resolve_SingleTwice_ReturnsTheSameInstance()
        {
            _context.Binder.BindToNewSelf<Service>().AsSingle();

            Assert.AreSame(_context.Resolver.Resolve<Service>(), _context.Resolver.Resolve<Service>());
        }

        [Test]
        public void Resolve_OneSingleBindingForSeveralContracts_SharesTheInstance()
        {
            _context.Binder.Bind<Service>().And<ServiceContract>().ToNew<Service>().AsSingle();

            Assert.AreSame(_context.Resolver.Resolve<Service>(), _context.Resolver.Resolve<ServiceContract>());
        }

        // Two separate bindings with equal settings are still two bindings.
        [Test]
        public void Resolve_EqualSingleBindingsWithDifferentIds_ReturnDifferentInstances()
        {
            _context.Binder.Bind<Service>("first").ToNew<Service>().AsSingle();
            _context.Binder.Bind<Service>("second").ToNew<Service>().AsSingle();

            Assert.AreNotSame(_context.Resolver.Resolve<Service>("first"), _context.Resolver.Resolve<Service>("second"));
        }

        private interface ServiceContract { }

        private class Service : ServiceContract { }
    }
}
