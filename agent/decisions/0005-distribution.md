# 0005: Distribution via a VPM listing and OpenUPM

**Status:** accepted, 2026-08-30

## Decision

Ship through two channels:

1. **VPM listing at `https://vpm.yuna0x0.com`**, source repo
   [`yuna0x0/vpm-listing`](https://github.com/yuna0x0/vpm-listing). This is the primary channel,
   because it is how this ecosystem installs things: Haï publishes his Basis packages through
   his own listing, and Basis users already have ALCOM or VCC pointed at listings.
2. **OpenUPM**, for plain UPM consumers who are not using a VPM client.

Git URL installation keeps working regardless, and is the lowest friction path while the
package is pre-release. It is how the Basis project itself pulls in Chillaxins.

## What the listing needs

`yuna0x0/vpm-listing` already exists and is wired up: `source.json` declares the repo
(`com.yuna0x0.vpm`, `https://vpm.yuna0x0.com/index.json`), and `.github/workflows/build-listing.yml`
runs `vrchat-community/package-list-action` on push to `source.json`, publishing to GitHub Pages.

Both `githubRepos` and `packages` in `source.json` are currently empty. To publish:

1. Add this package's repo to `githubRepos` in `source.json`.
2. This repo needs a release workflow that, per VPM convention, attaches to each GitHub release
   both the bare `package.json` and a zip of `Packages/com.yuna0x0.basis.convert`, named
   `com.yuna0x0.basis.convert-<version>.zip`. The listing action reads releases to build the
   index.
3. Tag versions to match `package.json`'s `version`. The listing action trusts the tag.

Our repo layout already matches what the VRChat package template expects, with the package
under `Packages/<package-id>/`, so the standard release workflow applies with only the package
id changed.

## Consequences

- `package.json` must keep `vpmDependencies` accurate. VPM clients resolve from it, and it is
  the only place a Basis dependency can be expressed, since Basis packages are not on any
  registry.
- Do not publish until the naming question in [0002](0002-menu-placement-and-naming.md) is
  settled. Renaming a package after it is in someone's listing is worse than renaming it now.
