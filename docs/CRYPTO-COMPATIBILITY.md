# Cryptographic Compatibility Contract

Read this document before changing CryptoLib cryptographic code, key handling,
serialization, dependency adapters or generated file names. A package update
is safe only when the contracts below remain unchanged and the regression
fixtures continue to pass.

## Ciphertext envelope

High-level text encryption is represented as:

```text
Base64(payload).unifiedName
```

The final suffix is both the key-set lookup name and authenticated context for
new GCM payloads. It is not a signature by itself. The outer string's shape
can be inspected with `IsValidEncryptedFormat`, but that method does not
authenticate or decrypt the payload.

## GCM v2 payload

New high-level writes use the following byte layout:

| Offset | Size | Field | Contract |
| ---: | ---: | --- | --- |
| 0 | 7 | Magic | ASCII `B2BCGCM` |
| 7 | 1 | Version | Byte value `2` |
| 8 | 12 | Nonce | Fresh random nonce for this encryption |
| 20 | Remaining | Ciphertext and tag | AES-GCM ciphertext followed by a 16-byte tag |

The payload is Base64 encoded for the outer string. There is no deterministic
nonce or plaintext-derived value in this envelope. The same plaintext and
unified name therefore do not guarantee the same ciphertext.

The tag is part of the final ciphertext bytes. A payload that is too short,
has an unsupported version, or fails tag verification is rejected. The magic
and version are format discriminators, not a substitute for authentication.

## AAD

The `unifiedName` suffix is encoded as UTF-8 and supplied as GCM additional
authenticated data. It is not encrypted, but it is bound to the ciphertext.
Changing the suffix to another valid key name causes the reader to select a
different key and AAD; a genuine GCM payload then fails authentication rather
than silently decrypting under the new name.

This design is intentionally not a deterministic database lookup scheme. Do
not use encrypted text equality as an index or assume repeated values have
repeated ciphertext. Store the unified name separately when an application
needs an explicit lookup or routing key.

## Legacy CBC fallback

Payloads without the `B2BCGCM` marker use the legacy AES-CBC path with PKCS#7
padding. This branch exists only for backward compatibility with ciphertext
written before GCM v2. It must remain available while historical data may still
be read.

The fallback is not a license to create new CBC data. New writes remain GCM
v2. Do not remove, reorder or reinterpret the no-marker fallback without a
planned format migration and a major-version decision.

## RSA OAEP

The current RSA data path uses OAEP with the current `.aes`/`.pub`/`.priv`
contract. OAEP is used by `CryptoService` for current RSA wrapping and by the
current key-set generation flow for newly generated AES material.

Changing OAEP parameters, PEM parsing, key selection or wrapped material
encoding can make existing key sets unreadable. Test current key-set fixtures
and round trips before any change.

## RSA PKCS#1 legacy

The historical `.der` key-set path uses RSA PKCS#1 v1.5 to wrap AES material,
with the corresponding legacy dot-separated material format. This path is
implemented by `LegacyKeySetCrypto` and is deliberately separate from the
current OAEP path; the two wrapping modes are not interchangeable.

The reader also retains a narrowly scoped transitional fallback for `.der`
files produced by an intermediate version that used OAEP and colon-separated
material. This compatibility behavior is part of the existing reader and must
not be removed casually.

## ECC

ECC is the signature/verification path, not the high-level text encryption
path. The offline generator supports NIST P-256, NIST P-384, NIST P-521 and
secp256k1. Keys are serialized as PEM in the established model format, and the
existing signature path uses SHA-256 with ECDSA.

Curve identifiers, PEM labels, private/public key roles and signature
algorithm selection are all compatibility inputs. A curve or PEM change must
be validated against existing fixtures and consumers before release.

## Key layouts

The runtime recognizes complete groups, not individual files:

| Layout | Required files | Meaning |
| --- | --- | --- |
| v2 | `<name>.aes`, `<name>.pub`, `<name>.priv` | RSA-wrapped AES material plus RSA public/private PEM. |
| legacy | `<name>.der`, `<name>.public.pem`, `<name>.private.pem` | Legacy RSA-wrapped material plus the corresponding PEM pair. |

The group can appear in `current`, `history` or `update`. `update` is staging
until an explicit publication. The AES file is the discovery marker during
publication, so it is written last. A filename extension alone must not be
used to discard the reader's transitional compatibility path.

## UnifiedName

`unifiedName` contains only letters, numbers, `_` and `-`, with no dot. It is
used as a filesystem-safe key-set identifier, the outer ciphertext suffix and
the UTF-8 GCM AAD. It is not a secret and should not contain path syntax.

Decryption selects the key set from the ciphertext suffix. `ActiveUnifiedName`
only selects the default key for new encryption; it does not replace the
suffix and does not control historical decryption.

## Rotation compatibility

Rotation should use a new unified name, retain the old complete group in
`history`, stage all three new files and then invoke one explicit update. The
manager searches `current` before `history` and clears its own caches after a
successful publication. It does not automatically move old current files to
history and does not coordinate other clients or processes sharing the root.

GCM v2 and legacy ciphertext remain readable as long as the suffix-matching
historical key set and private material are retained. Never test a rotation by
deleting the only key needed for old data.

## Regression fixtures

The compatibility suite includes tests for:

- AES-CBC round trips and a fixed known-answer vector;
- GCM v2 round trips, unsupported versions and tampered payloads;
- legacy `.der`/PEM key-set loading and legacy AES-CBC decryption;
- current and historical key lookup;
- old and new key versions after rotation;
- cross-decryption of new and legacy key sets;
- unified-name AAD binding;
- incomplete update groups and update-file consumption;
- cache invalidation when a unified name is replaced;
- RSA and ECC signature verification and altered data/signatures.

Keep these tests and any externally stored ciphertext fixtures when upgrading
cryptographic dependencies. A passing build alone is not evidence of format
compatibility.

## Changes requiring a major version

Any change that alters one of the following is a cryptographic format or key
contract change, not an ordinary dependency update:

```text
ciphertext envelope
magic
version
nonce/tag layout
AAD
padding
PEM layout
key wrapping
legacy decrypt
```

Such a change requires a written migration design, new and old fixture
coverage, explicit data/key migration or dual-read strategy, downstream
review, and a major-version decision. Do not hide it in a package refresh or
change it merely because a replacement crypto library exposes a different API.
