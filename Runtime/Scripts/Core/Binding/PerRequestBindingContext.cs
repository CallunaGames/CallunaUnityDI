namespace Calluna.DI
{
    /// <summary>
    /// Decides whether the context keeps track of the instances a per-request binding creates.
    /// A tracked instance is cleaned (<see cref="Cleanable"/>), disposed (<see cref="System.IDisposable"/>)
    /// or destroyed (components and prefab instances) when the context is reset - and is held by the
    /// context until then, also when the requester no longer uses it. An untracked instance belongs to
    /// the requester alone.
    /// Per-request instances are tracked by default; 2.0.0 will make <see cref="Tracked"/> opt-in.
    /// </summary>
    public class PerRequestBindingContext : LazyModeBindingContext
    {
        public PerRequestBindingContext(BindingArguments arguments) : base(arguments) { }

        public LazyModeBindingContext Tracked()
        {
            _binding.TrackInstances = true;
            return new LazyModeBindingContext(_arguments);
        }

        public LazyModeBindingContext Untracked()
        {
            _binding.TrackInstances = false;
            return new LazyModeBindingContext(_arguments);
        }
    }
}
