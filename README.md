# Vehicle Framework

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-squared&label=License" /></a>
  <a href="https://github.com/SmashPhil/Vehicle-Framework/releases/latest"><img src="https://img.shields.io/github/release/SmashPhil/Vehicle-Framework.svg?style=flat-squared&label=Release" /></a>
	<a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3014915404"><img src="https://img.shields.io/steam/downloads/3014915404?style=flat&logo=steam&label=Downloads" /></a>
</p>
<p align="center">
  <a href="https://github.com/SmashPhil/Vehicle-Framework/wiki"><img src="https://img.shields.io/badge/Documentation-gray?style=flat&logo=github&color=gray" /></a>
  <a href="https://discord.gg/zXDyfWQ"><img src="https://img.shields.io/discord/588278492655910925?style=flat&logo=Discord&label=Discord" /></a>
</p>

Vehicle Framework is a RimWorld mod framework for creating vehicles.

It provides the systems that vehicle content mods build upon: vehicle pawns, custom pathing, component-based vehicle health, turrets, aerial launch and landing behavior, paint and skin support, graphic overlays, mod settings, and compatibility hooks.

Vehicle Framework does **not** add playable vehicles by itself. Install vehicle content mods alongside it.

If you are a modder that wishes to use the framework, joining the Discord server is highly recommended.

## Links
- [Community Recommended Vehicle Mods](https://discord.gg/CtuQKJfJZs)
- [Mod Compatibility Spreadsheet](https://docs.google.com/spreadsheets/d/1JRFSMzkXdFmg7wIJeR3W34KQZkRNBS_zQw4wdPoRPnc)

## Requirements

- RimWorld `1.4`, `1.5`, or `1.6`
- [Harmony](https://steamcommunity.com/workshop/filedetails/?id=2009463077)

Load order:

1. Harmony
2. RimWorld core and DLCs
3. Vehicle Framework
4. Vehicle content mods

## Compatibility

Known mod incompatibilities are tracked [here](https://docs.google.com/spreadsheets/d/1JRFSMzkXdFmg7wIJeR3W34KQZkRNBS_zQw4wdPoRPnc).

Vehicle Framework is a large mod, so incompatibilities and strange interactions with other mods can happen. If a mod is not listed in the spreadsheet, then no bugs have been reported for it.

## For players

### Steam install

Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3014915404), enable Harmony, then enable Vehicle Framework before any vehicle content mods.

### Manual install

Download the latest release or the latest deployment from `develop`.

**Important:** If you download or clone the repository directly, you will have to build the project with `dotnet build` or rebuild the solution. Binaries are not included outside of deployments or releases.

Then drag the mod into your RimWorld `Mods` folder.

## For modders

Vehicle Framework is intended as a base dependency for vehicle mods.

Use the package ID below when declaring a dependency:

```xml
<packageId>SmashPhil.VehicleFramework</packageId>
```

### Cloning Repository

```bash
git clone --recursive https://github.com/SmashPhil/Vehicle-Framework.git
cd Vehicle-Framework
git submodule update --init --recursive
```

## Bug reports and feedback

Bug reports and feedback are only accepted on Discord and GitHub.

- [Discord](https://discord.gg/zXDyfWQ)
- [GitHub Issues](https://github.com/SmashPhil/Vehicle-Framework/issues)
