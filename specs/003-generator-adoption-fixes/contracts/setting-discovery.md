# Contract: Setting Discovery For Delegate-Shaped Code Hooks

## Purpose

Define which shell-feature properties become deploy-time settings and which are
ignored as code configuration hooks.

## Included Deploy-Time Settings

The existing supported setting shapes remain included:

- strings, booleans, numeric values, enums
- nullable supported values
- supported date/time, duration, URI, and identifier shapes
- arrays, lists, and dictionaries whose element/value type is supported
- properties enriched by supported manifest setting hints

## Ignored Code Configuration Hooks

The following public settable properties are ignored by default:

- direct delegate-shaped properties, including action callbacks and factory
  callbacks
- properties whose element type is delegate-shaped
- dictionary properties whose value type is delegate-shaped
- nested collection or dictionary shapes that contain delegate-shaped values

Examples:

```csharp
public Action<TOptions>? Configure { get; set; }
public Action<IServiceProvider, HttpClient>? ConfigureHttpClient { get; set; }
public Func<IServiceProvider, TService>? ServiceFactory { get; set; }
public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Factories { get; set; }
```

## Diagnostic Behavior

- Ignored code hooks do not appear in manifest settings.
- Ignored code hooks do not emit unsupported-setting errors.
- Ignored code hooks do not emit warnings by default.
- Verbose diagnostics may identify ignored code hooks and the owning feature.

## Unsupported Settings

Non-delegate complex object settings remain unsupported unless represented by a
supported primitive, enum, nullable, array, list, or dictionary shape. These
properties continue to follow the configured diagnostic severity policy.

## Acceptance Tests

- A feature with delegate hooks and one normal setting generates a manifest that
  includes only the normal setting.
- Direct delegate hooks are ignored without default warnings.
- Delegate-valued dictionaries and collections are ignored without default
  warnings.
- Non-delegate unsupported object settings still produce unsupported-setting
  diagnostics.
