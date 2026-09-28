# Building Route Distance HUD

This guide explains how to compile the plugin yourself on **Windows, Linux or macOS**.

The repository does **not** contain any game files. You have to copy a few DLLs from your own Vanguard Galaxy installation into a local `libs/` folder. That folder is listed in `.gitignore` and must never be committed.

## 1. Prerequisites

- The [.NET SDK](https://dotnet.microsoft.com/download) (version 6 or newer works fine; the project targets .NET Framework 4.7.2, which BepInEx 5 uses)
- A Vanguard Galaxy installation with BepInEx 5 installed (to copy the DLLs from)
- `git` (optional, you can also download the repository as a ZIP)

Check that the SDK is installed:

```bash
dotnet --version
```

On Linux and macOS the project pulls in the NuGet package `Microsoft.NETFramework.ReferenceAssemblies` automatically during restore. It provides the .NET Framework 4.7.2 reference files that are otherwise only available on Windows. You need an internet connection for the first build.

## 2. Get the source

```bash
git clone https://github.com/YOUR_NAME/RouteDistanceMod.git
cd RouteDistanceMod
```

## 3. Project layout

After preparing everything, the folder should look like this:

```
RouteDistanceMod/
├── RouteDistanceMod.csproj
├── Plugin.cs
├── RouteDistanceCalculator.cs
├── BUILDING.md
├── README.md
├── LICENSE
├── .gitignore
└── libs/                     <- you create this, it is NOT in the repository
    ├── Assembly-CSharp.dll
    ├── BepInEx.dll
    ├── 0Harmony.dll
    ├── UnityEngine.dll
    ├── UnityEngine.CoreModule.dll
    ├── UnityEngine.UI.dll
    └── Unity.TextMeshPro.dll
```

## 4. Copy the required DLLs

Create the `libs` folder next to the `.csproj` file and copy the following files into it.

**From the game's managed folder** (`<Vanguard Galaxy folder>/VanguardGalaxy_Data/Managed/`):

| File | Needed for |
|------|-----------|
| `Assembly-CSharp.dll` | the game's own classes (travel, map, player) |
| `UnityEngine.dll` | Unity base and type forwarding |
| `UnityEngine.CoreModule.dll` | `Vector2`, `MonoBehaviour`, `GameObject` |
| `UnityEngine.UI.dll` | UI types |
| `Unity.TextMeshPro.dll` | `TextMeshProUGUI` |

**From BepInEx** (`<Vanguard Galaxy folder>/BepInEx/core/`):

| File | Needed for |
|------|-----------|
| `BepInEx.dll` | plugin base class and attributes |
| `0Harmony.dll` | patching the game's methods |

Copying the whole contents of the `Managed` folder also works, but only the files above are actually referenced.

## 5. Build

From the project folder:

```bash
dotnet build -c Release
```

The compiled plugin is written to:

```
bin/Release/net472/RouteDistanceMod.dll
```

The game and BepInEx DLLs are referenced but not copied to the output, so that folder contains only your plugin.

## 6. Install

Copy `RouteDistanceMod.dll` to:

```
<Vanguard Galaxy folder>/BepInEx/plugins/
```

Start the game. On startup, BepInEx prints a line for the plugin in its console and in `BepInEx/LogOutput.log`.

## Troubleshooting

**`error CS1069` / `error CS0012` mentioning `UnityEngine.CoreModule`**
The file `UnityEngine.CoreModule.dll` is missing in `libs/` or is not found under exactly that name. On Linux and macOS, file names are case sensitive. Check with:
```bash
ls libs | grep -i coremodule
```

**`The type or namespace name 'TMPro' could not be found`**
`Unity.TextMeshPro.dll` is missing in `libs/`. Depending on the Unity version, the file may have a slightly different name. Search the `Managed` folder for `*textmesh*` and adjust the reference in the `.csproj` if needed.

**`NETSDK1100` or errors about missing .NET Framework reference assemblies**
The NuGet package `Microsoft.NETFramework.ReferenceAssemblies` could not be restored. Check your internet connection and run `dotnet restore` again.

**Wrong architecture on ARM machines (e.g. Raspberry Pi)**
Install the ARM64 build of the .NET SDK. The plugin itself is architecture independent, so a DLL built on ARM runs fine on the Windows PC where the game is installed.

**Build succeeds, but nothing shows up in the game**
Check `BepInEx/LogOutput.log` for errors from `RouteDistanceMod`. The most common causes are a game update that renamed the patched classes or a BepInEx version other than 5.x (Mono).

## Notes for contributors

- Never commit anything from `libs/`, and never commit decompiled game code.
- `bin/` and `obj/` are build output and stay out of the repository.
- If you change the plugin version, update it in both `Plugin.cs` (`[BepInPlugin(...)]`) and `RouteDistanceMod.csproj` (`<Version>`), so the release tag and the DLL match.
