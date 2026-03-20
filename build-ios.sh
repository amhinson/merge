#!/bin/bash
# Build iOS and upload to TestFlight.
# Usage:
#   ./build-ios.sh dev     # Development build (dev Supabase)
#   ./build-ios.sh prod    # Release build (prod Supabase, ready for TestFlight)
#
# Prerequisites:
#   - Unity installed (uses command-line build)
#   - Xcode installed
#   - Valid Apple Developer account configured in Xcode
#   - fastlane installed: brew install fastlane
#   - App Store Connect API key set up for fastlane (see below)

set -e

ENV="${1:-dev}"
PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity"
BUILD_DIR="$PROJECT_PATH/Build/iOS"
XCODE_PROJECT="$BUILD_DIR/Unity-iPhone.xcodeproj"

# Check Unity exists
if [ ! -f "$UNITY_PATH" ]; then
    echo "❌ Unity not found at $UNITY_PATH"
    echo "   Update UNITY_PATH in this script to match your Unity installation."
    exit 1
fi

echo "========================================"
echo "  Building iOS ($ENV)"
echo "========================================"

# Clean previous build
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

# Set build method based on environment
if [ "$ENV" = "prod" ]; then
    BUILD_OPTIONS="-buildTarget iOS"
    echo "📦 PRODUCTION build (release, prod Supabase)"
else
    BUILD_OPTIONS="-buildTarget iOS -development"
    echo "🔧 DEVELOPMENT build (dev Supabase)"
fi

# Unity command-line build
echo ""
echo "🔨 Building Xcode project with Unity..."
"$UNITY_PATH" \
    -batchmode \
    -nographics \
    -quit \
    -projectPath "$PROJECT_PATH" \
    $BUILD_OPTIONS \
    -executeMethod BuildScript.BuildiOS \
    -logFile "$PROJECT_PATH/Build/unity-build.log" \
    || { echo "❌ Unity build failed. Check Build/unity-build.log"; exit 1; }

echo "✅ Xcode project generated at $BUILD_DIR"

# Build and archive with xcodebuild
echo ""
echo "📱 Building and archiving with Xcode..."
ARCHIVE_PATH="$PROJECT_PATH/Build/Overtone.xcarchive"

xcodebuild \
    -project "$XCODE_PROJECT" \
    -scheme "Unity-iPhone" \
    -configuration Release \
    -archivePath "$ARCHIVE_PATH" \
    -allowProvisioningUpdates \
    archive \
    || { echo "❌ Xcode archive failed"; exit 1; }

echo "✅ Archive created"

# Export IPA
echo ""
echo "📤 Exporting IPA..."
IPA_DIR="$PROJECT_PATH/Build/IPA"
mkdir -p "$IPA_DIR"

cat > "$PROJECT_PATH/Build/ExportOptions.plist" << 'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>method</key>
    <string>app-store-connect</string>
    <key>destination</key>
    <string>upload</string>
</dict>
</plist>
PLIST

xcodebuild \
    -exportArchive \
    -archivePath "$ARCHIVE_PATH" \
    -exportPath "$IPA_DIR" \
    -exportOptionsPlist "$PROJECT_PATH/Build/ExportOptions.plist" \
    -allowProvisioningUpdates \
    || { echo "❌ IPA export failed"; exit 1; }

echo ""
echo "========================================"
echo "  ✅ Build complete!"
echo "  IPA: $IPA_DIR"
echo "========================================"

if [ "$ENV" = "prod" ]; then
    echo ""
    echo "The IPA was exported with 'app-store-connect' method."
    echo "It should be automatically uploaded to App Store Connect."
    echo "Check TestFlight in App Store Connect for the new build."
else
    echo ""
    echo "Dev build exported. To install on device, use Xcode or:"
    echo "  xcrun devicectl install app --device <UDID> $IPA_DIR/*.ipa"
fi
