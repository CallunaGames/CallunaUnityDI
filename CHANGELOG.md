## [1.2.0] - 2026-03-31

### Breaking Changes
- `CircularDependencyDetector` public class removed. Any code that instantiates, subclasses, or references this type directly will fail to compile. Circular dependency detection is now handled automatically by `DIContextBase`.
- `CircularDependencyException(BindingKey)` constructor removed; replaced by `CircularDependencyException(IReadOnlyList<Type> chain)`. Any code constructing this exception directly must be updated to pass the full type chain instead.

### Improvements
- Circular dependency detection no longer wraps every resolution call in a decorator. Detection now fires only during instance construction, eliminating one virtual-dispatch layer on every `Resolve` call and skipping cached singleton lookups entirely.
- `CircularDependencyException` message now includes the full cycle chain (e.g. `ServiceA → ServiceB → ServiceA`) instead of just the re-entered type, making cycles significantly easier to diagnose.
- Resolution chain is always cleaned up via `try/finally`, preventing false-positive cycle detection after any non-circular exception during injection.

### Bug Fixes
- Restored `nuget.moq` as a package dependency (accidentally dropped in v1.1.0; required by the test assembly).

### Tests
- Added `CircularDependencyTests` covering self-cycles, two- and three-hop cycles, exception message content and ordering, and chain cleanup after an exception.

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
