using System;

namespace Calluna.DI
{
    public class DIContainers : BindingStorage
    {
        public BindingsContainer Bindings { get; }
        public SingleInstancesContainer SingleInstances { get; }
        public NonLazyContainer NonLazyBindings { get; }
        public DisposablesContainer Disposables { get; }
        public ObjectsContainer Objects { get; }
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