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

## Build, test, and package

Install a .NET 9 or newer SDK, then run:

```powershell
dotnet restore RefactorAnalyzer.sln
dotnet test RefactorAnalyzer.sln --configuration Release
dotnet pack src/RefactorAnalyzer.Package/RefactorAnalyzer.Package.csproj --configuration Release
```

The package is written to `artifacts/packages/RefactorAnalyzer.0.1.0.nupkg`.
It contains the analyzer and code-fix assemblies under `analyzers/dotnet/cs`, so
NuGet-enabled C# projects discover them automatically.

## Install a local package

Point `dotnet` at the folder that contains the generated `.nupkg` and add the
package to each C# project that should use the analyzer:

```powershell
dotnet add path/to/CompanyProject.csproj package RefactorAnalyzer `
  --version 0.1.0 `
  --source C:\absolute\path\to\RefactorAnalyzer\artifacts\packages
```

The equivalent project entry is:

```xml
<ItemGroup>
  <PackageReference Include="RefactorAnalyzer" Version="0.1.0" PrivateAssets="all" />
</ItemGroup>
```

When editing the project file directly, register `artifacts/packages` as a
package source in the solution's `NuGet.Config` or restore with the `--source`
option. `PrivateAssets="all"` keeps this development-time dependency from
flowing to consumers of the company project.

The analyzer currently contains one informational diagnostic:

| ID | Category | Meaning |
|---|---|---|
| `RA0001` | Design | A supported `void` method directly mutates its sole reference-type parameter and can return it. |

## Current scope

This repository contains the analyzer, code fix, automated tests, and a local
NuGet packaging project. The package is not published to an external feed.
