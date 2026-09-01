# B2B.CryptoLib

## Overview

B2B.CryptoLib 2.0.1 is a .NET 10-only library for AES, RSA and ECC operations
and runtime key-set based text encryption. The high-level runtime API does not
require dependency injection; Autofac is an optional integration path.

The runtime uses explicit key contexts. New text ciphertext is randomized by a
fresh GCM nonce, while decryption selects its key from the ciphertext's
`unifiedName` suffix. Key material must never be committed to this repository.

## Installation

Reference the package candidate from the intended feed:

```xml
<PackageReference Include="B2B.CryptoLib" Version="2.0.1" />
```

The package targets `net10.0` only. It declares the runtime dependencies
`Autofac` 9.3.2, `BouncyCastle.Cryptography` 2.7.0 and `Newtonsoft.Json`
13.0.4.

## Quick Start

Configure the process default client once with an explicit protected key-set
root and active unified name:

```csharp
using B2B.CryptoLib;
using B2B.CryptoLib.Models;

Crypto.Initialize(new CryptoOptions
{
    KeyManagerBasePath = @"D:\B2B\Keys",
    ActiveUnifiedName = "B2B_20260901"
});

var encrypted = Crypto.Encrypt("test");
var plainText = Crypto.Decrypt(encrypted);
```

`Crypto.Initialize` is thread-safe. Repeating it with the same normalized
configuration is idempotent; a different configuration is rejected. Using the
facade before initialization fails fast. The facade does not read
`appsettings.json`, create a default `Keys` directory, select the latest key by
sorting names, or consume `update` files.

Key publication is explicit:

```csharp
await Crypto.UpdateKeySetsAsync();
```

## Isolated Client

Use `CryptoClient` when a process needs more than one independent key root or
key context:

```csharp
var client = CryptoClient.Create(new CryptoOptions
{
    KeyManagerBasePath = @"D:\B2B\TenantA\Keys",
    ActiveUnifiedName = "tenant-a-key"
});

var encrypted = client.Encrypt("value");
var plainText = client.Decrypt(encrypted);
```

One client owns one key-manager, cache, directory context and optional active
name. Construction creates `current`, `history` and `update`, but does not
publish or consume staged files. If `ActiveUnifiedName` is omitted, use
`client.Encrypt(value, unifiedName)`. Decrypt always uses the name in the
ciphertext suffix. Separate clients sharing one root do not share cache state
or an update lock; coordinate rotation externally or use one client per root.

## Optional Autofac

Existing Autofac applications can register the runtime module:

```csharp
using Autofac;
using B2B.CryptoLib;

var builder = new ContainerBuilder();
builder.RegisterModule(new CryptoSuiteModule(@"D:\B2B\Keys", "B2B_20260901"));
using var container = builder.Build();
```

`CryptoSuiteModule` registers the runtime services as singletons. It is
optional; direct `CryptoClient` construction is supported. The separate
`KeyGenerationModule` belongs to the offline key-generation tool and should
not be registered in a web/runtime container.

## Key Generation

Key generation is an offline responsibility. The tool reads its copied
`appsettings.json` through the legacy static `CryptoConfig` API and supports
`AES`, `RSA`, `ECC` and `KEYSET` commands:

```powershell
dotnet run --project .\B2B.CryptoLib.KeyGenTool -- KEYSET sample-20260902
```

The output contains secret-bearing AES material and private PEM keys. Keep it
in a protected offline location and transfer only through the approved key
publication process.

## Key Publication

`KEYSET` output is staged under the configured key root's `update` directory.
Only an explicit `Crypto.UpdateKeySetsAsync()` or
`KeyManagerService.StartAsync()` processes it. A complete set is published as
public key, private key, then AES material last; temporary files and atomic
replacement protect readers from partial individual files. Successful update
consumes the staged files and clears the updating client's key caches. Do not
run this filesystem-mutating operation on every request.

See [Key management](docs/KEY-MANAGEMENT.md) for rotation, history, rollback,
backup and permission requirements.

## Security / Query Semantics

- New GCM ciphertext uses a random nonce, so encrypting the same plaintext can
  produce different ciphertext. Do not use encrypted text equality as a
  deterministic database lookup key.
- The outer value is `Base64(payload).unifiedName`; the unified name is also
  authenticated GCM AAD for new payloads.
- `IsValidEncryptedFormat` checks only the outer shape. It does not prove
  authentication, authorization, key existence or decryptability.
- Store key roots outside the source tree, web root and publicly writable
  directories. Never commit `.aes`, `.pub`, `.priv`, `.der`, `.public.pem`,
  `.private.pem` or generated JSON containing key bytes.

## Compatibility

The package identity change does not change the public crypto contract,
ciphertext envelope, GCM v2 payload, key layouts, RSA modes, ECC PEM/signature
path or legacy decrypt behavior. Payloads without the GCM marker continue to
use legacy AES-CBC/PKCS#7 reading.

Read [CRYPTO-COMPATIBILITY.md](docs/CRYPTO-COMPATIBILITY.md) before changing
cryptographic code, key serialization or dependency adapters.

## Offline Packaging

Build, test and pack the exact candidate using the repository's .NET 10
Microsoft Testing Platform selection:

```powershell
dotnet restore B2B.CryptoLib.sln
dotnet build B2B.CryptoLib.sln -c Release --no-restore
dotnet test B2B.CryptoLib.sln -c Release --no-build
dotnet pack .\B2B.CryptoLib\B2B.CryptoLib.csproj -c Release --no-build -o <offline-feed-directory>
```

Use a fresh package cache and only the intended offline source when verifying
the transfer. See [OFFLINE-PACKAGING.md](docs/OFFLINE-PACKAGING.md) for the
full procedure and SHA-256 integrity check.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Key management](docs/KEY-MANAGEMENT.md)
- [Cryptographic compatibility contract](docs/CRYPTO-COMPATIBILITY.md)
- [Offline packaging](docs/OFFLINE-PACKAGING.md)
- [Changelog](CHANGELOG.md)

## Dependencies

Runtime direct dependencies:

- `Autofac` 9.3.2
- `BouncyCastle.Cryptography` 2.7.0, replacing `Portable.BouncyCastle` 1.9.0
- `Newtonsoft.Json` 13.0.4

The test project uses xUnit v3 4.0.0, `Microsoft.NET.Test.Sdk` 18.9.0 and
`xunit.runner.visualstudio` 4.0.0. The production and KeyGeneration projects
emit XML documentation files for IntelliSense and offline development.

## Versioning

Version 2.0.1 is the dependency-modernized package candidate and remains
.NET 10-only. See [CHANGELOG.md](CHANGELOG.md), especially
[Upgrading from 2.0.0 to 2.0.1](CHANGELOG.md#upgrading-from-200-to-201), for
the dependency migration and downstream notes.

The downstream `B2B_API` update from `B2B.CryptoLib` 2.0.0 to 2.0.1 and from
Autofac 9.1.0 to 9.3.2 is a separate follow-up. This CryptoLib-only change
does not modify that repository.
