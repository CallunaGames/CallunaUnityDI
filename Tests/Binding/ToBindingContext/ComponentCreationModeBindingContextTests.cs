using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Calluna.DI.Tests
{
    public class ComponentCreationModeBindingContextTests
    {
        private ComponentCreationModeBindingContext<Foo> _context;
        private Mock<BindingStorage> _bindingStorageMock;
        private BindingStorage BindingStorage => _bindingStorageMock.Object;
        private Binding _binding;
        
        public static Arguments[] CreateValidArguments()
        {
            return new Arguments[]
            {
                new Arguments
                {
                    Name = "One",
                    Types = new List<Type>() { typeof(Foo) }
                },
                new Arguments
                {
                    Name = "Two",
                    Types = new List<Type>() { typeof(Foo), typeof(Bar) }
                },
                new Arguments
                {
                    Name = "Three",
                    Types = new List<Type>() { typeof(Foo) }
                },
            };
        }

        public static Arguments[] CreateInvalidArguments()
        {
            return new Arguments[]
            {
                new Arguments
                {
                    Name = "One",
                    Types = new List<Type>() { }
                },
                new Arguments
                {
                    Name = "Two",
                    Types = new List<Type>() { typeof(Bar) }
                },
                new Arguments
                {
                    Name = "Three",
                    Types = new List<Type>() { }
                },
            };
        }

        [Test]
        public void FromNewPrefabInstance_ThrowsExceptionIfPrefabDoesNotHaveComponent(
            [ValueSource(nameof(CreateInvalidArguments))] Arguments args)
        {
            GameObject prefab = CreateFromArgs(args);
            GivenADefaultSetup();
            Action test = () => WhenFromNewPrefabInstanceIsCalled(prefab);
            ThenThrowsException<MissingComponentException>(test);
            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void FromNewPrefabInstance_SetsBindingCreationMode(
            [ValueSource(nameof(CreateValidArguments))] Arguments args)
        {
            GameObject prefab = CreateFromArgs(args);
            GivenADefaultSetup();
            WhenFromNewPrefabInstanceIsCalled(prefab);
            ThenCreationModeIsSetTo(InstanceCreationMode.FromPrefabInstance);
        }

        [Test]
        public void FromNewPrefabInstance_InstanceProvideFunctionReturnsPrefab(
            [ValueSource(nameof(CreateValidArguments))] Arguments args)
        {
            GameObject prefab = CreateFromArgs(args);
            GivenADefaultSetup();
            WhenFromNewPrefabInstanceIsCalled(prefab);
            ThenProvideInstanceFunctionReturns(prefab);
            Object.DestroyImmediate(prefab);
        }

        private GameObject CreateFromArgs(Arguments args)
        {
            GameObject result = new GameObject(args.Name);
            foreach (Type type in args.Types)
            {
                result.AddComponent(type);
            }

            return result;
        }

        private void GivenADefaultSetup()
        {
            GivenAMockBindingStorage();
            GivenANewBinding();
            GivenANewContext();
        }

        private void GivenANewBinding()
        {
            _binding = new Binding(typeof(Foo));
        }

        private void GivenAMockBindingStorage()
        {
            Mock<BindingStorage> bindingStorageMock = new Mock<BindingStorage>();
            _bindingStorageMock = bindingStorageMock;
        }

        private void GivenANewContext()
        {
            BindingArguments arguments = new BindingArguments(_binding, BindingStorage);
            _context = new ComponentCreationModeBindingContext<Foo>(arguments);
        }

        private void WhenFromNewPrefabInstanceIsCalled(GameObject prefab)
        {
            _context.FromNewPrefabInstance(prefab);
        }

        private void ThenThrowsException<T>(Action test) where T : Exception
        {
            Assert.Throws<T>(() => test());
        }

        private void ThenCreationModeIsSetTo(InstanceCreationMode creationMode)
        {
            Assert.AreEqual(_binding.CreationMode, creationMode);
        }

        private void ThenProvideInstanceFunctionReturns(object prefab)
        {
            Assert.AreEqual(_binding.ProvideInstanceFunction(), prefab);
        }

        public class Foo : MonoBehaviour{}

        public class Bar : MonoBehaviour{}

        public struct Arguments
        {
            public string Name;
            public List<Type> Types;
        }
    }
}
