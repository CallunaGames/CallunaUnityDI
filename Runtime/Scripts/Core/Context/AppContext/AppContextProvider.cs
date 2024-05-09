using UnityEngine;

namespace SBaier.DI
{
    public class AppContextProvider
    {
        public AppContext FindOrCreateAppContext()
        {
            AppContext appContext = Object.FindObjectOfType<AppContext>();
            if (appContext == null)
                appContext = CreateAppContext();
            return appContext;
        }

        private AppContext CreateAppContext()
        {
            GameObject appContextObject = new GameObject(nameof(AppContext));
            return appContextObject.AddComponent<AppContext>();
        }
    }
}
