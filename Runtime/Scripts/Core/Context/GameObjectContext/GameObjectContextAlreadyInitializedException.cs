using System;

namespace Calluna.DI
{
	[Serializable]
	public class GameObjectContextAlreadyInitializedException : ContextAlreadyInitializedException
	{
		public GameObjectContextAlreadyInitializedException(string contextObjectName) : 
			base(typeof(GameObjectContext), contextObjectName)
		{
		}
	}
}