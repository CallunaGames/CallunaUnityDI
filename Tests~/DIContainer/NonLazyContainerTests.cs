using System.Linq;
using NUnit.Framework;

namespace Calluna.DI.Tests
{
    public class NonLazyContainerTests
    {
        private NonLazyContainer _container;

        [SetUp]
        public void SetUp()
        {
            _container = new NonLazyContainer();
        }

        // --- Add / Bindings ---

        [Test]
        [Description("Add binding => binding appears in Bindings property?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void Add_SingleBinding_AppearsInBindingsProperty(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            _container.Add(binding);
            Assert.Contains(binding, _container.Bindings.ToList());
        }

        [Test]
        [Description("Add multiple distinct bindings => all appear in Bindings property?")]
        public void Add_MultipleBindings_AllAppearInBindingsProperty()
        {
            Binding bindingA = GivenABinding(typeof(ServiceA));
            Binding bindingB = GivenABinding(typeof(ServiceB));
            Binding bindingC = GivenABinding(typeof(ServiceC));

            _container.Add(bindingA);
            _container.Add(bindingB);
            _container.Add(bindingC);

            Assert.Contains(bindingA, _container.Bindings.ToList());
            Assert.Contains(bindingB, _container.Bindings.ToList());
            Assert.Contains(bindingC, _container.Bindings.ToList());
        }

        // --- ShallCreate ---

        [Test]
        [Description("ShallCreate on added but not yet created binding => returns true?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void ShallCreate_AfterAdd_NotYetCreated_ReturnsTrue(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            _container.Add(binding);
            Assert.IsTrue(_container.ShallCreate(binding));
        }

        [Test]
        [Description("ShallCreate on binding not added to non-lazy set => returns false?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void ShallCreate_NotAdded_ReturnsFalse(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            Assert.IsFalse(_container.ShallCreate(binding));
        }

        // --- TryAddToCreated ---

        [Test]
        [Description("TryAddToCreated on an added non-lazy binding => ShallCreate subsequently returns false?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void TryAddToCreated_AfterAdd_SubsequentShallCreateReturnsFalse(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            _container.Add(binding);
            _container.TryAddToCreated(binding);
            Assert.IsFalse(_container.ShallCreate(binding));
        }

        [Test]
        [Description("TryAddToCreated on binding not in non-lazy set => ShallCreate is unaffected and returns false?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void TryAddToCreated_BindingNotAdded_ShallCreateRemainsUnaffected(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            _container.TryAddToCreated(binding);
            Assert.IsFalse(_container.ShallCreate(binding));
        }

        [Test]
        [Description("TryAddToCreated called twice on the same added binding => ShallCreate still returns false (idempotent)?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void TryAddToCreated_CalledTwice_ShallCreateReturnsFalse(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            _container.Add(binding);
            _container.TryAddToCreated(binding);
            _container.TryAddToCreated(binding);
            Assert.IsFalse(_container.ShallCreate(binding));
        }

        [Test]
        [Description("ShallCreate after Add is true; after TryAddToCreated it turns false; another binding in same container is unaffected?")]
        public void TryAddToCreated_DoesNotAffectOtherBindings()
        {
            Binding bindingA = GivenABinding(typeof(ServiceA));
            Binding bindingB = GivenABinding(typeof(ServiceB));
            _container.Add(bindingA);
            _container.Add(bindingB);
            _container.TryAddToCreated(bindingA);
            Assert.IsFalse(_container.ShallCreate(bindingA));
            Assert.IsTrue(_container.ShallCreate(bindingB));
        }

        // --- Helpers ---

        private static Binding GivenABinding(System.Type concreteType)
        {
            return new Binding(concreteType);
        }

        // --- Test types ---

        private class ServiceA { }
        private class ServiceB { }
        private class ServiceC { }
    }
}
