using System;
using NUnit.Framework;

namespace Calluna.DI.Tests
{
    public class BasicInstanceResolverTests
    {
        private BasicInstanceResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = new BasicInstanceResolver();
        }

        // --- IsResolvable ---

        [Test]
        public void IsResolvable_BeforeAdd_ReturnsFalse()
        {
            Assert.IsFalse(_resolver.IsResolvable(new BindingKey(typeof(ServiceA))));
        }

        [Test]
        public void IsResolvable_AfterAdd_ReturnsTrue()
        {
            _resolver.Add<ServiceA>(new ServiceA());
            Assert.IsTrue(_resolver.IsResolvable(new BindingKey(typeof(ServiceA))));
        }

        [Test]
        [TestCase(1)]
        [TestCase(42)]
        [TestCase("myId")]
        public void IsResolvable_WithMatchingID_ReturnsTrue(IComparable id)
        {
            _resolver.Add<ServiceA>(new ServiceA(), id);
            Assert.IsTrue(_resolver.IsResolvable(new BindingKey(typeof(ServiceA), id)));
        }

        [Test]
        [TestCase(1)]
        [TestCase(42)]
        [TestCase("myId")]
        public void IsResolvable_AddedWithoutID_IDKeyReturnsFalse(IComparable id)
        {
            _resolver.Add<ServiceA>(new ServiceA());
            Assert.IsFalse(_resolver.IsResolvable(new BindingKey(typeof(ServiceA), id)));
        }

        // --- Resolve ---

        [Test]
        public void Resolve_AfterAdd_ReturnsSameInstance()
        {
            ServiceA instance = new ServiceA();
            _resolver.Add<ServiceA>(instance);
            Assert.AreSame(instance, _resolver.Resolve<ServiceA>());
        }

        [Test]
        public void Resolve_InterfaceContract_ReturnsAddedInstance()
        {
            ServiceA instance = new ServiceA();
            _resolver.Add<IService>(instance);
            Assert.AreSame(instance, _resolver.Resolve<IService>());
        }

        [Test]
        [TestCase(10)]
        [TestCase(99)]
        [TestCase("slot")]
        public void Resolve_WithID_ReturnsInstanceAddedUnderSameID(IComparable id)
        {
            ServiceA instance = new ServiceA();
            _resolver.Add<ServiceA>(instance, id);
            Assert.AreSame(instance, _resolver.Resolve<ServiceA>(id));
        }

        [Test]
        public void Resolve_TwoInstancesSameTypeDifferentIDs_EachReturnsOwnInstance()
        {
            ServiceA instanceOne = new ServiceA();
            ServiceA instanceTwo = new ServiceA();
            _resolver.Add<ServiceA>(instanceOne, "one");
            _resolver.Add<ServiceA>(instanceTwo, "two");

            Assert.AreSame(instanceOne, _resolver.Resolve<ServiceA>("one"));
            Assert.AreSame(instanceTwo, _resolver.Resolve<ServiceA>("two"));
        }

        // --- ResolveOptional ---

        [Test]
        public void ResolveOptional_AfterAdd_ReturnsInstance()
        {
            ServiceA instance = new ServiceA();
            _resolver.Add<ServiceA>(instance);
            Assert.AreSame(instance, _resolver.ResolveOptional<ServiceA>());
        }

        [Test]
        public void ResolveOptional_WhenNotAdded_ReturnsNull()
        {
            Assert.IsNull(_resolver.ResolveOptional<ServiceA>());
        }

        [Test]
        public void ResolveOptional_WithWrongID_ReturnsNull()
        {
            _resolver.Add<ServiceA>(new ServiceA());
            Assert.IsNull(_resolver.ResolveOptional<ServiceA>("wrongId"));
        }

        // --- Test types ---

        private interface IService { }
        private class ServiceA : IService { }
    }
}
