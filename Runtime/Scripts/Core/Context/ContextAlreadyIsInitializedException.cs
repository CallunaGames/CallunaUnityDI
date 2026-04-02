using System;

namespace Calluna.DI
{
	[Serializable]
	internal abstract class ContextAlreadyIsInitializedException : InvalidOperationException
	{
		public ContextAlreadyIsInitializedException(Type contextType, string contextObjectName) : 
			base($"The {contextType} with name {contextObjectName} has already been initialized")
		{
		}
	}
}