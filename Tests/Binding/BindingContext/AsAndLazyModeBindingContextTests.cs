using Moq;
using NUnit.Framework;

namespace Calluna.DI.Tests
{
    public class AsAndLazyModeBindingContextTests
    {
        // --- AsSingle ---

        [Test]
        [Description("AsSingle() => binding.AmountMode is InstanceAmountMode.Single?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void AsSingle_SetsAmountModeToSingle(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            AsBindingContext context = GivenAnAsBindingContext(binding);
            context.AsSingle();
            Assert.AreEqual(InstanceAmountMode.Single, binding.AmountMode);
        }

        // --- PerRequest ---

        [Test]
        [Description("PerRequest() => binding.AmountMode is InstanceAmountMode.PerRequest?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void PerRequest_SetsAmountModeToPerRequest(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            AsBindingContext context = GivenAnAsBindingContext(binding);
            context.PerRequest();
            Assert.AreEqual(InstanceAmountMode.PerRequest, binding.AmountMode);
        }

        [Test]
        [Description("AsSingle then PerRequest => final AmountMode is PerRequest?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void AsSingle_ThenPerRequest_AmountModeIsPerRequest(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            AsBindingContext context = GivenAnAsBindingContext(binding);
            context.AsSingle();
            AsBindingContext secondContext = GivenAnAsBindingContext(binding);
            secondContext.PerRequest();
            Assert.AreEqual(InstanceAmountMode.PerRequest, binding.AmountMode);
        }

        // --- NonLazy ---

        [Test]
        [Description("NonLazy() => calls AddToNonLazy on the backing BindingStorage?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void NonLazy_CallsAddToNonLazyOnBindingStorage(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            Mock<BindingStorage> storageMock = new Mock<BindingStorage>();
            LazyModeBindingContext context = GivenALazyModeBindingContext(binding, storageMock.Object);
            context.NonLazy();
            storageMock.Verify(s => s.AddToNonLazy(binding), Times.Once);
        }

        [Test]
        [Description("NonLazy() via AsSingle chain => calls AddToNonLazy on the backing BindingStorage?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void NonLazy_ViaAsSingleChain_CallsAddToNonLazy(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            Mock<BindingStorage> storageMock = new Mock<BindingStorage>();
            AsBindingContext context = GivenAnAsBindingContext(binding, storageMock.Object);
            context.AsSingle().NonLazy();
            storageMock.Verify(s => s.AddToNonLazy(binding), Times.Once);
        }

        [Test]
        [Description("NonLazy() via PerRequest chain => calls AddToNonLazy on the backing BindingStorage?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void NonLazy_ViaPerRequestChain_CallsAddToNonLazy(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            Mock<BindingStorage> storageMock = new Mock<BindingStorage>();
            AsBindingContext context = GivenAnAsBindingContext(binding, storageMock.Object);
            context.PerRequest().NonLazy();
            storageMock.Verify(s => s.AddToNonLazy(binding), Times.Once);
        }

        // --- WithoutInjection ---

        [Test]
        [Description("WithoutInjection() => binding.InjectionAllowed is false?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void WithoutInjection_SetsInjectionAllowedToFalse(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            AllowInjectionBindingContext context = GivenAnAllowInjectionBindingContext(binding);
            context.WithoutInjection();
            Assert.IsFalse(binding.InjectionAllowed);
        }

        // --- WithInjection ---

        [Test]
        [Description("WithInjection() => binding.InjectionAllowed is true?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void WithInjection_SetsInjectionAllowedToTrue(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            binding.InjectionAllowed = false;
            AllowInjectionBindingContext context = GivenAnAllowInjectionBindingContext(binding);
            context.WithInjection();
            Assert.IsTrue(binding.InjectionAllowed);
        }

        [Test]
        [Description("WithoutInjection then WithInjection => binding.InjectionAllowed is true?")]
        [TestCase(typeof(ServiceA))]
        [TestCase(typeof(ServiceB))]
        [TestCase(typeof(ServiceC))]
        public void WithoutInjection_ThenWithInjection_InjectionAllowedIsTrue(System.Type concreteType)
        {
            Binding binding = GivenABinding(concreteType);
            AllowInjectionBindingContext contextA = GivenAnAllowInjectionBindingContext(binding);
            contextA.WithoutInjection();
            AllowInjectionBindingContext contextB = GivenAnAllowInjectionBindingContext(binding);
            contextB.WithInjection();
            Assert.IsTrue(binding.InjectionAllowed);
        }

        // --- Helpers ---

        private static Binding GivenABinding(System.Type concreteType)
        {
            return new Binding(concreteType);
        }

        private static BindingArguments GivenBindingArguments(Binding binding, BindingStorage storage = null)
        {
            BindingStorage bindingStorage = storage ?? new Mock<BindingStorage>().Object;
            return new BindingArguments(binding, bindingStorage);
        }

        private static AsBindingContext GivenAnAsBindingContext(Binding binding, BindingStorage storage = null)
        {
            return new AsBindingContext(GivenBindingArguments(binding, storage));
        }

        private static LazyModeBindingContext GivenALazyModeBindingContext(Binding binding, BindingStorage storage = null)
        {
            return new LazyModeBindingContext(GivenBindingArguments(binding, storage));
        }

        private static AllowInjectionBindingContext GivenAnAllowInjectionBindingContext(Binding binding, BindingStorage storage = null)
        {
            return new AllowInjectionBindingContext(GivenBindingArguments(binding, storage));
        }

        // --- Test types ---

        private class ServiceA { }
        private class ServiceB { }
        private class ServiceC { }
    }
}
