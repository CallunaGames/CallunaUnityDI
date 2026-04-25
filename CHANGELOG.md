## [1.4.0] - 2026-04-24

### Breaking Changes
- `ScopedFactory<T>` no longer requires `T : new()`. `Create()` now resolves `T` through the child container's `Resolver` instead of calling `new T()` directly. Callers must bind `T` (and any of its dependencies) inside `InitScope`; failing to do so will produce a `MissingBindingException` at runtime.
- Approximately 20 additional types have been narrowed from `public` to `internal`, beyond the handful listed in the 1.3.0 entry. The 1.3.0 entry named the most prominent removals; the full set of affected commits moved roughly 40 types total across both releases. Any external code that references types not covered by the 1.3.0 entry by name (variable declarations, casts, `typeof`, subclassing) will no longer compile; rely on the documented public interfaces instead.

### Fixed
- `BasicFactory` now calls `Initialize()` on created instances that implement `Initializable`. Previously, only `Inject()` was called, leaving factory-created objects in an uninitialised state if they relied on `Initialize()` for post-injection setup.
- `AppContext` now resets `SceneContext`s in dependency order on application quit, ensuring that a child context is always cleaned up before the parent it depends on. Previously, contexts could be torn down in an arbitrary order, causing cleanup errors when a scene accessed bindings from an already-reset parent context.

## [1.3.0] - 2026-04-02

### Breaking Changes
- `DIContext` interface changed from `public` to `internal`. External code that references `DIContext` by name (e.g. variable declarations, casts, `typeof`) will no longer compile; rely on the concrete context types or the public `Resolver`/`Binder` interfaces instead.
- `DIContext` interface gained a new `void PostInit()` member. Any external class that explicitly implements `DIContext` must add a `PostInit()` implementation.
- `ChildDIContext` changed from `public` to `internal` and its public `Parent` property was removed. Code that holds a `ChildDIContext` reference or reads `Parent` must be removed or rewritten.
- `GameObjectContextInstaller` public class deleted. Remove any scene components or code that reference this installer; its functionality is no longer required.
- `SceneContextInstaller` changed from `public` to `internal` and its constructor no longer accepts a `DIContext` parameter. External instantiation of this class is no longer possible.
- `NonResolvableContext` public class removed. Any code that instantiates or references this type must be deleted.
- `DeepObjectActivator` public class removed. Use `BasicObjectActivator` or a custom `ObjectActivator` implementation instead.
- `MissingSceneContextException` and `MultipleSceneContextsException` public exception types removed. Catch sites targeting these types must be updated or removed.

### Added
- Components that implement `Initializable` and are resolved lazily at mid-runtime (after the initial `PostInit()` phase) now have `Initialize()` called automatically. Previously, lazy-resolved Components never received `Initialize()`.

### Fixed
- `ScopedFactoryBase.DoCreation()` now creates a fresh child context on every call, preventing state from leaking between successive `Create()` calls. The child context is also cleared in a `try/finally` block, ensuring cleanup even when an exception is thrown during factory execution.
- `MissingBindingException` now includes the requester type name for all injection paths. Previously, installer injection in `MonoContext` and instance-creation in `DIContextBase` threw the exception without the `— requested by <TypeName>` suffix that other paths already produced.

### Performance
- `AppContextProvider` caches the located `AppContext` instance after the first scene scan, eliminating repeated `FindObjectsByType` calls on every subsequent lookup.

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
