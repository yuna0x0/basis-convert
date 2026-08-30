# Basis Convert

Bring an avatar you already own into [Basis](https://basisvr.org/), with its physics, constraints
and menus intact.

Import the avatar into a Basis project, pick it in the scene, and convert:

- **VRChat PhysBones and legacy Dynamic Bone** become Basis jiggle physics
- **VRChat constraints** become their Basis equivalents
- **The avatar descriptor** becomes a `BasisAvatar` component
- **Menu toggles** become HVR Vixxy controls

Clothing and accessories convert with the avatar they are worn on.

Nothing is written until you press Convert, one undo reverts the lot, and anything that cannot
come across cleanly is listed rather than dropped quietly.

Documentation: **https://yuna0x0.github.io/basis-convert/**

## Installing

The package needs a Basis project.

Add the VPM listing `https://vpm.yuna0x0.com/index.json` in
[ALCOM](https://vrc-get.anatawa12.com/en/alcom/) or the VPM CLI, then add **Basis Convert** to
your project.

With [OpenUPM](https://openupm.com/):

```sh
openupm add com.yuna0x0.basis.convert
```

Or by git URL, in Unity's Package Manager under `Add package from git URL`:

```
https://github.com/yuna0x0/basis-convert.git?path=/Packages/com.yuna0x0.basis.convert
```

Every release also has a `.unitypackage` attached. The
[documentation](https://yuna0x0.github.io/basis-convert/docs/installation) covers all four in
full.

## Contributing

Issues and pull requests are welcome. [CONTRIBUTING.md](CONTRIBUTING.md) covers the development
setup, and [`agent/`](agent/README.md) holds the design notes and past decisions.

## License

MIT, see [LICENSE](LICENSE).

Basis, BasisVR and Basis Framework are trademarks of the Basis Project. This is an independent
tool, not affiliated with or endorsed by them.
