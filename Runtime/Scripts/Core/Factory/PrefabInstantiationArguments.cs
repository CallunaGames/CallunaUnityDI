using UnityEngine;

namespace SBaier.DI
{
    public struct PrefabInstantiationArguments
    {
        public Transform Parent;
        public Vector3? Position;
        public Quaternion? Rotation;
        public Vector3? Scale;
        public bool? WorldPositionStays;
        public bool? FitRectTransform;
        
        public static PrefabInstantiationArguments CreateFittedUIArgs(Transform parent)
        {
            return new PrefabInstantiationArguments()
            {
                Parent = parent,
                FitRectTransform = true,
                Scale = Vector3.one
            };
        }
        
        public static PrefabInstantiationArguments CreateUIArgs(Transform parent)
        {
            return new PrefabInstantiationArguments()
            {
                Parent = parent,
                Scale = Vector3.one
            };
        }
    }
}