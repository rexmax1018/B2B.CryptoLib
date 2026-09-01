# Changelog

## 2.0.1

### Changed

- Updated direct production dependencies to `Autofac` 9.3.2,
  `Newtonsoft.Json` 13.0.4 and `BouncyCastle.Cryptography` 2.7.0.
- Replaced the deprecated `Portable.BouncyCastle` package identity with
  `BouncyCastle.Cryptography`; the existing Bouncy Castle namespaces and
  runtime crypto paths remain in use.
- Migrated the test project to xUnit v3 4.0.0 with
  `Microsoft.NET.Test.Sdk` 18.9.0, `xunit.runner.visualstudio` 4.0.0 and the
  .NET 10 Microsoft Testing Platform runner selection.
- Enabled XML documentation artifacts for the reusable runtime and
  KeyGeneration assemblies.
- Added the architecture, key-management, cryptographic compatibility and
  offline-packaging guides.

### Compatibility

- Public runtime source usage remains compatible for `Crypto`, `CryptoClient`,
  `IDataEncryptionService`, `ICryptoService` and `CryptoSuiteModule`.
- The ciphertext envelope remains `Base64(payload).unifiedName`.
- GCM v2 retains its `B2BCGCM` marker, version 2, random 12-byte nonce,
  unified-name UTF-8 AAD and 16-byte authentication tag.
- The legacy AES-CBC/PKCS#7 reader remains available for payloads without the
  GCM marker.
- Current and legacy key layouts, RSA OAEP, RSA PKCS#1 v1.5 legacy material,
  ECC PEM/signature behavior, key rotation and cache invalidation are retained.

## 2.0.0

The 2.0.0 entry reflects the changes present in the repository history leading
to merged PR #1: `fce942e` (`feat: modernize CryptoLib runtime and usability`),
`2ee7f2e` (`fix: harden CryptoLib v2 runtime contracts`) and the earlier
`5be1226` (`feat: support legacy key set compatibility`) work that is part of
the merged implementation.

### Changed

- Moved the solution's runtime, key-generation, tool and test projects to
  SDK-style .NET 10 projects with nullable reference types and explicit project
  boundaries.
- Added the process-level `Crypto` facade and isolated `CryptoClient` with
  explicit runtime options and active unified-name semantics.
- Added explicit key publication through `UpdateKeySetsAsync()`/
  `KeyManagerService.StartAsync()` and hardened initialization, path/name
  validation and same-root client behavior.
- Kept Autofac as an optional integration surface through
  `CryptoSuiteModule`, while separating offline generation through
  `KeyGenerationModule` and `KeyGenTool`.
- Added legacy key-set compatibility for `.der`, `.public.pem` and
  `.private.pem` files, legacy RSA PKCS#1 v1.5 AES-material wrapping and the
  AES-CBC fallback used by historical ciphertext.
- Added current/history/update key-set lifecycle, rotation and cache
  invalidation behavior used by the runtime integration tests.

### Compatibility

- Runtime encryption and decryption use the explicit unified-name contract;
  decryption resolves the key from the ciphertext suffix.
- Runtime construction is independent of the legacy static `CryptoConfig`,
  while the offline generator retains that configuration API.

## Upgrading from 2.0.0 to 2.0.1

Application source changes are not required for the existing public runtime
surface:

```text
Crypto
CryptoClient
IDataEncryptionService
ICryptoService
CryptoSuiteModule
```

The primary dependency graph changes are:

```text
Autofac 6.0.0 -> 9.3.2
Newtonsoft.Json 13.0.1 -> 13.0.4
Portable.BouncyCastle 1.9.0
    -> BouncyCastle.Cryptography 2.7.0
```

Applications that direct-pin Autofac should verify that their host resolves a
version compatible with CryptoLib 2.0.1. Applications that directly use the
old Portable.BouncyCastle package should migrate that direct reference to the
new package identity; CryptoLib's existing `Org.BouncyCastle` source namespace
does not require an application-level API rewrite when the application only
uses CryptoLib.

The downstream `B2B_API` candidate will require a separate follow-up update
from `B2B.CryptoLib` 2.0.0 to 2.0.1 and from Autofac 9.1.0 to 9.3.2. That
downstream work is intentionally outside this CryptoLib-only change and no
`B2B_API` files are modified here.
