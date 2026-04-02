using System;

namespace Calluna.DI
{
	public class InvalidBindingException : InvalidOperationException
	{
		public InvalidBindingException(string message) : base(message) { }
	}
}