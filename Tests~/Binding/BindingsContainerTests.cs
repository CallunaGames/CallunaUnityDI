using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Calluna.DI.Tests
{
    public class BindingsContainerTests
    {
        private BindingsContainer _container;

        [SetUp]
        public void SetUp()
        {
            _container = new BindingsContainer();
        }

        // --- HasBinding ---

        [Test]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void HasBinding_AfterAddBinding_ReturnsTrue(Type contractType)
        {
            BindingKey key = new BindingKey(contractType);
            _container.AddBinding(key, new Binding(contractType));
            Assert.IsTrue(_container.HasBinding(key));
        }

        [Test]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void HasBinding_WhenNothingBound_ReturnsFalse(Type contractType)
        {
            BindingKey key = new BindingKey(contractType);
            Assert.IsFalse(_container.HasBinding(key));
        }

        [Test]
        [TestCase(1)]
        [TestCase(42)]
        [TestCase("slot-a")]
        public void HasBinding_WithID_FoundOnlyUnderMatchingID(IComparable id)
        {
            BindingKey keyWithId = new BindingKey(typeof(ServiceA), id);
            BindingKey keyWithoutId = new BindingKey(typeof(ServiceA));
            _container.AddBinding(keyWithId, new Binding(typeof(ServiceA)));

            Assert.IsTrue(_container.HasBinding(keyWithId));
            Assert.IsFalse(_container.HasBinding(keyWithoutId));
        }

        // --- AddBinding (BindingKey overload) ---

        [Test]
        public void AddBinding_KeyOverload_BindingIsRetrievable()
        {
            Binding binding = new Binding(typeof(ServiceA));
            BindingKey key = new BindingKey(typeof(ServiceA));
            _container.AddBinding(key, binding);
            Assert.AreSame(binding, _container.GetBinding(key));
        }

        [Test]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void AddBinding_SameKeyTwice_ThrowsAlreadyBoundException(Type contractType)
        {
            BindingKey key = new BindingKey(contractType);
            _container.AddBinding(key, new Binding(contractType));
            Assert.Throws<AlreadyBoundException>(() =>
                _container.AddBinding(key, new Binding(contractType)));
        }

        // --- AddBinding (generic overload) ---

        [Test]
        public void AddBinding_Generic_BindingIsRetrievable()
        {
            Binding binding = new Binding(typeof(ServiceA));
            _container.AddBinding<ServiceA>(binding);
            Assert.AreSame(binding, _container.GetBinding(new BindingKey(typeof(ServiceA))));
        }

        [Test]
        [TestCase(7)]
        [TestCase(99)]
        [TestCase("named")]
        public void AddBinding_GenericWithID_BindingRetrievableUnderThatID(IComparable id)
        {
            Binding binding = new Binding(typeof(ServiceA));
            _container.AddBinding<ServiceA>(binding, id);
            Assert.AreSame(binding, _container.GetBinding(new BindingKey(typeof(ServiceA), id)));
        }

        [Test]
        public void AddBinding_GenericSameTypeTwice_ThrowsAlreadyBoundException()
        {
            _container.AddBinding<ServiceA>(new Binding(typeof(ServiceA)));
            Assert.Throws<AlreadyBoundException>(() =>
                _container.AddBinding<ServiceA>(new Binding(typeof(ServiceA))));
        }

        // --- GetBinding ---

        [Test]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void GetBinding_UnregisteredType_ThrowsMissingBindingException(Type contractType)
        {
            BindingKey key = new BindingKey(contractType);
            Assert.Throws<MissingBindingException>(() => _container.GetBinding(key));
        }

        [Test]
        public void GetBinding_WrongID_ThrowsMissingBindingException()
        {
            _container.AddBinding(new BindingKey(typeof(ServiceA), "correct"), new Binding(typeof(ServiceA)));
            Assert.Throws<MissingBindingException>(() =>
                _container.GetBinding(new BindingKey(typeof(ServiceA), "wrong")));
        }

        // --- Remove ---

        [Test]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void Remove_AfterAddBinding_HasBindingReturnsFalse(Type contractType)
        {
            BindingKey key = new BindingKey(contractType);
            _container.AddBinding(key, new Binding(contractType));
            _container.Remove(key);
            Assert.IsFalse(_container.HasBinding(key));
        }

        [Test]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void Remove_NonExistentKey_ThrowsMissingBindingException(Type contractType)
        {
            BindingKey key = new BindingKey(contractType);
            Assert.Throws<MissingBindingException>(() => _container.Remove(key));
        }

        [Test]
        public void Remove_ThenAddSameKey_DoesNotThrow()
        {
            BindingKey key = new BindingKey(typeof(ServiceA));
            _container.AddBinding(key, new Binding(typeof(ServiceA)));
            _container.Remove(key);
            Assert.DoesNotThrow(() => _container.AddBinding(key, new Binding(typeof(ServiceA))));
        }

        // --- GetInstantiationInfos ---

        [Test]
        public void GetInstantiationInfos_EmptyContainer_ReturnsEmptyCollection()
        {
            int count = 0;
            foreach (InstantiationInfo _ in _container.GetInstantiationInfos())
                count++;
            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetInstantiationInfos_AfterAddingThreeBindings_ContainsAllThree()
        {
            Binding bindingA = new Binding(typeof(ServiceA));
            Binding bindingB = new Binding(typeof(ServiceB));
            Binding bindingC = new Binding(typeof(ServiceC));
            _container.AddBinding(new BindingKey(typeof(ServiceA)), bindingA);
            _container.AddBinding(new BindingKey(typeof(ServiceB)), bindingB);
            _container.AddBinding(new BindingKey(typeof(ServiceC)), bindingC);

            List<InstantiationInfo> infos = new List<InstantiationInfo>(_container.GetInstantiationInfos());

            Assert.AreEqual(3, infos.Count);
            Assert.Contains(bindingA, infos);
            Assert.Contains(bindingB, infos);
            Assert.Contains(bindingC, infos);
        }

        [Test]
        public void GetInstantiationInfos_AfterRemove_DoesNotContainRemovedBinding()
        {
            BindingKey keyA = new BindingKey(typeof(ServiceA));
            BindingKey keyB = new BindingKey(typeof(ServiceB));
            Binding bindingA = new Binding(typeof(ServiceA));
            Binding bindingB = new Binding(typeof(ServiceB));
            _container.AddBinding(keyA, bindingA);
            _container.AddBinding(keyB, bindingB);
            _container.Remove(keyA);

            List<InstantiationInfo> infos = new List<InstantiationInfo>(_container.GetInstantiationInfos());

            Assert.AreEqual(1, infos.Count);
            Assert.Contains(bindingB, infos);
        }

        // --- Test types ---

        private class ServiceA { }
        private class ServiceB { }
        private class ServiceC { }
    }
}
