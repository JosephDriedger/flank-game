# Installer

Packages the game for distribution on Windows and macOS.

| Platform | Script | Output |
|----------|--------|--------|
| Windows | `flank-setup.iss` | `Output\FlankSetup-<version>.exe` |
| macOS | `build-dmg.sh` | `Output/FlankSetup-<version>.dmg` |

---

## Windows — Inno Setup

### Prerequisites

- [Inno Setup 6](https://jrsoftware.org/isdl.php) installed on Windows
- A completed Unity build at `Flank/Build/` (target: **Windows x86_64**)

### Build

From the repo root:

```cmd
iscc installer\flank-setup.iss
```

Output: `installer\Output\FlankSetup-1.0.0.exe`.  
You can also open `flank-setup.iss` in the Inno Setup IDE and press **Ctrl+F9**.

### What it does

- Installs to `%ProgramFiles%\Flank` (user can change the path)
- Creates a Start Menu shortcut
- Offers an optional desktop shortcut
- Registers a standard Windows uninstaller

### What is excluded

`Flank_BurstDebugInformation_DoNotShip` — Unity's Burst debug symbols. Not needed by players, intentionally omitted.

---

## macOS — DMG

### Prerequisites

- macOS (the script guards against running on other platforms)
- A completed Unity build at `Flank/BuildMac/` (target: **macOS**)
- **Recommended:** `create-dmg` for a polished drag-to-Applications layout:
  ```bash
  brew install create-dmg
  ```
  Without it, the script falls back to the built-in `hdiutil` (plain compressed DMG, no background art).

### Build

```bash
cd installer
chmod +x build-dmg.sh
./build-dmg.sh
```

Output: `installer/Output/FlankSetup-1.0.0.dmg`.

### What it produces

A DMG disk image containing `Flank.app` with a symlink to `/Applications` so players can drag-install in one step.

### Notarization (required for public distribution)

Without notarization, Gatekeeper will block the app on other people's Macs. After building the DMG:

```bash
xcrun notarytool submit "Output/FlankSetup-1.0.0.dmg" \
  --apple-id you@example.com \
  --team-id YOURTEAMID \
  --password <app-specific-password> \
  --wait

xcrun stapler staple "Output/FlankSetup-1.0.0.dmg"
```

You need an Apple Developer account to notarize.

---

## Changing the version

**Windows** — edit `#define AppVersion` in `flank-setup.iss`:
```iss
#define AppVersion "1.1.0"
```

**macOS** — edit `APP_VERSION` in `build-dmg.sh`:
```bash
APP_VERSION="1.1.0"
```
