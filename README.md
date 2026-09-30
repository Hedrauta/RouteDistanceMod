# Route Distance HUD

A small [BepInEx](https://github.com/BepInEx/BepInEx) plugin for **Vanguard Galaxy** that extends the travel HUD with information about your **entire remaining route**, not just the current warp segment.

By default the game only shows the distance and ETA to the next waypoint. When you travel across several systems via jumpgates, this plugin adds two extra lines:

|    Line   |  Meaning  |
|-----------|---------|
| `To Target:   XXXX Ls` | Total remaining distance to your final destination, summed over all segments |
| `Route ETA: M:SS` | Estimated total travel time, including gate transitions and fast lane travel |

The extra lines disappear automatically on the last segment of the trip, where they would only repeat the normal HUD values.

## Features

- Total remaining distance across all jumpgates and waypoints
- Total ETA that accounts for:
  - normal warp segments (acceleration, cruise, braking)
  - fast lane speed on gate-to-gate segments
  - a fixed transition time per gate jump (6 s)
- Extra HUD lines are placed below the existing distance and ETA lines
- Lines hide themselves on the final segment
- No configuration needed

## Requirements

- Vanguard Galaxy (v0.8.2.4+)
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases)

## Installation

1. Install BepInEx 5 into your Vanguard Galaxy folder and start the game once, so that the `BepInEx` folder structure gets created.
2. Download `RouteDistanceMod.dll` from the [Releases](../../releases) page.
3. Copy it to:
   ```
   <Vanguard Galaxy folder>/BepInEx/plugins/
   ```
4. Start the game. Set a route across multiple systems and the extra HUD lines will show up.

To uninstall, delete the DLL from `BepInEx/plugins/`.

## Known limitations

- The ETA is an **estimate**. It is based on the game's own movement rules, but a few things are not modeled:
  - the charge time of fast lane travel at each gate
  - the speed penalty when your hull is nearly destroyed
  - dynamic events that appear during travel
- The 6 second gate transition time is a fixed assumption. Real values may differ slightly.
- Wormhole segments are counted as zero distance plus the same transition time as a gate.

## Compatibility

Tested on the game version 0.8.2.4 at the time of this release. Since the plugin patches the game's HUD classes with [Harmony](https://github.com/pardeike/Harmony), a major game update may break it. If that happens, please open an issue.

## Building from source

See [BUILDING.md](BUILDING.md).

## Contributing

Bug reports and pull requests are welcome. When reporting a problem, please include your game version and the `BepInEx/LogOutput.log`.

## Legal

This is an unofficial fan-made mod and is not affiliated with or endorsed by the developers of Vanguard Galaxy. This repository does not contain any game files or game code. To build the plugin you need to copy the required DLLs from your own game installation (see [BUILDING.md](BUILDING.md)).

## License

Released under the [MIT License](LICENSE).

## Credits

- Developed with AI assistance (Claude by Anthropic).
- Built with [BepInEx](https://github.com/BepInEx/BepInEx) and [Harmony](https://github.com/pardeike/Harmony).
