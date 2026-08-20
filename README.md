# RefactorAnalyzer

RefactorAnalyzer is a deliberately small Roslyn analyzer and code fix for C#.
Its first rule, `RA0001`, identifies a narrow set of `void` methods that directly
mutate their only reference-type parameter and can safely return that parameter.

```csharp
void Update(User user)
{
    user.Name = "Ala";
}
```

The code fix changes it to:

```csharp
User Update(User user)
{
    user.Name = "Ala";
    return user;
}
```

## Supported in the first version

A diagnostic is reported only when the method:

- returns `void`;
- has a block body and exactly one ordinary parameter;
- receives a reference type;
- directly assigns to an instance field or property of that parameter;
- has no earlier `return` statement;
- is not async, partial, virtual, an override, or an interface implementation.

The analyzer intentionally does not support multiple parameters, value types,
`ref`/`out`/`in` parameters, expression-bodied methods, indirect mutation,
compound assignments, or mutation inside lambdas and local functions. The code
fix does not update call sites.

## Repository layout

- `src/RefactorAnalyzer` — the compiler analyzer and diagnostic definition.
- `src/RefactorAnalyzer.CodeFixes` — the IDE code fix provider.
- `tests/RefactorAnalyzer.Tests` — positive, negative, and code-fix tests.
- `docs/PLAN.md` — the agreed scope and implementation plan.

Both shipping projects target .NET Standard 2.0. The tests target .NET 9.

## Build and test

Install a .NET 9 or newer SDK, then run:

```powershell
dotnet restore RefactorAnalyzer.sln
dotnet test RefactorAnalyzer.sln --configuration Release
```

The analyzer currently contains one informational diagnostic:

| ID | Category | Meaning |
|---|---|---|
| `RA0001` | Design | A supported `void` method directly mutates its sole reference-type parameter and can return it. |

## Current scope

This repository contains the analyzer, code fix, and automated tests. Packaging
as a NuGet package or Visual Studio extension is intentionally left for a later
step.
