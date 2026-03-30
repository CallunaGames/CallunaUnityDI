using System;

namespace Calluna.DI
{
	[Serializable]
	public class SceneContextAlreadyIsInitializedException : ContextAlreadyIsInitializedException
	{
		public SceneContextAlreadyIsInitializedException(string contextObjectName) : 
			base(typeof(SceneContext), contextObjectName)
		{
		}
	}
}