# NuGet publishing & GitHub Actions — Design

**Date:** 2026-06-25
**Status:** Approved

## Goal

Make `LambdaTale.v3` publishable to NuGet and add GitHub Actions workflows for
build/test (CI) and tag-driven publishing. The repo is currently private and has
no workflows; the library is not yet on NuGet.

## Decisions

- **Versioning:** tag-driven, *lightweight* — version is extracted from the git
  tag in the publish workflow (no MinVer or other build-time dependency).
- **CI OS coverage:** `ubuntu-latest` only (pure managed code, no platform bits).
- **Publish feed:** nuget.org only.
- **GitHub Release:** auto-created on publish from the tag, with generated notes.
- **Authors:** `LambdaTale.v3 Authors` (placeholder, changeable later).
- **Repository URL:** `https://github.com/bbvch/LambdaTale.v3`.

## 1. Package metadata & versioning

### Package metadata — `src/LambdaTale.v3/LambdaTale.v3.csproj`

Add a `<PropertyGroup>` with NuGet packaging metadata:

- `PackageId` = `bbv.LambdaTale.v3` (matches existing `AssemblyName` and README)
- `Title` / `Description` — short BDD-for-xUnit-v3 summary
- `Authors` = `LambdaTale.v3 Authors`
- `PackageLicenseExpression` = `MIT`
- `PackageReadmeFile` = `README.md` (packed from repo root)
- `PackageProjectUrl` / `RepositoryUrl` = `https://github.com/bbvch/LambdaTale.v3`
- `RepositoryType` = `git`
- `PackageTags` = e.g. `xunit;xunit-v3;bdd;testing;scenario;gherkin`
- `IncludeSymbols` = `true`, `SymbolPackageFormat` = `snupkg`

Pack the root `README.md` into the package:

```xml
<None Include="..\..\README.md" Pack="true" PackagePath="\" />
```

### Source Link / reproducibility (built into the .NET 10 SDK)

Add to the lib csproj (or `Directory.Build.props` if shared):

- `PublishRepositoryUrl` = `true`
- `EmbedUntrackedSources` = `true`
- `ContinuousIntegrationBuild` = `true` **only in CI** (set via `-p:` or env in
  the workflows, not hardcoded, so local builds stay fast/deterministic-friendly).

### Versioning

- **Keep** `<Version>0.0.1-alpha.1</Version>` in `Directory.Build.props` as the
  fallback for local and CI builds.
- The publish workflow overrides it from the tag, e.g. tag `v0.0.1-alpha.2` →
  `dotnet pack -p:Version=0.0.1-alpha.2`.
- No MinVer; no `fetch-depth: 0` needed.

## 2. CI workflow — `.github/workflows/ci.yml`

**Triggers:** `pull_request` and `push` to `main`.
**Job:** single `ubuntu-latest`.

Steps:

1. `actions/checkout`
2. `actions/setup-dotnet` (SDK resolved from `global.json`)
3. `dotnet restore --locked-mode` (honors existing `packages.lock.json` files)
4. `dotnet build -c Release --no-restore` (Release treats warnings as errors)
5. `dotnet test -c Release --no-build` (Microsoft.Testing.Platform runner per
   `global.json`)

## 3. Publish workflow — `.github/workflows/publish.yml`

**Trigger:** push of a tag matching `v*`.
**Permissions:** `contents: write` (for creating the GitHub Release).
**Job:** single `ubuntu-latest`.

Steps:

1. `actions/checkout`
2. `actions/setup-dotnet`
3. Derive version: `VERSION=${GITHUB_REF_NAME#v}` → `$GITHUB_ENV`
4. `dotnet restore --locked-mode`
5. `dotnet pack src/LambdaTale.v3 -c Release -p:Version=$VERSION
   -p:ContinuousIntegrationBuild=true -o artifacts` (packs `.nupkg` + `.snupkg`)
6. `dotnet nuget push "artifacts/*.nupkg" --source https://api.nuget.org/v3/index.json
   --api-key ${{ secrets.NUGET_API_KEY }} --skip-duplicate` (symbol `.snupkg`
   pushed alongside)
7. Create GitHub Release for the tag with auto-generated notes, attaching the
   `.nupkg` (e.g. `softprops/action-gh-release` or `gh release create`).

## Prerequisites (manual, by the user)

- Create a `NUGET_API_KEY` repo secret (nuget.org → API Keys, scoped to
  `bbv.LambdaTale.v3` / glob `bbv.LambdaTale.*`).
- To cut a release: `git tag v0.0.1-alpha.2 && git push origin v0.0.1-alpha.2`.

## Out of scope

- CodeQL, lint, spell-check workflows (existed in v2; can be added later).
- Multi-OS CI matrix.
- GitHub Packages feed.
- Automatic version bumping / changelog generation beyond the GitHub Release notes.
