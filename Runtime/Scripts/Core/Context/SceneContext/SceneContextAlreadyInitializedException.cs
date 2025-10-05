using System;

namespace Calluna.DI
{
	[Serializable]
	public class SceneContextAlreadyInitializedException : ContextAlreadyInitializedException
	{
		public SceneContextAlreadyInitializedException(string contextObjectName) : 
			base(typeof(SceneContext), contextObjectName)
		{
		}
	}
}