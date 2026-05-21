# Installer

Produces a single-file Windows installer (`FlankSetup-<version>.exe`) using [Inno Setup 6](https://jrsoftware.org/isinfo.php).

## Prerequisites

- [Inno Setup 6](https://jrsoftware.org/isdl.php) installed on Windows
- A completed Unity build at `Flank/Build/` (target: **Windows x86_64**)

## Building the installer

From the repo root:

```cmd
iscc installer\flank-setup.iss
```

Output is written to `installer\Output\FlankSetup-1.0.0.exe`.

You can also open `flank-setup.iss` in the Inno Setup IDE and press **Ctrl+F9**.

## What the installer does

- Installs the game to `%ProgramFiles%\Flank` (user can change the path)
- Creates a Start Menu shortcut
- Offers an optional desktop shortcut
- Registers a standard Windows uninstaller

## Changing the version

Edit the `#define AppVersion` line near the top of `flank-setup.iss`:

```iss
#define AppVersion "1.1.0"
```

## What is excluded

`Flank_BurstDebugInformation_DoNotShip` — Unity's Burst debug symbols folder. It is not needed by players and is intentionally omitted.
