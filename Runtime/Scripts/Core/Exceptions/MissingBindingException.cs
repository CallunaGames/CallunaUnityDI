using System;

namespace Calluna.DI
{
    public class MissingBindingException : InvalidOperationException
    {
        public MissingBindingException(string message) : base(message) { }
    }
}