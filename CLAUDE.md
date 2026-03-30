# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**CallunaUnityDI** is a lightweight, reflection-free dependency injection framework for Unity. Package ID: `com.calluna.di`.

This is one of the most widely referenced packages in the Calluna ecosystem — it is a dependency of nearly every other Calluna Unity package (alongside `com.calluna.core`). Changes here have broad downstream impact, so **performance, clarity, documentation, and test coverage are first-class concerns**.

The design is deliberately Zenject-inspired but intentionally more constrained: fewer ways to do the same thing, less API surface, lower conceptual overhead. When adding features, prefer the simplest API that covers the use case over a flexible one that introduces new patterns.

## Commands

This is a Unity package — there are no CLI build commands. All building and testing is done inside the Unity Editor.

- **Run tests:** Unity Editor → Window > General > Test Runner → Run All (uses NUnit + Moq)
- **Run a single test:** Select the specific test in the Test Runner window and click Run Selected
- **Import package:** Add via Unity Package Manager using the local path or git URL

There is no linting configuration. Follow existing C# conventions in the codebase.

## Architecture

### Context Hierarchy (Three-Tier Scope)

Dependency scopes nest in a strict hierarchy:

```
AppContext  (application scope — DontDestroyOnLoad, script order -10000)
  └── SceneContext  (scene scope — script order -9999, optional parent ID for cross-scene deps)
        └── GameObjectContext  (per-hierarchy scope — script order -9998)
```

Each context holds its own `DIContainer` and resolves through a `ChildResolver` that walks up the hierarchy for unresolved types.

### Core Interfaces

| Interface | Purpose |
|-----------|---------|
| `DIContext` | Root contract: exposes `Resolver` and `Binder`, plus `ValidateBindings()`, `Reset()`, `Clear()` |
| `Binder` | Fluent entry point: `Bind<T>()`, `BindInstance<T>()`, `BindComponent<T>()`, etc. |
| `Resolver` | Lookup: `Resolve<T>()`, `ResolveOptional<T>()`, `IsResolvable()` |
| `Injectable` | Implement to receive injection: `void Inject(Resolver resolver)` |
| `Initializable` | Called after injection: `void Initialize()` |
| `Cleanable` | Called on context reset: `void Clean()` |
| `Installer` | Organizes bindings; base classes are `MonoInstaller` and `ScriptableObjectInstaller` |

### Binding Fluent Chain

```csharp
binder.Bind<IService>()   // contract type
    .To<ServiceImpl>()     // concrete type (or .ToSelf())
    .FromNew()             // creation mode
    .AsSingle()            // cardinality: AsSingle() or AsPerRequest()
    .NonLazy();            // optional: eager instantiation
```

**Creation modes** (`InstanceCreationMode`): `FromNew`, `FromInstance`, `FromMethod`, `FromFactory`, `FromPrefabInstance`, `FromResourcePrefabInstance`, `FromNewComponentOn`, `FromNewComponentOnNewGameObject`, `FromResources`.

**Cardinality** (`InstanceAmountMode`): `Single` (shared) or `PerRequest` (new instance each resolve).

### Resolution Pipeline

1. `Resolver` finds `Binding` for requested type (walks up hierarchy via `ChildResolver`)
2. `DIInstanceFactory` creates instance using binding metadata
3. If `Injectable`, calls `Inject(resolver)`
4. If `Initializable`, calls `Initialize()`
5. Stores in container (`SingleInstancesContainer` or ephemeral)
6. `CircularDependencyDetector` wraps resolution to catch infinite loops

### DIContainers

Specialized storage split by concern: `BindingsContainer`, `SingleInstancesContainer`, `NonLazyContainer`, `DisposablesContainer`, `ObjectsContainer`, `GameObjectsContainer`, `CleanablesContainer`.

### Key Subsystems

- **Pooling** (`Runtime/Scripts/Core/Pooling/`): `MonoPool<T>` / `AbstractMonoPool<T>` — pooling for MonoBehaviours, configured via `MonoPoolInstaller`.
- **Factory** (`Runtime/Scripts/Core/Factory/`): `Factory<T>` interface for `FromFactory` creation mode; `ScopedFactory` creates instances in a child scope.
- **Non-Resolvable Instances**: `CreateNonResolvableInstance()` binding — instantiates without making the type resolvable; must use `.NonLazy()`.
- **Application Quit Detector** (`Runtime/Scripts/Core/ApplicationQuitDetector/`): Guards cleanup during Unity app shutdown.

## Assembly Definitions

| Assembly | Path | Notes |
|----------|------|-------|
| `Calluna.DI` | `Runtime/Scripts/Core/Calluna.DI.asmdef` | Runtime, no external deps |
| `Calluna.DI.Tests` | `Tests~/Calluna.DI.Tests.asmdef` | Refs NUnit, Moq; constrained to `UNITY_INCLUDE_TESTS` |

## Samples

Located in `Samples~/`, each is a self-contained Unity scene: `CircularDependencyInjection`, `ComponentOnNewGameObject`, `NonResolvableInstances`, `Pooling`, `SceneDependencies`, `ScopedFactory`.
