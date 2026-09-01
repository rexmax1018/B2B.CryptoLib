# Key management

This guide describes the files and operational steps around CryptoLib key
sets. It intentionally contains placeholders only; never place real private
keys, AES material, PEM bodies or decrypted secrets in this document, a commit,
an issue, or a log.

## Directory layout

Configure one protected root for a runtime context:

```text
<key-root>/
    current/
    history/
    update/
```

| Directory | Purpose | Runtime behavior |
| --- | --- | --- |
| `current` | Complete key sets currently available for normal reads and new writes. | Searched first. |
| `history` | Retained complete key sets needed to decrypt older ciphertext. | Searched after `current`. |
| `update` | Staging area for a newly generated complete key set. | Processed only by an explicit update call. |

The runtime recognizes these complete layouts:

| Layout | Files | Material format |
| --- | --- | --- |
| v2 | `<name>.aes`, `<name>.pub`, `<name>.priv` | `.aes` contains RSA-wrapped AES key material; `.pub` and `.priv` contain PEM keys. |
| legacy | `<name>.der`, `<name>.public.pem`, `<name>.private.pem` | `.der` historically uses RSA PKCS#1 v1.5 wrapping and dot-separated AES material; the PEM files are the corresponding public/private keys. |

`unifiedName` is the shared `<name>` without an extension. It must contain only
ASCII letters, numbers, `_` and `-`. Dots, path separators, whitespace and an
empty name are not valid. A key root should be outside the source repository,
web root and any directory writable by an untrusted user.

The key-set generator currently stages the legacy-compatible filenames
`<name>.der`, `<name>.public.pem` and `<name>.private.pem`, while writing the
current OAEP and `Key:IV` material format. `KeyManagerService` keeps a
transitional OAEP/colon fallback for those files. Do not infer the format only
from the extension; preserve the documented reader behavior.

## Key generation

Key generation belongs on a controlled offline host. The tool loads its
`appsettings.json`, creates the configured output directories, and writes
secret-bearing files. Replace the configuration's key directory with a
protected deployment location; the following command uses only a sample name:

```powershell
dotnet run --project .\B2B.CryptoLib.KeyGenTool -- KEYSET sample-20260902
```

The tool accepts `AES`, `RSA`, `ECC` or `KEYSET` as the first argument and an
optional filename/name as the second argument. A `KEYSET` operation generates
an RSA pair, a 256-bit AES key and IV, wraps the AES material with RSA OAEP,
and stages the three files in `update`.

Keep the output directory protected during generation. Restrict access to the
operators and deployment identity, avoid copying the output to a developer
worktree, and remove any failed or abandoned temporary output through the
approved secret-destruction process.

## KEYSET command

The command writes a complete staged group and prints paths in its result. A
successful command is not the same as a runtime publication. Verify the three
expected files exist under the configured `update` directory, check ownership
and permissions, and transfer the group through the approved deployment
channel.

No real key material is needed in the runtime repository. Tests generate
ephemeral keys under their own temporary directories.

## Update staging

Treat `update` as an inbound staging area, not as a directory that requests
are allowed to mutate. A group is identified by its layout and unified name.
Incomplete groups are ignored and remain available for repair or retry.

The staging operation is separate from the runtime manager. Generating files
does not make them active, and constructing `CryptoClient` does not consume
them.

## Explicit publication

Publish only at an intentional deployment point:

```csharp
await client.UpdateKeySetsAsync();
```

or, when the service is managed directly:

```csharp
await keyManager.StartAsync();
```

Publication scans `update`, requires all three files, atomically replaces each
destination in `current`, and deletes the source files only after successful
copying. The order is deliberate:

```text
public key
private key
AES material LAST
```

The AES file is the discovery marker. Writing it last ensures a new group is
not discoverable until its public and private PEM files are present. Temporary
files and atomic replacement ensure a reader sees an old complete destination
or a new complete destination for each file, rather than a partially written
file. The per-instance gate prevents that instance from reading during its
own update.

Do not call the update method on every request. It mutates the filesystem,
consumes staged files and clears caches. Run it as an explicit startup,
deployment or rotation step with suitable operational coordination.

## Current/history lifecycle

`current` is the normal source for both new encryption and decryption. A
unified name that is not complete in `current` can be resolved from a complete
matching group in `history`, allowing old ciphertext to remain readable.

The manager does not automatically archive old `current` files into `history`.
Before replacing or retiring a key set, the deployment process must preserve a
complete historical copy, with the same layout and protected permissions, if
historical decryption is required. Use versioned unified names for rotations
when possible; a current group with the same name takes precedence over a
history group with that name.

## Rotation

A safe rotation sequence is:

1. Generate a new key set with a new, valid unified name on the offline host.
2. Preserve the old complete key set in `history` if older data must remain
   decryptable.
3. Stage the new three-file group under `update`.
4. Verify file ownership, permissions and filenames without printing secrets.
5. Call `UpdateKeySetsAsync` once at the coordinated deployment point.
6. Configure the new name as `ActiveUnifiedName` for new writes.
7. Keep the old historical set until the retention and backup policy allows
   retirement.

Because GCM ciphertext carries its unified name in the outer suffix, decryption
continues to select the historical key by that suffix. It does not use the
new active name to decrypt old data.

## Rollback / historical decrypt

Rollback is a deployment operation, not an automatic behavior. Keep the old
complete group and its backup before a rotation. If a newly staged group must
be withdrawn, stop new writes or point the client at the prior active name,
then coordinate restoration of the prior complete group. A successful update
clears only the cache of the manager that performed it.

Historical decrypt works only while the matching complete group and its private
key remain available. Do not delete a historical private key merely because a
new public key is active.

## Cache invalidation

Each `KeyManagerService` has an instance-local lazy RSA cache and AES cache.
After at least one group publishes successfully, the manager clears both so
the next lookup reads the replacement. A second client that shares the same
root does not receive that invalidation. Coordinate the update across clients,
restart them, or avoid same-root multi-client deployments.

There is no cross-process lock or distributed cache invalidation. File copies,
permissions, deployment ordering and process coordination remain host
responsibilities.

## Backup requirement

Back up complete key sets, not only public keys. A backup needed for historical
decryption must contain the matching AES material, RSA public PEM and RSA
private PEM, preserve its layout and be encrypted and access-controlled by the
organization's secret-management policy.

Test restore procedures on an isolated host. Record unified names and retention
metadata separately from secret contents, and never put backup passphrases or
private key bodies in source control.

## Permissions requirement

The key root, all three subdirectories and every key file should be writable or
readable only by the identities that need that operation. In particular:

- runtime readers need read access to the required current/history files;
- the deployment identity needs controlled write access to `update` and
  `current`;
- ordinary application request identities should not generate or publish keys;
- directory and file permissions should prevent untrusted replacement or
  symlink/path manipulation according to the host operating system policy.

The library validates names and path relationships, but it cannot replace OS
permissions, secret storage, backup encryption or an audited key ceremony.

## Private key protection

PEM private keys and AES material are secrets at rest. Use an encrypted disk,
an access-controlled secret store or the organization's equivalent protection;
limit process and operator access; do not include contents in diagnostics;
and rotate or revoke access according to the retention policy.

Never commit key files, generated JSON containing key bytes, `.der`, `.pub`,
`.priv`, `.public.pem` or `.private.pem` files. If a secret is accidentally
committed, treat it as compromised and follow the incident and key-rotation
process rather than merely deleting the file from a later commit.
