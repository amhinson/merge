#!/bin/bash
# Install the dev APK to a connected Android device via ADB.
# Usage:
#   ./install-android.sh          # Install dev APK
#   ./install-android.sh prod     # Install prod APK (if built as APK)
#
# Prerequisites:
#   - Android device connected via USB with USB Debugging enabled
#   - ADB available (comes with Android SDK via Unity)

set -e

ENV="${1:-dev}"
PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
APK_PATH="$PROJECT_PATH/Build/Android/murge-${ENV}.apk"

# Find ADB
ADB="adb"
if ! command -v adb &> /dev/null; then
    ADB="$HOME/Library/Android/sdk/platform-tools/adb"
    if [ ! -f "$ADB" ]; then
        echo "ADB not found. Install Android SDK or add platform-tools to PATH:"
        echo "  export PATH=\"\$PATH:\$HOME/Library/Android/sdk/platform-tools\""
        exit 1
    fi
fi

if [ ! -f "$APK_PATH" ]; then
    echo "APK not found at $APK_PATH"
    echo "Run ./build-android.sh $ENV first."
    exit 1
fi

# Check for connected device
DEVICES=$("$ADB" devices | grep -v "List" | grep "device$" | wc -l | tr -d ' ')
if [ "$DEVICES" = "0" ]; then
    echo "No Android device found. Check:"
    echo "  1. Phone is connected via USB"
    echo "  2. USB Debugging is enabled (Settings > Developer Options)"
    echo "  3. You've approved the connection prompt on the phone"
    exit 1
fi

echo "Installing murge-${ENV}.apk..."
"$ADB" install -r "$APK_PATH"
echo "Done. Launch Murge on your device."
