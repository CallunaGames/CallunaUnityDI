using System;

namespace Calluna.DI
{
    internal class DIContainers : BindingStorage
    {
        internal BindingsContainer Bindings { get; }
        internal SingleInstancesContainer SingleInstances { get; }
        internal NonLazyContainer NonLazyBindings { get; }
        internal DisposablesContainer Disposables { get; }
        internal ObjectsContainer Objects { get; }
		internal GameObjectsContainer GameObjectsContainer { get; }
		internal CleanablesContainer CleanablesContainer { get; }

		public DIContainers(BindingsContainer bindings,
            SingleInstancesContainer singleInstances,
            NonLazyContainer nonLazyBindings,
            DisposablesContainer disposables,
            ObjectsContainer objects,
            GameObjectsContainer gameObjectsContainer,
            CleanablesContainer cleanablesContainer)
		{
            Bindings = bindings;
            SingleInstances = singleInstances;
            NonLazyBindings = nonLazyBindings;
            Disposables = disposables;
            Objects = objects;
            GameObjectsContainer = gameObjectsContainer;
            CleanablesContainer = cleanablesContainer;
		}

		public void AddBinding<TContract>(Binding binding, IComparable iD = null)
		{
            Bindings.AddBinding<TContract>(binding, iD);
        }

		public void AddToNonLazy(Binding binding)
		{
            NonLazyBindings.Add(binding);
		}

		public void RemoveBinding(BindingKey key)
		{
            Bindings.Remove(key);
        }

		public void Clear()
		{
            Bindings.Clear();
            SingleInstances.Clear();
            NonLazyBindings.Clear();
            Disposables.Clear();
			Objects.Clear();
            GameObjectsContainer.Clear();
            CleanablesContainer.Clear();
		}
	}
}