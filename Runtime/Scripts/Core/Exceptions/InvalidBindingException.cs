using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Calluna.DI
{
	public class InvalidBindingException : InvalidOperationException
	{
		public InvalidBindingException(string message) : base(message) { }
	}
}