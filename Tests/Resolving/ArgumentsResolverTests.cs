using NUnit.Framework;

namespace Calluna.DI.Tests
{
    public class ArgumentsResolverTests
    {
        private BasicInstanceResolver _baseResolver;
        private ArgumentsResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _baseResolver = new BasicInstanceResolver();
            _resolver = new ArgumentsResolver(_baseResolver);
        }

        // ── SetArgument: overwrites existing ────────────────────────────────────

        [Test]
        public void ArgumentsResolver_SetArgument_OverwritesValueAddedByAddArgument()
        {
            ServiceA original = new ServiceA();
            ServiceA replacement = new ServiceA();
            _resolver.AddArgument(original);

            _resolver.SetArgument(replacement);

            Assert.AreSame(replacement, _resolver.Resolve<ServiceA>(),
                "SetArgument must overwrite a value previously added by AddArgument");
        }

        [Test]
        public void ArgumentsResolver_SetArgument_CalledTwice_ReturnsLastValue()
        {
            ServiceA first = new ServiceA();
            ServiceA second = new ServiceA();
            _resolver.SetArgument(first);

            _resolver.SetArgument(second);

            Assert.AreSame(second, _resolver.Resolve<ServiceA>(),
                "Calling SetArgument twice must return the most recent value");
        }

        // ── SetArgument: adds when absent ────────────────────────────────────────

        [Test]
        public void ArgumentsResolver_SetArgument_AddsArgumentWhenNotPreviouslyPresent()
        {
            ServiceA instance = new ServiceA();

            _resolver.SetArgument(instance);

            Assert.IsTrue(_resolver.IsResolvable(new BindingKey(typeof(ServiceA))),
                "SetArgument must make the type resolvable even without a prior AddArgument");
        }

        [Test]
        public void ArgumentsResolver_SetArgument_ResolveReturnsSetValue_WhenNotPreviouslyPresent()
        {
            ServiceA instance = new ServiceA();

            _resolver.SetArgument(instance);

            Assert.AreSame(instance, _resolver.Resolve<ServiceA>());
        }

        // ── SetArgument: argument takes precedence over base resolver ─────────────

        [Test]
        public void ArgumentsResolver_SetArgument_TakesPrecedenceOverBaseResolver()
        {
            ServiceA baseInstance = new ServiceA();
            ServiceA argInstance = new ServiceA();
            _baseResolver.Add<ServiceA>(baseInstance);

            _resolver.SetArgument(argInstance);

            Assert.AreSame(argInstance, _resolver.Resolve<ServiceA>(),
                "Argument set via SetArgument must shadow the same type in the base resolver");
        }

        // ── SetArgument: with ID ──────────────────────────────────────────────────

        [Test]
        public void ArgumentsResolver_SetArgument_WithId_ResolveWithSameIdReturnsValue()
        {
            ServiceA instance = new ServiceA();

            _resolver.SetArgument(instance, "my-id");

            Assert.AreSame(instance, _resolver.Resolve<ServiceA>("my-id"));
        }

        [Test]
        public void ArgumentsResolver_SetArgument_WithId_ResolveWithoutIdReturnsFalse()
        {
            _resolver.SetArgument(new ServiceA(), "my-id");

            Assert.IsFalse(_resolver.IsResolvable(new BindingKey(typeof(ServiceA))),
                "An argument registered with an ID must not be resolvable without an ID");
        }

        // ── Test types ────────────────────────────────────────────────────────────

        private class ServiceA { }
    }
}
