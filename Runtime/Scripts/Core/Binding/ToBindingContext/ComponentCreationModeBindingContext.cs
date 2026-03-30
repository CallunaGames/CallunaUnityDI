using UnityEngine;

namespace Calluna.DI
{
	public class ComponentCreationModeBindingContext<TConcrete> : 
		CreationModeBindingContext<TConcrete> where TConcrete : Component
	{
		public ComponentCreationModeBindingContext(BindingArguments arguments) : base(arguments) { }

		public AllowInjectionBindingContext FromNewPrefabInstance(GameObject prefab)
		{
			ValidateHasComponent(prefab);
			_binding.CreationMode = InstanceCreationMode.FromPrefabInstance;
			_binding.ProvideInstanceFunction = () => prefab;
			return new AllowInjectionBindingContext(_arguments);
		}

		public AllowInjectionBindingContext FromNewPrefabInstance(TConcrete prefab)
		{
			_binding.CreationMode = InstanceCreationMode.FromPrefabInstance;
			_binding.ProvideInstanceFunction = () => prefab.gameObject;
			return new AllowInjectionBindingContext(_arguments);
		}

		public AllowInjectionBindingContext FromNewResourcePrefabInstance(string path)
		{
			ValidateResourcePath(path);
			_binding.CreationMode = InstanceCreationMode.FromResourcePrefabInstance;
			_binding.ProvideInstanceFunction = () => path;
			return new AllowInjectionBindingContext(_arguments);
		}

		public AllowInjectionBindingContext FromNewComponentOn(GameObject gameObject)
		{
			_binding.CreationMode = InstanceCreationMode.FromNewComponentOn;
			_binding.ProvideInstanceFunction = () => gameObject.AddComponent<TConcrete>();
			return new AllowInjectionBindingContext(_arguments);
		}

		public AllowInjectionBindingContext FromNewComponentOnNewGameObject(
			string name = "", Transform parent = null, bool worldPositionStays = false)
		{
			name = string.IsNullOrEmpty(name) ? $"{nameof(TConcrete)}Object" : name;
			_binding.CreationMode = InstanceCreationMode.FromNewComponentOnNewGameObject;
			_binding.ProvideInstanceFunction = () =>
			{
				GameObject gameObject = new GameObject(name);
				gameObject.transform.SetParent(parent, worldPositionStays);
				return gameObject.AddComponent<TConcrete>();
			};
			return new AllowInjectionBindingContext(_arguments);
		}

		private static void ValidateResourcePath(string path)
		{
			if(Resources.Load<GameObject>(path) == null) 
				throw new MissingComponentException($"There is no resource of type {typeof(TConcrete)} at path {path}");
		}

		private static void ValidateHasComponent(GameObject gameObject)
		{
			TConcrete target = gameObject.GetComponent<TConcrete>();
			if (target == null)
				throw new MissingComponentException($"There is no component of type {typeof(TConcrete)} on gameObject {gameObject.name}");
		}
	}
}
