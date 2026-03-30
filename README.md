# Calluna Unity DI
A lightweight, reflection-free [Unity](https://unity.com/) dependency injection framework with a syntax inspired by [Zenject](https://github.com/modesttree/Zenject) but with less API surface and lower conceptual overhead.

## Motivation
[Zenject](https://github.com/modesttree/Zenject) is a powerful DI framework for Unity, but it comes with significant complexity and many ways to accomplish the same thing. Calluna Unity DI takes the best ideas from Zenject and strips away the rest: one way to do each thing, no reflection, and a predictable fluent binding chain.

Why use dependency injection at all? Unity's built-in approach to dependency inversion is the `SerializeField` attribute, which works for MonoBehaviours in the same scene or prefab. It cannot reference plain C# instances, cross-prefab dependencies, or interface types. Dependency injection solves all of those cases.

## Features

- Reflection-free — no runtime type scanning.
- Fluent binding DSL inspired by Zenject.
- Three-tier scope hierarchy: `AppContext` → `SceneContext` → `GameObjectContext`.
- Installers (`MonoInstaller`, `ScriptableObjectInstaller`) to organise bindings.
- Multiple creation modes: constructor, method, factory, prefab, resources, and more.
- Single or per-request cardinality.
- Object pooling for `MonoBehaviour` types.
- Scoped factories that create instances inside a short-lived child scope.
- Non-resolvable bindings for fire-and-forget instantiation.
- Circular dependency detection.

---

## Context Hierarchy

Dependency scopes nest in a strict three-tier hierarchy:

```
AppContext          (application scope — DontDestroyOnLoad, script order -10000)
  └── SceneContext  (scene scope — script order -9999)
        └── GameObjectContext  (per-hierarchy scope — script order -9998)
```

When a type cannot be resolved in the current scope, resolution walks up to the parent context automatically.

### AppContext
`AppContext : MonoContext`

Attach one `AppContext` to a GameObject in your first scene. It marks itself `DontDestroyOnLoad` and initialises the DI system. Bindings made here are available for the lifetime of the application.

### SceneContext
`SceneContext : MonoContext`

Attach one `SceneContext` per scene. It finds or creates an `AppContext` on `Awake` and registers itself as the parent scope for any `GameObjectContext` in the scene.

**Cross-scene dependencies** — set `Parent Context ID` on the child `SceneContext` to the `ID` of another `SceneContext` to resolve types from that scene.

### GameObjectContext
`GameObjectContext : MonoContext`

Attach to any GameObject to create a local scope. Injection is limited to the GameObject's hierarchy. Multiple `GameObjectContext` instances per scene are allowed, including nested ones.

### MonoContext (base class)
`abstract MonoContext : MonoBehaviour, Context`

All three context MonoBehaviours share this base class. The one public member of interest is:

| Member | Description |
|--------|-------------|
| `bool IsInitialized` | `true` after `Init()` has completed. |

---

## Core Interfaces

| Interface | Purpose |
|-----------|---------|
| `DIContext` | Exposes `Resolver` and `Binder` for the current scope. |
| `Binder` | Fluent entry point for all binding operations. |
| `Resolver` | Looks up instances by type. |
| `Injectable` | Implement to receive injection: `void Inject(Resolver resolver)`. |
| `Initializable` | Called after all injection in the scene is complete: `void Initialize()`. |
| `Cleanable` | Called when the context resets: `void Clean()`. |
| `Installer` | Groups related bindings; base classes are `MonoInstaller` and `ScriptableObjectInstaller`. |
| `Context` | Implemented by all three context MonoBehaviours. |

---

## Binding

Use the `Binder` inside an installer's `InstallBindings` method to declare how types are created and resolved.

### Binder entry points

| Method | Use when |
|--------|----------|
| `Bind<TContract>()` | Contract type differs from (or is an interface of) the concrete type. |
| `BindToSelf<T>()` | Contract and concrete type are the same. |
| `BindToNewSelf<T>()` | Shorthand for `BindToSelf<T>().FromNew()` — `T` must have a parameterless constructor. |
| `BindInstance<T>(instance)` | You already hold an instance. |
| `BindComponent<T>()` | `T` is a `Component` — unlocks component-specific creation modes. |
| `BindObject<T>()` | `T` is a `UnityEngine.Object` — unlocks `FromResources`. |

### Binding fluent chain

```
Bind<TContract>()
  [.And<TContract2>() ...]      // bind additional contract types to the same concrete
  .To<TConcrete>()              // or: .ToNew<T>() / .ToComponent<T>() / .ToObject<T>() / .ToInstance(x)
  .FromNew()                    // creation mode (see table below)
  [.WithArgument<TArg>(value)]  // pass arguments to be resolved as dependencies
  [.WithInjection() | .WithoutInjection()]
  .AsSingle()                   // or: .PerRequest()
  [.NonLazy()]                  // optional: create immediately, do not wait for first resolve
```

### Creation modes

| Method | Description |
|--------|-------------|
| `FromNew()` | Calls the parameterless constructor of the concrete type. |
| `FromInstance(instance)` | Uses a pre-existing instance. Injection is off by default; call `.WithInjection()` to enable it. |
| `FromMethod(Func<T>)` | Calls a delegate to create the instance. |
| `FromFactory()` | Delegates creation to a bound `Factory<T>`. |
| `FromNewPrefabInstance(prefab)` | Instantiates a prefab and returns the component. (`BindComponent` only) |
| `FromNewResourcePrefabInstance(path)` | Loads a prefab from Resources and instantiates it. (`BindComponent` only) |
| `FromNewComponentOn(gameObject)` | Adds the component to an existing GameObject. (`BindComponent` only) |
| `FromNewComponentOnNewGameObject(name, parent)` | Creates a new GameObject and adds the component to it. (`BindComponent` only) |
| `FromResources(path)` | Loads an asset directly from Resources. (`BindObject` only) |

### Cardinality

| Method | Description |
|--------|-------------|
| `AsSingle()` | One shared instance; created once and cached. |
| `PerRequest()` | New instance every time the type is resolved. |

### Usage

```csharp
public class MyInstaller : MonoInstaller
{
    [SerializeField] private EnemyView _enemyPrefab;

    public override void InstallBindings(Binder binder)
    {
        // Bind an interface to a concrete class, created with new()
        binder.Bind<IScoreService>()
            .ToNew<ScoreService>()
            .AsSingle();

        // Bind an already-created instance (no injection by default)
        binder.BindInstance<IConfig>(_config);

        // Bind a concrete type to itself
        binder.BindToSelf<AudioManager>()
            .FromNew()
            .AsSingle();

        // Bind two contracts to one concrete type
        binder.Bind<IEnemy>()
            .And<IDamageable>()
            .ToComponent<Enemy>()
            .FromNewPrefabInstance(_enemyPrefab)
            .PerRequest();

        // Use FromMethod when construction requires parameters
        binder.BindToSelf<Pathfinder>()
            .FromMethod(CreatePathfinder);
    }

    private Pathfinder CreatePathfinder()
    {
        Grid grid = _resolver.Resolve<Grid>();
        return new Pathfinder(grid);
    }
}
```

### Passing build-time arguments

Use `WithArgument` to supply a value that the created instance (or its factory) should receive as a resolved dependency:

```csharp
binder.BindToNewSelf<Spawner>()
    .WithArgument(maxSpawnCount)
    .AsSingle();
```

### Injection control

By default, created instances are injected automatically if they implement `Injectable`. You can override this per binding:

```csharp
// Turn injection off for a FromMethod binding
binder.BindToSelf<MyClass>()
    .FromMethod(Create)
    .WithoutInjection()
    .AsSingle();

// Turn injection on for a FromInstance binding (off by default)
binder.BindInstance(myInstance)
    .WithInjection();
```

---

## Installers

Installers group related bindings and are the standard extension point.

### MonoInstaller
`abstract MonoInstaller : MonoBehaviour, Installer`

Attach to a GameObject in the same hierarchy as a context. Add it to the context's `Mono Installers` list in the Inspector.

```csharp
public class GameInstaller : MonoInstaller
{
    public override void InstallBindings(Binder binder)
    {
        binder.Bind<IPlayerService>().ToNew<PlayerService>().AsSingle();
    }
}
```

### ScriptableObjectInstaller
`abstract ScriptableObjectInstaller : ScriptableObject, Installer`

Create as an asset. Add it to the context's `Scriptable Object Installers` list in the Inspector. Useful for data-driven configuration.

```csharp
[CreateAssetMenu(menuName = "Installers/Config Installer")]
public class ConfigInstaller : ScriptableObjectInstaller
{
    [SerializeField] private GameConfig _config;

    public override void InstallBindings(Binder binder)
    {
        binder.BindInstance<IGameConfig>(_config).WithInjection();
    }
}
```

---

## Resolving

Implement `Injectable` on any class (MonoBehaviour or plain C#) to receive its dependencies automatically when the context initialises.

```csharp
public class PlayerController : MonoBehaviour, Injectable
{
    private IScoreService _scoreService;

    public void Inject(Resolver resolver)
    {
        _scoreService = resolver.Resolve<IScoreService>();
    }
}
```

### Resolver API

| Method | Description |
|--------|-------------|
| `Resolve<T>()` | Returns the bound instance; throws if not found. |
| `Resolve<T>(IComparable id)` | Resolves a binding with a specific ID. |
| `ResolveOptional<T>()` | Returns the bound instance or `default` if not found. |
| `ResolveOptional<T>(IComparable id)` | Optional resolve with an ID. |
| `IsResolvable(BindingKey key)` | Returns `true` if the key has a registered binding. |

### Initializable

Implement `Initializable` to receive a callback after all injection in the scene has completed.

```csharp
public class GameManager : MonoBehaviour, Injectable, Initializable
{
    private IScoreService _scoreService;

    public void Inject(Resolver resolver)
    {
        _scoreService = resolver.Resolve<IScoreService>();
    }

    public void Initialize()
    {
        // All objects have been injected; safe to call resolved services here.
        _scoreService.Reset();
    }
}
```

### Cleanable

Implement `Cleanable` to receive a callback when the context resets (e.g. on scene unload or application quit).

```csharp
public class AnalyticsService : IDisposable, Injectable, Cleanable
{
    public void Inject(Resolver resolver) { /* ... */ }

    public void Clean()
    {
        // Release resources when the context tears down.
    }
}
```

---

## Non-Resolvable Instances

A non-resolvable binding instantiates an object and injects it, but does not register it as a resolvable type. Use this when you want injection side-effects (for example, a MonoBehaviour that listens to events) without exposing the type for resolution elsewhere.

Call `.AsNonResolvable()` at the end of the binding chain. The instance is always created eagerly (equivalent to `NonLazy`).

```csharp
public class NonResolvableInstaller : MonoInstaller
{
    [SerializeField] private Bar _barPrefab;

    public override void InstallBindings(Binder binder)
    {
        binder.BindComponent<Bar>()
            .FromNewPrefabInstance(_barPrefab)
            .AsNonResolvable();
    }
}
```

See the *Non Resolvable Instances* sample for a full scene example.

---

## Factory

Factories let you create instances on demand after the context has initialised — for example in a spawner.

### Factory interfaces

```
Factory<TInstance>               — Create()
Factory<TInstance, TArg>         — Create(TArg arg)
Factory<TInstance, TArg1, TArg2> — Create(TArg1 arg1, TArg2 arg2)
```

### Usage

Implement the interface, bind it with `FromFactory`, and bind the factory itself:

```csharp
// 1. Declare the factory
public class EnemyFactory : Injectable, Factory<Enemy>
{
    private Resolver _resolver;
    private EnemyView _prefab;

    public void Inject(Resolver resolver) => _resolver = resolver;

    public Enemy Create()
    {
        Enemy enemy = new Enemy();
        (enemy as Injectable)?.Inject(_resolver);
        return enemy;
    }
}

// 2. Bind in an installer
binder.Bind<Factory<Enemy>>()
    .ToNew<EnemyFactory>()
    .AsSingle();

binder.BindToSelf<Enemy>()
    .FromFactory()
    .PerRequest();

// 3. Resolve and use
public class Spawner : Injectable
{
    private Factory<Enemy> _factory;

    public void Inject(Resolver resolver)
    {
        _factory = resolver.Resolve<Factory<Enemy>>();
    }

    public void Spawn() => _factory.Create();
}
```

### PrefabFactory

`PrefabFactory<TPrefab>` is a ready-made factory for instantiating prefabs. It is wired up automatically by `MonoPoolInstaller` and can also be used standalone.

```csharp
binder.Bind<Factory<BulletView>>()
    .ToNew<PrefabFactory<BulletView>>()
    .WithArgument(_bulletPrefab);
```

`PrefabInstantiationArguments` controls where and how the prefab is placed:

```csharp
PrefabInstantiationArguments args = new PrefabInstantiationArguments
{
    Parent = _container,
    Position = spawnPoint
};
factory.Create(args);

// Helpers for UI prefabs:
PrefabInstantiationArguments.CreateUIArgs(parent);
PrefabInstantiationArguments.CreateFittedUIArgs(parent);
```

---

## Scoped Factory

A scoped factory creates each product inside a short-lived child `DIContext`. This lets you inject per-product dependencies (data, IDs, state) alongside context-wide ones, without polluting the parent scope.

Extend `ScopedFactory<T>` (or `ScopedFactory<T, TArgument>`) and override `InitScope` to add the per-product bindings:

```csharp
// Scoped factory for Foo — no per-call argument
public class FooFactory : ScopedFactory<Foo>
{
    protected override void InitScope(Resolver resolver, Binder binder)
    {
        // Bindings added here are only visible to this Foo instance.
        binder.BindInstance("some-per-instance-id");
    }
}

// Scoped factory for Bar — receives a Foo argument per call
public class BarFactory : ScopedFactory<Bar, Foo>
{
    protected override void InitScope(Resolver resolver, Binder binder)
    {
        // The Foo argument is automatically bound and available to Bar via Inject().
    }
}
```

Bind the factory like any other:

```csharp
binder.Bind<Factory<Foo>>()
    .ToNew<FooFactory>()
    .AsSingle();

binder.BindToSelf<Foo>()
    .FromFactory()
    .PerRequest();

binder.Bind<Factory<Bar, Foo>>()
    .ToNew<BarFactory>()
    .AsSingle();
```

See the *Scoped Factory* sample for a complete scene example.

---

## Object Pooling

The pooling system reuses `MonoBehaviour` instances instead of instantiating and destroying them on every request.

### Class hierarchy

```
Pool<TItem>                               (interface — Request() / Return(item))
Pool<TItem, TArg>                         (interface — Request(arg) / Return(item))
MonoPool<TItem> : Pool<TItem>, Pool<TItem, PrefabInstantiationArguments>
MonoPool<TItem, TArg> : Pool<TItem, TArg>, Pool<TItem, TArg, PrefabInstantiationArguments>
AbstractPoolItem<TComparable>             (interface — ItemId)
AbstractMonoPool<TItem, TComparable>      (multi-prefab pool keyed by ID)
AbstractMonoPool<TItem, TComparable, TArg>
```

### MonoPool — single-prefab pool

Use `MonoPoolInstaller<TItem>` to set up a pool with one prefab in the Inspector:

```csharp
// Attach MonoPoolInstaller<BulletView> to a GameObject and assign the prefab.
// Then resolve the pool anywhere in the same scope:
public class GunController : Injectable
{
    private Pool<BulletView> _pool;

    public void Inject(Resolver resolver)
    {
        _pool = resolver.Resolve<Pool<BulletView>>();
    }

    public void Fire()
    {
        BulletView bullet = _pool.Request();
        // configure bullet ...
    }

    public void OnBulletExpired(BulletView bullet)
    {
        _pool.Return(bullet);
    }
}
```

Use `MonoPoolInstaller<TItem, TArgument>` when each requested item needs an argument at creation time:

```csharp
// Pool<EnemyView, EnemyData>
EnemyView enemy = _pool.Request(enemyData);
```

### AbstractMonoPool — multi-prefab pool keyed by ID

Use when you have several prefab variants and want to request one by an identifier.

Items must implement `AbstractPoolItem<TComparable>`:

```csharp
public class EnemyView : MonoBehaviour, AbstractPoolItem<EnemyType>
{
    public EnemyType ItemId => _type;
    [SerializeField] private EnemyType _type;
}
```

Attach `AbstractMonoPoolInstaller<EnemyView, EnemyType>` to a GameObject and assign the prefab array in the Inspector.

```csharp
Pool<EnemyView, EnemyType> pool = resolver.Resolve<Pool<EnemyView, EnemyType>>();
EnemyView enemy = pool.Request(EnemyType.Ranged);
pool.Return(enemy);
```

See the *Pooling* sample for a complete scene example.

---

## Application Quit Detector

`QuitDetector` is an abstract `MonoBehaviour` that fires a callback when the application quits, allowing cleanup code to be skipped or handled gracefully during teardown.

`ApplicationQuitDetector : QuitDetector` — the concrete implementation registered by the framework automatically via `QuitDetectorInstaller`.

### Public API

| Member | Type | Description |
|--------|------|-------------|
| `IsQuitting` | `bool` | `true` after `Application.quitting` fires. |
| `OnQuit` | `event Action` | Raised once when the application is about to quit. |

```csharp
public class CleanupService : Injectable
{
    private QuitDetector _quitDetector;

    public void Inject(Resolver resolver)
    {
        _quitDetector = resolver.Resolve<QuitDetector>();
        _quitDetector.OnQuit += HandleQuit;
    }

    private void HandleQuit()
    {
        if (_quitDetector.IsQuitting) return; // skip cleanup
    }
}
```

---

## Samples

The following samples are importable via the Unity Package Manager.

| Sample | Description |
|--------|-------------|
| **Circular Dependency Injection** | Shows how the framework detects and reports circular dependencies at runtime. |
| **Component On New Game Object** | Demonstrates `FromNewComponentOnNewGameObject` — binding a MonoBehaviour that is added to a freshly created GameObject. |
| **Non Resolvable Instances** | Demonstrates `.AsNonResolvable()` — instantiating and injecting objects that are not registered for resolution. |
| **Pooling** | Full scene using `MonoPoolInstaller` and the `Pool<T>` / `Pool<T,TArg>` interfaces. |
| **Scene Dependencies** | Shows how to wire a `SceneContext` to another scene's context using the Parent Context ID field. |
| **Scoped Factory** | Shows `ScopedFactory<T>` and `ScopedFactory<T, TArgument>` with per-product scope bindings. |

---

## General Scene Setup

A typical hierarchy looks like this:

```
AppContext (DontDestroyOnLoad)
  └── [AppInstallers...]
Scene Root
  ├── SceneContext
  │     └── [SceneInstallers...]
  ├── GameObjectContext
  │     └── [GameObjectInstallers...]
  └── [Other GameObjects with Injectable MonoBehaviours]
```

1. **AppContext** — add to the first scene. It persists across scene loads. Use it for application-wide services.
2. **SceneContext** — one per scene. Use it for scene-local services and for injecting into all root GameObjects in the scene.
3. **GameObjectContext** — one per prefab hierarchy that needs its own scope. Injection is limited to that hierarchy.
