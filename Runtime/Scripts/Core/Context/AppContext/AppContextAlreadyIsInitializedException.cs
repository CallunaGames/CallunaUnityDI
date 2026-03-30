using System;

namespace Calluna.DI
{
	[Serializable]
	public class AppContextAlreadyIsInitializedException : ContextAlreadyIsInitializedException
	{
		public AppContextAlreadyIsInitializedException(string contextObjectName) : 
			base(typeof(AppContext), contextObjectName)
		{
		}
	}
}