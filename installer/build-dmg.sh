#!/usr/bin/env bash
# build-dmg.sh — macOS DMG packager for Flank
#
# Run this script on macOS after building the game in Unity:
#   File > Build Settings > Platform: macOS > Build
#
# Usage:
#   chmod +x installer/build-dmg.sh
#   cd installer && ./build-dmg.sh
#
# Requirements (choose one):
#   • create-dmg (recommended): brew install create-dmg
#   • hdiutil (fallback): built into macOS, no install needed
#
# Output: installer/Output/FlankSetup-<version>.dmg

set -euo pipefail

# ── Configuration ────────────────────────────────────────────────────────────
APP_NAME="Flank"
APP_VERSION="1.0.0"
BUILD_DIR="${BUILD_DIR:-../Flank/BuildMac}"   # Unity macOS build output (override via env)
APP_BUNDLE="${BUILD_DIR}/${APP_NAME}.app"
OUTPUT_DIR="Output"
DMG_NAME="${APP_NAME}Setup-${APP_VERSION}"
# ─────────────────────────────────────────────────────────────────────────────

# Guard: must run on macOS
if [[ "$(uname)" != "Darwin" ]]; then
  echo "Error: this script must be run on macOS." >&2
  exit 1
fi

# Guard: Unity build must exist
if [[ ! -d "${APP_BUNDLE}" ]]; then
  echo "Error: macOS build not found at '${APP_BUNDLE}'" >&2
  echo "       Build the project in Unity: File > Build Settings > macOS > Build" >&2
  exit 1
fi

mkdir -p "${OUTPUT_DIR}"
OUTPUT_DMG="${OUTPUT_DIR}/${DMG_NAME}.dmg"

echo "Packaging ${APP_BUNDLE} → ${OUTPUT_DMG}"

# ── Prefer create-dmg for a polished, drag-to-Applications DMG ───────────────
if command -v create-dmg &>/dev/null; then
  echo "Using create-dmg …"

  # Remove any previous attempt (create-dmg refuses to overwrite)
  rm -f "${OUTPUT_DMG}"

  create-dmg \
    --volname "${APP_NAME}" \
    --window-pos 200 120 \
    --window-size 660 400 \
    --icon-size 160 \
    --icon "${APP_NAME}.app" 180 170 \
    --hide-extension "${APP_NAME}.app" \
    --app-drop-link 480 170 \
    "${OUTPUT_DMG}" \
    "${APP_BUNDLE}"

# ── Fallback: plain compressed DMG via hdiutil (no extra install needed) ──────
else
  echo "create-dmg not found — falling back to hdiutil (plain DMG)."
  echo "For a polished result: brew install create-dmg"

  STAGING="$(mktemp -d)"
  trap 'rm -rf "${STAGING}"' EXIT

  cp -R "${APP_BUNDLE}" "${STAGING}/"
  # Symlink so users can drag straight to Applications
  ln -s /Applications "${STAGING}/Applications"

  hdiutil create \
    -volname "${APP_NAME}" \
    -srcfolder "${STAGING}" \
    -ov \
    -format UDZO \
    "${OUTPUT_DMG}"
fi

echo ""
echo "Done: ${OUTPUT_DMG}"
echo ""
echo "Before distributing outside the Mac App Store, notarize with:"
echo "  xcrun notarytool submit \"${OUTPUT_DMG}\" --apple-id <you@example.com> \\"
echo "    --team-id <TEAMID> --password <app-specific-password> --wait"
echo "  xcrun stapler staple \"${OUTPUT_DMG}\""
