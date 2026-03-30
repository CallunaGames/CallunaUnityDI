using System;

namespace Calluna.DI
{
	[Serializable]
	public class GameObjectContextAlreadyIsInitializedException : ContextAlreadyIsInitializedException
	{
		public GameObjectContextAlreadyIsInitializedException(string contextObjectName) : 
			base(typeof(GameObjectContext), contextObjectName)
		{
		}
	}
}