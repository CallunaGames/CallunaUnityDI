namespace Calluna.DI
{
    /// <summary>
    /// Implement to react when the application quits - while every context is still intact, before
    /// any context is cleaned, disposed or destroyed. Use it e.g. to hand state to the persistence.
    /// <para>
    /// The AppContext calls <see cref="HandleQuit"/> once, when quitting can no longer be cancelled
    /// (<c>Application.quitting</c> or its <c>OnApplicationQuit</c>, whichever comes first) - also when
    /// leaving play mode, but not on a crash or a forced kill, and not when a scene is just unloaded.
    /// Child contexts are handled before their parents; within a context, components in the order of
    /// <see cref="Cleanable"/> (parents before children), then the context's instances in reverse
    /// creation order. Untracked per-request instances and <c>FromInstance</c> bindings aren't called.
    /// </para>
    /// <para>
    /// Runs synchronously: no coroutines or awaited tasks. Systems outside the DI that shut down in their
    /// own <c>OnApplicationQuit</c> may already be gone. An exception is logged and doesn't stop the
    /// other handlers.
    /// </para>
    /// </summary>
    public interface QuitHandler
    {
        void HandleQuit();
    }
}
