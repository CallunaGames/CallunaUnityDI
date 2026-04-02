using System;

namespace Calluna.DI
{
	[Serializable]
	internal class AppContextAlreadyIsInitializedException : ContextAlreadyIsInitializedException
	{
		public AppContextAlreadyIsInitializedException(string contextObjectName) : 
			base(typeof(AppContext), contextObjectName)
		{
		}
	}
}