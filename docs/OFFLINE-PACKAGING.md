# Offline packaging

This guide builds and verifies a CryptoLib package candidate before moving it
to an environment without internet access. The commands use placeholders so no
machine-specific path or secret is committed.

## Build candidate

Run from the repository root with the .NET 10 SDK selected by the repository's
`global.json`:

```powershell
dotnet restore B2B.CryptoLib.sln
dotnet build B2B.CryptoLib.sln -c Release --no-restore
dotnet test B2B.CryptoLib.sln -c Release --no-build
```

`global.json` selects the Microsoft Testing Platform runner. The test project
is an executable for xUnit v3, so the solution-level `dotnet test` command
above is the supported full-suite command for this repository. Do not replace
it with a v2-only runner command.

The expected baseline is a warning-free Release build and the complete test
suite passing. Also run the targeted crypto suites when investigating a
dependency change:

```powershell
dotnet test --project .\B2B.CryptoLib.Tests\B2B.CryptoLib.Tests.csproj `
  -c Release `
  --no-build `
  --filter FullyQualifiedName~CryptoServiceBehaviorTests

dotnet test --project .\B2B.CryptoLib.Tests\B2B.CryptoLib.Tests.csproj `
  -c Release `
  --no-build `
  --filter FullyQualifiedName~CryptoSuiteIntegrationTests
```

## Pack

Pack the runtime project after the successful build. Replace the placeholder
with a staging directory, not a source-controlled directory:

```powershell
dotnet pack .\B2B.CryptoLib\B2B.CryptoLib.csproj `
  -c Release `
  --no-build `
  -o <offline-feed-directory>
```

The package is `B2B.CryptoLib.2.0.1.nupkg`. Because the production project
enables XML documentation, inspect the archive and require at least:

```text
lib/net10.0/B2B.CryptoLib.dll
lib/net10.0/B2B.CryptoLib.xml
README.md
B2B.CryptoLib.nuspec
```

The KeyGeneration assembly also emits its XML documentation next to its build
output, although it is not included in the runtime package.

## Integrity

Calculate the hash of the exact package file that will be transferred:

```powershell
$offlineFeed = '<offline-feed-directory>'
Get-FileHash (Join-Path $offlineFeed 'B2B.CryptoLib.2.0.1.nupkg') -Algorithm SHA256
```

Record the resulting hash with the release candidate metadata and verify it
again after transfer. A package rebuilt after documentation or source changes
is a new candidate and needs a new hash.

## Offline feed

Copy the package and every package required by the dependency graph to the
offline feed directory. Register the feed on the offline host with a
machine-appropriate path:

```powershell
dotnet nuget add source '<offline-feed-directory>' --name CryptoLibOffline
```

Use the intended offline source explicitly when restoring. Do not commit a
developer-specific path, NuGet.Config, credential or cache directory.

## Clean restore verification

Validate with a fresh package cache and only the intended source, so an online
global cache cannot mask a missing package. The following is a sample
PowerShell procedure; choose a temporary location permitted by the offline
host:

```powershell
$offlineFeed = (Resolve-Path '<offline-feed-directory>').Path
$offlinePackageCache = Join-Path ([System.IO.Path]::GetTempPath()) 'B2B.CryptoLib-offline-packages'
New-Item -ItemType Directory -Force -Path $offlinePackageCache | Out-Null
$env:NUGET_PACKAGES = $offlinePackageCache

dotnet restore .\B2B.CryptoLib.sln `
  --source $offlineFeed `
  --force-evaluate
```

If the solution's test projects are part of the offline validation, the feed
must also contain their xUnit v3, Microsoft Testing Platform/Test SDK and
adapter packages. Restore with network access disabled or on a genuinely
offline host to prove the source is sufficient. Remove the temporary cache
according to the host's cleanup policy after verification.

## Required transitive dependencies

The runtime package's dependency graph must be available in the offline feed,
including:

```text
Autofac 9.3.2
BouncyCastle.Cryptography 2.7.0
Newtonsoft.Json 13.0.4
```

The package's nuspec is the authoritative dependency declaration. Do not copy
only the runtime DLL and assume the package is self-contained; restore must be
able to resolve the declared graph from the intended source.

## Package and source checks

Before transfer, compare the package metadata against the checked-in project
files, verify that `Portable.BouncyCastle` is absent from the runtime graph,
and confirm that no key files or generated secret material are in the package
or repository. Finish with:

```powershell
git diff --check
git status --short
```

The working tree should contain only intentional source/documentation changes;
build output and package candidates should remain ignored or outside the
repository.
