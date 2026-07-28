using NUnit.Framework;

namespace Calluna.DI.Tests
{
    public class ChildResolverTests
    {
        private BasicInstanceResolver _parentResolver;
        private BasicInstanceResolver _baseResolver;
        private ChildResolver _childResolver;

        [SetUp]
        public void SetUp()
        {
            _parentResolver = new BasicInstanceResolver();
            _baseResolver = new BasicInstanceResolver();
            _childResolver = new ChildResolver(_parentResolver, _baseResolver);
        }

        // --- IsResolvable ---

        [Test]
        public void IsResolvable_TypeInBaseOnly_ReturnsTrue()
        {
            _baseResolver.Add<ServiceA>(new ServiceA());
            Assert.IsTrue(_childResolver.IsResolvable(new BindingKey(typeof(ServiceA))));
        }

        [Test]
        public void IsResolvable_TypeInParentOnly_ReturnsTrue()
        {
            _parentResolver.Add<ServiceA>(new ServiceA());
            Assert.IsTrue(_childResolver.IsResolvable(new BindingKey(typeof(ServiceA))));
        }

        [Test]
        public void IsResolvable_TypeInNeither_ReturnsFalse()
        {
            Assert.IsFalse(_childResolver.IsResolvable(new BindingKey(typeof(ServiceA))));
        }

        [Test]
        public void IsResolvable_TypeInBoth_ReturnsTrue()
        {
            _baseResolver.Add<ServiceA>(new ServiceA());
            _parentResolver.Add<ServiceA>(new ServiceA());
            Assert.IsTrue(_childResolver.IsResolvable(new BindingKey(typeof(ServiceA))));
        }

        // --- Resolve: base takes precedence over parent ---

        [Test]
        public void Resolve_TypeInBoth_ReturnsBaseInstance()
        {
            ServiceA baseInstance = new ServiceA();
            ServiceA parentInstance = new ServiceA();
            _baseResolver.Add<ServiceA>(baseInstance);
            _parentResolver.Add<ServiceA>(parentInstance);

            Assert.AreSame(baseInstance, _childResolver.Resolve<ServiceA>());
        }

        [Test]
        public void Resolve_TypeInBaseOnly_ReturnsBaseInstance()
        {
            ServiceA baseInstance = new ServiceA();
            _baseResolver.Add<ServiceA>(baseInstance);

            Assert.AreSame(baseInstance, _childResolver.Resolve<ServiceA>());
        }

        [Test]
        public void Resolve_TypeInParentOnly_ReturnsParentInstance()
        {
            ServiceA parentInstance = new ServiceA();
            _parentResolver.Add<ServiceA>(parentInstance);

            Assert.AreSame(parentInstance, _childResolver.Resolve<ServiceA>());
        }

        // --- ResolveOptional ---

        [Test]
        public void ResolveOptional_TypeInBase_ReturnsInstance()
        {
            ServiceA instance = new ServiceA();
            _baseResolver.Add<ServiceA>(instance);
            Assert.AreSame(instance, _childResolver.ResolveOptional<ServiceA>());
        }

        [Test]
        public void ResolveOptional_TypeInParentOnly_ReturnsParentInstance()
        {
            ServiceA instance = new ServiceA();
            _parentResolver.Add<ServiceA>(instance);
            Assert.AreSame(instance, _childResolver.ResolveOptional<ServiceA>());
        }

        [Test]
        public void ResolveOptional_TypeInNeither_ReturnsNull()
        {
            Assert.IsNull(_childResolver.ResolveOptional<ServiceA>());
        }

        // --- Test types ---

        private class ServiceA { }
    }
}
