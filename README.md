<img src="assets/logo/rcinellevel-128.png" alt="RL" width="96" align="right">

# RcinelLevel

A Valheim mod for levelling ground the way you plan it: a ground probe that frames an area and finds
the height where digging equals filling, a level hoe that fills up to it with straight walls (for
stone), a depth lock that keeps the pickaxe inside the frame, and moats sized for the game's
drawbridges.

**Players: get it on Thunderstore** (`CodeIsNotEvil-RcinelLevel`), which also has the controls and
everything about multiplayer: [`package/README.md`](package/README.md) is that page.

## Building

Needs the .NET SDK and Valheim installed, because the plugin compiles against the game's assemblies,
which never go into this repository.

```sh
cp Environment.props.example Environment.props   # the game's path, optionally a LaunchHeim test instance
dotnet build RcinelLevel.slnx                     # builds, and copies into the test instance if set
scripts/package.py                                # dist/CodeIsNotEvil-RcinelLevel-<version>.zip
```

The models and item icons are made by `models/build.py` in Blender ([`models/README.md`](models/README.md)),
the logo by `scripts/text_logo.py` ([`assets/README.md`](assets/README.md)).

## License

MIT, see [`LICENSE`](LICENSE). The logo uses Kode Mono (SIL Open Font License, `assets/fonts/OFL.txt`).
Nothing of Valheim's is in the repository or the package: the game's models, textures and code are
used at run time from the player's own install.
