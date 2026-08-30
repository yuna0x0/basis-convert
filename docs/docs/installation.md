---
sidebar_position: 2
---

# Installing

Watari installs into a Basis project as a Unity package. It builds against the Basis SDK,
Jiggle Physics and HVR Basis Comms, all of which ship with the Basis framework, so a Basis
project already has everything it needs.

Four ways to install it, in the order most people will want them.

## ALCOM

The package is published to a VPM listing. [ALCOM](https://vrc-get.anatawa12.com/en/alcom/) is
the client to use for it: an open-source VPM client, the graphical front end to vrc-get, and it
works with any Unity project.

1. Add the repository `https://vpm.yuna0x0.com/index.json` under Packages, using
   **Add Repository**.
2. Open your Basis project under Manage Project.
3. Add **Watari** and apply.

Updates appear in the same place, as a version to move to.

From a terminal, [vrc-get](https://github.com/vrc-get/vrc-get) does the same job. It is the
open-source client ALCOM is built on, so the two share their repository list:

```sh
vrc-get repo add https://vpm.yuna0x0.com/index.json
vrc-get install com.yuna0x0.basis.convert
```

VRChat's own [VPM CLI](https://vcc.docs.vrchat.com/vpm/cli/) understands the same listing:

```sh
vpm add repo https://vpm.yuna0x0.com/index.json
vpm add package com.yuna0x0.basis.convert
```

{/*
  IMAGE PLACEHOLDER: ALCOM with the listing added and Watari ready to install.
  Save as docs/static/img/install-alcom.png, then replace this comment with:
  ![Installing through ALCOM](/img/install-alcom.png)
*/}

## OpenUPM

For projects managed with plain Unity Package Manager rather than a VPM client.

With the [OpenUPM CLI](https://openupm.com/docs/getting-started-cli.html):

```sh
openupm add com.yuna0x0.basis.convert
```

Or add the scoped registry by hand, in **Edit > Project Settings > Package Manager**:

- Name: `OpenUPM`
- URL: `https://package.openupm.com`
- Scope: `com.yuna0x0`

Then add `com.yuna0x0.basis.convert` in **Window > Package Manager > + > Install package by
name**.

## Git URL

No extra tooling, and useful for trying an unreleased version.

**Window > Package Manager > + > Add package from git URL**:

```
https://github.com/yuna0x0/watari-basis.git?path=/Packages/com.yuna0x0.basis.convert
```

Add `#v0.1.0` to the end to pin a version. Packages added this way are updated by removing and
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
