namespace Calluna.DI
{
    public class AllowInjectionBindingContext : ArgumentsBindingContext
    {
        public AllowInjectionBindingContext(BindingArguments bindingArguments) : base(bindingArguments) 
        {
            
        }

        public AsBindingContext WithoutInjection()
        {
            _binding.InjectionAllowed = false;
            return new AsBindingContext(_arguments);
        }

        public ArgumentsBindingContext WithInjection()
        {
            _binding.InjectionAllowed = true;
            return new ArgumentsBindingContext(_arguments);
        }
    }
}