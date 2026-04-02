using System;

namespace Calluna.DI
{
	[Serializable]
	internal class SceneContextAlreadyIsInitializedException : ContextAlreadyIsInitializedException
	{
		public SceneContextAlreadyIsInitializedException(string contextObjectName) : 
			base(typeof(SceneContext), contextObjectName)
		{
		}
	}
}