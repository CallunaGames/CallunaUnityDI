using System;

namespace Calluna.DI
{
    public class MissingBindingException : InvalidOperationException
    {
        public MissingBindingException(string message) : base(message) { }

        public MissingBindingException(MissingBindingException inner, Type requester)
            : base($"{inner.Message} — requested by {requester.Name}") { }
    }
}