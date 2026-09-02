---
sidebar_position: 2
---

# Installing

Watari installs into a Basis project as a Unity package. It builds against the Basis SDK,
Jiggle Physics and HVR Basis Comms, all of which ship with the Basis framework, so a Basis
project already has everything it needs.

:::danger Do not install the VRChat SDK into a Basis project

It is not needed. VRChat components arrive as missing scripts, which is what Watari reads.

It changes layers and the physics collision matrix, which are project-wide. Basis uses layer 3
and layers 6 to 11 for its player, avatar and interaction handling, so this breaks Basis itself.

:::

Four ways to install it.

## VPM

The package is published to a VPM listing at `https://vpm.yuna0x0.com/index.json`. Add that
listing to your VPM client once, and the package and its updates appear alongside everything else
you install that way.

### ALCOM

[ALCOM](https://vrc-get.anatawa12.com/en/alcom/) is an open-source VPM client with a graphical
interface, and works with any Unity project.

1. Open **Resources** in the sidebar. It opens on the **Repositories** tab.
2. Press **Add Repository**, enter `https://vpm.yuna0x0.com/index.json`, and confirm the dialog
   that lists what the repository holds. It is saved to ALCOM itself, not to a project, so this
   is done once.
3. Open **Projects** and select your Basis project.
4. Find **Watari (Converter for Basis)** in its package list and press the **+** on that row.
   The version column takes a specific version instead.

Updates appear in the same list, as a newer version in that column.

{/*
  IMAGE PLACEHOLDER: ALCOM with the listing added and Watari ready to install.
  Save as docs/static/img/install-alcom.png, then replace this comment with:
  ![Installing through ALCOM](/img/install-alcom.png)
*/}

### vrc-get

[vrc-get](https://github.com/vrc-get/vrc-get) is the command line client ALCOM is built on, so
the two share their repository list:

```sh
vrc-get repo add https://vpm.yuna0x0.com/index.json
vrc-get install com.yuna0x0.basis.convert
```

### VPM CLI

VRChat's own [VPM CLI](https://vcc.docs.vrchat.com/vpm/cli/) reads the same listing:

```sh
vpm add repo https://vpm.yuna0x0.com/index.json
vpm add package com.yuna0x0.basis.convert
```

## OpenUPM

For projects managed with plain Unity Package Manager rather than a VPM client.

With the [OpenUPM CLI](https://openupm.com/docs/getting-started-cli.html):

```sh
openupm add com.yuna0x0.basis.convert
```

Or add the scoped registry by hand, in **Edit > Project Settings > Package Manager**:

- Name: `OpenUPM`
- URL: `https://package.openupm.com`
- Scope: `com.yuna0x0.basis.convert`

Scoping to the package rather than to `com.yuna0x0` keeps Unity resolving only this package from
OpenUPM.

Then add `com.yuna0x0.basis.convert` in **Window > Package Manager > + > Install package by
name**.

## Git URL

No extra tooling, and useful for trying an unreleased version.

**Window > Package Manager > + > Install package from git URL**:

```
https://github.com/yuna0x0/watari-basis.git?path=/Packages/com.yuna0x0.basis.convert
```

Add `#v{{VERSION}}` to the end to pin a version. Packages added this way are updated by removing and
re-adding them, or by changing the version at the end of the URL.

{/*
  IMAGE PLACEHOLDER: Unity's Package Manager with the git URL field filled in.
  Save as docs/static/img/install-package-manager.png, then replace this comment with:
  ![Adding the package by git URL](/img/install-package-manager.png)
*/}

## Manual

Every release also has a `.unitypackage` attached, for projects that do not use a package manager
at all.

Download it from the
[releases page](https://github.com/yuna0x0/watari-basis/releases) and drag it into your project.
It restores to `Packages/com.yuna0x0.basis.convert`, so Unity treats it as an embedded package.
Updating means deleting that folder first, then importing the new one.
