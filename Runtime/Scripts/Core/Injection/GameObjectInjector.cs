using UnityEngine;

namespace Calluna.DI
{
    public class GameObjectInjector
    {
        public void InjectIntoHierarchy(Transform root, Resolver resolver)
        {
            InjectInto(root, resolver);
            InjectIntoChildren(root, resolver);
        }

        public void InjectIntoContextHierarchy(Transform root, Resolver resolver)
        {
            if (root.TryGetComponent(out GameObjectContext gameObjectContext))
                gameObjectContext.Init(resolver);
            else
                InjectIntoHierarchy(root, resolver);
        }

        private void InjectIntoChildren(Transform root, Resolver resolver)
        {
            foreach (Transform child in root)
                InjectIntoContextHierarchy(child, resolver);
        }

        private void InjectInto(Transform root, Resolver resolver)
        {
            Injectable[] injectables = root.GetComponents<Injectable>();
            foreach (Injectable injectable in injectables)
                InjectInto(injectable, resolver);
        }

        private void InjectInto(Injectable injectable, Resolver resolver)
        {
            if (injectable is Installer)
                return;
            try
            {
                injectable.Inject(resolver);
            }
            catch (MissingBindingException e)
            {
                throw new MissingBindingException(e, injectable.GetType());
            }
        }
    }
}