using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Calluna.DI.Tests
{
    public class CircularDependencyTests
    {
        private BasicDIContext _context;

        [SetUp]
        public void SetUp()
        {
            _context = new Bootstrapper().Resolver.Resolve<BasicDIContext>();
        }

        // --- CircularDependencyException constructor ---

        [Test]
        public void Constructor_TwoTypeChain_MessageContainsBothTypeNames()
        {
            var chain = new List<Type> { typeof(TwoHopA), typeof(TwoHopB), typeof(TwoHopA) };
            var exception = new CircularDependencyException(chain);
            StringAssert.Contains(nameof(TwoHopA), exception.Message);
            StringAssert.Contains(nameof(TwoHopB), exception.Message);
        }

        [Test]
        public void Constructor_TwoTypeChain_MessageContainsArrowSeparator()
        {
            var chain = new List<Type> { typeof(TwoHopA), typeof(TwoHopB), typeof(TwoHopA) };
            var exception = new CircularDependencyException(chain);
            StringAssert.Contains("→", exception.Message);
        }

        // --- Cycle detection via DIContextBase ---

        // SelfDependent.Inject resolves SelfDependent — the simplest possible cycle.

        [Test]
        public void Resolve_SelfCycle_ThrowsCircularDependencyException()
        {
            _context.Binder.BindToNewSelf<SelfDependent>().AsSingle();
            Assert.Throws<CircularDependencyException>(
                () => _context.Resolver.Resolve<SelfDependent>());
        }

        // TwoHopA.Inject → TwoHopB; TwoHopB.Inject → TwoHopA.

        [Test]
        public void Resolve_TwoHopCycle_ThrowsCircularDependencyException()
        {
            _context.Binder.BindToNewSelf<TwoHopA>().AsSingle();
            _context.Binder.BindToNewSelf<TwoHopB>().AsSingle();
            Assert.Throws<CircularDependencyException>(
                () => _context.Resolver.Resolve<TwoHopA>());
        }

        [Test]
        public void Resolve_TwoHopCycle_ExceptionMessageContainsBothTypeNames()
        {
            _context.Binder.BindToNewSelf<TwoHopA>().AsSingle();
            _context.Binder.BindToNewSelf<TwoHopB>().AsSingle();
            CircularDependencyException exception = Assert.Throws<CircularDependencyException>(
                () => _context.Resolver.Resolve<TwoHopA>());
            StringAssert.Contains(nameof(TwoHopA), exception.Message);
            StringAssert.Contains(nameof(TwoHopB), exception.Message);
        }

        [Test]
        public void Resolve_TwoHopCycle_ExceptionMessageShowsTypeNamesInCreationOrder()
        {
            _context.Binder.BindToNewSelf<TwoHopA>().AsSingle();
            _context.Binder.BindToNewSelf<TwoHopB>().AsSingle();
            CircularDependencyException exception = Assert.Throws<CircularDependencyException>(
                () => _context.Resolver.Resolve<TwoHopA>());
            int indexA = exception.Message.IndexOf(nameof(TwoHopA));
            int indexB = exception.Message.IndexOf(nameof(TwoHopB));
            Assert.Less(indexA, indexB, "TwoHopA (root of cycle) should appear before TwoHopB in the chain");
        }

        // ThreeHopA → ThreeHopB → ThreeHopC → ThreeHopA.

        [Test]
        public void Resolve_ThreeHopCycle_ThrowsCircularDependencyException()
        {
            _context.Binder.BindToNewSelf<ThreeHopA>().AsSingle();
            _context.Binder.BindToNewSelf<ThreeHopB>().AsSingle();
            _context.Binder.BindToNewSelf<ThreeHopC>().AsSingle();
            Assert.Throws<CircularDependencyException>(
                () => _context.Resolver.Resolve<ThreeHopA>());
        }

        [Test]
        public void Resolve_ThreeHopCycle_ExceptionMessageContainsAllThreeTypeNames()
        {
            _context.Binder.BindToNewSelf<ThreeHopA>().AsSingle();
            _context.Binder.BindToNewSelf<ThreeHopB>().AsSingle();
            _context.Binder.BindToNewSelf<ThreeHopC>().AsSingle();
            CircularDependencyException exception = Assert.Throws<CircularDependencyException>(
                () => _context.Resolver.Resolve<ThreeHopA>());
            StringAssert.Contains(nameof(ThreeHopA), exception.Message);
            StringAssert.Contains(nameof(ThreeHopB), exception.Message);
            StringAssert.Contains(nameof(ThreeHopC), exception.Message);
        }

        // After a CircularDependencyException the try/finally in CreateInstance unwinds
        // _creationChain completely. Resolving an unrelated binding on the same context
        // must succeed — proving the chain was cleaned and the context is still usable.

        [Test]
        public void Resolve_AfterCircularDependencyException_ContextRemainsUsable()
        {
            _context.Binder.BindToNewSelf<TwoHopA>().AsSingle();
            _context.Binder.BindToNewSelf<TwoHopB>().AsSingle();
            _context.Binder.BindToNewSelf<Standalone>().AsSingle();

            Assert.Throws<CircularDependencyException>(
                () => _context.Resolver.Resolve<TwoHopA>());

            Standalone result = null;
            Assert.DoesNotThrow(() => result = _context.Resolver.Resolve<Standalone>());
            Assert.IsNotNull(result);
        }

        // --- Test types ---

        private class SelfDependent : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<SelfDependent>();
        }

        private class TwoHopA : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<TwoHopB>();
        }

        private class TwoHopB : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<TwoHopA>();
        }

        private class ThreeHopA : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<ThreeHopB>();
        }

        private class ThreeHopB : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<ThreeHopC>();
        }

        private class ThreeHopC : Injectable
        {
            public void Inject(Resolver resolver) => resolver.Resolve<ThreeHopA>();
        }

        private class Standalone : Injectable
        {
            public void Inject(Resolver resolver) { }
        }
    }
}
