using UnityEngine;

namespace Calluna.DI
{
    public class AppContextProvider
    {
        public AppContext FindOrCreateAppContext()
        {
            AppContext appContext = Object.FindFirstObjectByType<AppContext>();
            appContext ??= CreateAppContext();
            return appContext;
        }

        private AppContext CreateAppContext()
        {
            GameObject appContextObject = new GameObject(nameof(AppContext));
            return appContextObject.AddComponent<AppContext>();
        }
    }
}
