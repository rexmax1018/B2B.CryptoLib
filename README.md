# B2B.CryptoLib

B2B.CryptoLib is a .NET 10 library for AES, RSA and ECC operations and for
runtime key-set based text encryption. The runtime library uses instance-based
services internally; Autofac is optional.

## Recommended: static facade

Configure the process default client once with an explicit key-set root and
active unified name:

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
configuration is idempotent; attempting to replace the default key context
with a different configuration throws an exception. Using the facade before
initialization also fails fast. The facade never reads `appsettings.json`,
creates a default `Keys` directory, or selects a key by sorting unified names.
If the configured active key does not exist, encryption fails instead of
silently selecting another key.

`Crypto.Initialize` does not process the `update` directory. When an explicit
key publication is intended, call `Crypto.UpdateKeySetsAsync()` after
initialization.

If `ActiveUnifiedName` is omitted, use the explicit overload instead:

```csharp
var encrypted = Crypto.Encrypt("test", "B2B_20260901");
```

## Isolated client

Use a client when an application needs more than one key root or key context:

```csharp
var crypto = CryptoClient.Create(new CryptoOptions
{
    KeyManagerBasePath = @"D:\B2B\TenantA\Keys",
    ActiveUnifiedName = "tenant-a-key"
});

var encrypted = crypto.Encrypt("value");
var plainText = crypto.Decrypt(encrypted);
```

Each `CryptoClient` owns its `KeyManagerService`, cache, directory context and
active unified name. Creating a client reads existing key sets from `current`
and `history` using the existing precedence rules, but does not scan, publish,
move or consume files in `update`. Call `crypto.UpdateKeySetsAsync()` when an
explicit key publication is intended; this uses the existing
`KeyManagerService.StartAsync()` semantics and invalidates that client's
caches after a successful update.

Multiple clients using the same `KeyManagerBasePath` are not guaranteed to
share update or cache state. Treat same-root clients as unsupported when key
rotation or updates can occur; use one client per root, or use distinct roots
for isolated clients. The isolation guarantee applies to clients with
different key roots.

Available high-level operations include:

```csharp
crypto.Encrypt(value);
crypto.Encrypt(value, unifiedName);
crypto.Decrypt(encryptedValue);
crypto.IsValidEncryptedFormat(encryptedValue);
crypto.GetUnifiedName(encryptedValue);
await crypto.UpdateKeySetsAsync();
```

`IsValidEncryptedFormat` checks only the outer Base64/envelope shape; it does
not authenticate the payload or prove that the current key can decrypt it.

## Optional Autofac integration

Existing DI consumers can continue to use the module:

```csharp
using Autofac;
using B2B.CryptoLib;
using B2B.CryptoLib.Interfaces;

var keyManagerBasePath = @"D:\B2B\Keys";
var builder = new ContainerBuilder();
builder.RegisterModule(new CryptoSuiteModule(keyManagerBasePath));

using var container = builder.Build();
var dataEncryption = container.Resolve<IDataEncryptionService>();
```

`CryptoSuiteModule(string keyManagerBasePath)` remains available, and the
existing `IDataEncryptionService`, `ICryptoService`, `ICryptoKeyService`,
key loaders and `KeyManagerService` registrations remain instance-oriented.
The module also exposes `ICryptoClient`; pass a second `activeUnifiedName` to
`CryptoSuiteModule` when that DI client should support `Encrypt(value)`.
`ICryptoClient.UpdateKeySetsAsync()` is explicit; resolving the client does not
consume `update` files. Applications that use the module do not need
`ICryptoClient` in order to use the existing interfaces.

## Key and ciphertext compatibility

This modernization does not redesign the cryptographic data contract:

- The outer encrypted value remains `Base64(payload).unifiedName`.
- New high-level writes retain the existing GCM v2 payload (`B2BCGCM`, version
  `2`, 12-byte nonce, UTF-8 unified-name AAD and 16-byte authentication tag).
- Payloads without the GCM marker continue through the AES-CBC/PKCS#7 legacy
  reader.
- Both v2 key sets (`.aes`, `.pub`, `.priv`) and legacy key sets (`.der`,
  `.public.pem`, `.private.pem`) remain supported.
- RSA-OAEP and legacy RSA PKCS#1 v1.5 key-material paths remain separate.
- `current`, `history` and `update` processing, cache invalidation and
  unified-name extraction retain their existing behavior.

`GetLatestActiveUnifiedName()` is still available for compatibility, but new
convenience encryption uses only the explicitly configured
`ActiveUnifiedName`.

## Offline key generation

Key generation remains a separate assembly and tool. It continues to use the
legacy `CryptoConfig` APIs and the tool's copied `appsettings.json`; the
runtime `CryptoClient` and `Crypto` facade do not depend on that global
configuration state.

```text
dotnet run --project B2B.CryptoLib.KeyGenTool -- KEYSET B2B_20260901
```

## Target framework and dependencies

All production, tool and test projects target `net10.0` and use SDK-style
projects with `PackageReference` and nullable reference types enabled. .NET
Framework 4.8 is no longer supported. The current crypto dependencies remain
Autofac 6.0.0, Portable.BouncyCastle 1.9.0 and Newtonsoft.Json 13.0.1; unused
direct compatibility references and NLog were removed.

Integration with `B2B_API`, EF Core, Oracle and B2B.Dao mapping is intentionally
deferred to the separate `GOAL-B2B-API-CRYPTOLIB-INTEGRATION` goal.
