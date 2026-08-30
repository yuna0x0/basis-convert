# Signing UPM packages, and whether it applies here

Read 2026-08-30 from OpenUPM's guide and Unity's own manual. Sources:
`openupm.com/docs/signing-upm-packages.html`, `docs.unity3d.com/Manual/upm-cli-install.html`,
`docs.unity3d.com/Manual/upm-cli-pack.html`.

## What it is

Unity 6.3 added signature verification for tarball packages: the Package Manager checks whether a
`.tgz` carries a signature and marks it when it does not. Unity holds the certificate and the
private key. A package author authenticates with a Unity service account and Unity signs on their
behalf, so nobody has to run their own signing infrastructure.

OpenUPM does not sign anything. It publishes a tarball the author signed, taken from a GitHub
release, when the package is tracked with `trackingMode: githubRelease`.

## Whether it matters for us

Basis targets Unity 6000.5, which is past the version that checks, so a consumer installing an
unsigned tarball sees it flagged. It applies to the OpenUPM channel only: a VPM zip is not a UPM
tarball, and a git URL install is not one either. So signing improves one of our four install
routes and leaves the rest unchanged.

Signing says who published a tarball, not that its contents are safe, and the tarball can differ
from the repository it was built from.

## What it needs

- A **public** GitHub repository, which OpenUPM requires anyway.
- A Unity organization, and its organization ID from the Unity Cloud Dashboard.
- A service account in that organization with the **Package Manager Package Signer** role, and a
  key: an id and a secret, the secret shown once.
- Three secrets in the repository: `UPM_SERVICE_ACCOUNT_KEY_ID`,
  `UPM_SERVICE_ACCOUNT_KEY_SECRET`, `UPM_ORG_ID`.

## The commands

```sh
curl -fsSL https://cdn.packages.unity.com/upm-cli/install.sh | bash
upm --version
upm pack Packages/com.yuna0x0.basis.convert --organization-id "$UPM_ORG_ID" --destination dist
```

The credentials are read from the environment; `--organization-id` is a flag. A signed tarball
contains `package/package.json` and `package/.attestation.p7m`, which is worth asserting in CI
rather than trusting.

The release asset has to be attached to the GitHub release itself. Workflow artifacts expire, and
OpenUPM reads releases.

## State here

`.github/workflows/release.yml` carries the steps, guarded on `UPM_ORG_ID` being set, so releases
work unchanged until the secrets exist. What remains is outside this repository: the Unity
organization and service account, making the repository public, and the OpenUPM package entry
with `trackingMode: githubRelease`.
