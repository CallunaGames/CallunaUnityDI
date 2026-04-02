using System;

namespace Calluna.DI
{
	[Serializable]
	internal class GameObjectContextAlreadyIsInitializedException : ContextAlreadyIsInitializedException
	{
		public GameObjectContextAlreadyIsInitializedException(string contextObjectName) : 
			base(typeof(GameObjectContext), contextObjectName)
		{
		}
	}
}