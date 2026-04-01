using UnityEngine;

namespace Calluna.DI
{
    public class AppContextProvider
    {
        private static AppContext _cachedAppContext;

        public AppContext FindOrCreateAppContext()
        {
            if (_cachedAppContext == null)
            {
                _cachedAppContext = Object.FindFirstObjectByType<AppContext>();
                _cachedAppContext ??= CreateAppContext();
            }
            return _cachedAppContext;
        }

        private AppContext CreateAppContext()
        {
            GameObject appContextObject = new GameObject(nameof(AppContext));
            return appContextObject.AddComponent<AppContext>();
        }
    }
}
