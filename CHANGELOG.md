## [1.1.0] - 2026-03-31

### Breaking Changes
- `QuitDetector.ApplicationIsQuitting` removed; use `QuitDetector.IsQuitting` instead.
- `MonoContext.Initialized` removed; use `MonoContext.IsInitialized` instead.
- `ContextAlreadyInitializedException` and its four concrete subclasses (`AppContextAlreadyInitializedException`, `GameObjectContextAlreadyInitializedException`, `SceneContextAlreadyInitializedException`) removed; the replacements insert `Is` before `Initialized` (e.g. `ContextAlreadyIsInitializedException`, `AppContextAlreadyIsInitializedException`, etc.).
- `MonoContext.CreateContextAlreadyInitializedException()` protected abstract method renamed to match the new exception class names.
- `SingleInstancesContainer.Has()` and `Get<T>()` removed; replace both with the single `TryGet<T>()` call.
- `DIContainers.NonLazyInstanceInfos` renamed to `NonLazyBindings`; `DisposablesContainer` renamed to `Disposables`; `ObjectsContainer` renamed to `Objects`.
- `Bootstrapper.CreateBasicDIContextResolver()` is now `private`; it was never intended as public API and must not be called from outside the package.
- `InstanceCreationMode` enum values 3–9 changed from power-of-two values to sequential integers. Any code that serialises or compares raw integer values of `InstanceCreationMode` must be updated.
- `Calluna.DI` assembly definition no longer sets `autoReferenced = true`. Projects that relied on the assembly being referenced automatically must add an explicit assembly reference to `Calluna.DI`.

### Added
- `MissingBindingException` now propagates a full requester chain across the injection hierarchy. The message appends `— requested by <TypeName>` at each injection point, including cross-context resolution through `ChildResolver`, making binding errors significantly easier to diagnose.

### Fixed
- `AppContextAlreadyIsInitializedException` was incorrectly passing `typeof(GameObjectContext)` to its base constructor instead of `typeof(AppContext)`.
- `SceneContextProvider.ValidateRemove` was throwing `AlreadyAddedException` instead of `NotAddedException` when the scene being removed had never been added.

### Performance
- `BindingsContainer.GetBinding` collapsed to a single `TryGetValue` lookup, eliminating a redundant dictionary probe.
- `SingleInstancesContainer` resolution collapsed from three dictionary lookups to one via the new `TryGet<T>()` method.
- `MonoPoolCache.Take` reuses a single `TryGetValue` result, removing a second lookup on every pool acquire.
- `DIContextBase.GetResolverFor` short-circuits when no arguments are present, skipping `ArgumentsResolver` allocation in the common (argument-free) case.
- `GameObjectInjector` hierarchy walk replaced with `foreach (Transform child in root)`, eliminating boxing on every child transform.
- Redundant `TryAddToCreated` call in `DIContextBase.CreateNonLazyInstance` removed.
