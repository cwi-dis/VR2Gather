#!/bin/bash
#
# Build a player for this Unity project with the Unity CLI (`unity build`) and zip it.
#
# Usage: scripts/build.sh [mac|windows|linux]     (default: the current platform)
#
# This script is identical in every VR2Gather project: the project is the directory above this
# script, and the app name is the productName from ProjectSettings.
# The Unity version comes from ProjectVersion.txt; the Unity CLI finds the Editor.
# The project must not be open in a Unity Editor while building.
# Output: Builds/<platform>/ and Builds/<productName>-<platform>.zip, build log in Builds/.
#
set -e
set -x

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
APP_NAME="$(sed -n 's/^  productName: *//p' "$PROJECT_DIR/ProjectSettings/ProjectSettings.asset" | tr -d '\r')"
if [ -z "$APP_NAME" ]; then
	echo "Cannot find productName in $PROJECT_DIR/ProjectSettings/ProjectSettings.asset" >&2
	exit 1
fi

platform="$1"
if [ -z "$platform" ]; then
	case "$(uname -s)" in
	Darwin) platform=mac ;;
	MINGW*|MSYS*|CYGWIN*) platform=windows ;;
	Linux) platform=linux ;;
	*) echo "Unknown system $(uname -s), specify mac, windows or linux" >&2; exit 2 ;;
	esac
fi

case "$platform" in
mac) target=StandaloneOSX; player="$APP_NAME.app" ;;
windows) target=StandaloneWindows64; player="$APP_NAME.exe" ;;
linux) target=StandaloneLinux64; player="$APP_NAME" ;;
*) echo "Usage: $0 [mac|windows|linux]" >&2; exit 2 ;;
esac

BUILD_DIR="$PROJECT_DIR/Builds/$platform"
ZIP_PATH="$PROJECT_DIR/Builds/$APP_NAME-$platform.zip"
LOG_PATH="$PROJECT_DIR/Builds/buildlog-$platform.txt"

rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"
echo "Building $target player for $PROJECT_DIR into $BUILD_DIR/$player"
unity build "$PROJECT_DIR" --target "$target" -o "$BUILD_DIR/$player" -l "$LOG_PATH" --no-tail --non-interactive

# Unity puts debug-symbol folders next to the player that must not be shipped
echo "Zipping to $ZIP_PATH"
rm -f "$ZIP_PATH"
if [ "$platform" = windows ] && command -v powershell.exe >/dev/null; then
	# Git Bash on Windows usually has no zip
	powershell.exe -NoProfile -Command "Get-ChildItem '$(cygpath -w "$BUILD_DIR")' | Where-Object { \$_.Name -notlike '*DoNotShip*' -and \$_.Name -notlike '*DontShip*' } | Compress-Archive -DestinationPath '$(cygpath -w "$ZIP_PATH")'"
else
	# -y keeps symlinks, needed for macOS .app bundles
	(cd "$BUILD_DIR" && zip -qry "$ZIP_PATH" . -x '*DoNotShip*' '*DontShip*')
fi
echo "Done: $ZIP_PATH"
