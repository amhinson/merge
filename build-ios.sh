#!/bin/bash
# Build iOS and upload to TestFlight.
# Usage:
#   ./build-ios.sh dev      # Development build (dev Supabase)
#   ./build-ios.sh prod     # Release build (prod Supabase)
#   ./build-ios.sh all      # Both dev and prod (sequentially)
#
# Build number auto-increments from .build-number file.
# Override with: BUILD_NUMBER=42 ./build-ios.sh prod
#
# Prerequisites:
#   - Unity installed (uses command-line build)
#   - Xcode installed
#   - Valid Apple Developer account configured in Xcode

set -e

PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity"
BUILD_NUM_FILE="$PROJECT_PATH/.build-number"

# ===== Build number =====

get_next_build_number() {
    if [ -n "$BUILD_NUMBER" ]; then
        echo "$BUILD_NUMBER"
        return
    fi
    if [ -f "$BUILD_NUM_FILE" ]; then
        echo $(( $(cat "$BUILD_NUM_FILE") + 1 ))
    else
        echo 1
    fi
}

save_build_number() {
    echo "$1" > "$BUILD_NUM_FILE"
}

# ===== Build one environment =====

build_env() {
    local env="$1"
    local build_num="$2"

    echo "========================================"
    echo "  iOS $env — build #$build_num"
    echo "========================================"

    local BUILD_DIR="$PROJECT_PATH/Build/iOS"
    local XCODE_PROJECT="$BUILD_DIR/Unity-iPhone.xcodeproj"
    local ARCHIVE_PATH="$PROJECT_PATH/Build/Murge-${env}.xcarchive"
    local IPA_DIR="$PROJECT_PATH/Build/IPA-${env}"

    # Clean
    rm -rf "$BUILD_DIR"
    mkdir -p "$BUILD_DIR"

    # Unity flags
    local BUILD_OPTIONS="-buildTarget iOS"
    if [ "$env" = "dev" ]; then
        BUILD_OPTIONS="$BUILD_OPTIONS -development"
    fi

    # Unity build
    echo ""
    echo "Building Xcode project with Unity..."
    "$UNITY_PATH" \
        -batchmode \
        -nographics \
        -quit \
        -projectPath "$PROJECT_PATH" \
        $BUILD_OPTIONS \
        -executeMethod BuildScript.BuildiOS \
        -buildNumber "$build_num" \
        -logFile "$PROJECT_PATH/Build/unity-build-${env}.log" \
        || { echo "Unity build failed ($env). Check Build/unity-build-${env}.log"; exit 1; }

    echo "Xcode project generated"

    # Archive
    echo ""
    echo "Archiving with Xcode..."
    xcodebuild \
        -project "$XCODE_PROJECT" \
        -scheme "Unity-iPhone" \
        -configuration Release \
        -archivePath "$ARCHIVE_PATH" \
        -allowProvisioningUpdates \
        archive \
        || { echo "Xcode archive failed ($env)"; exit 1; }

    echo "Archive created"

    # Export + upload
    echo ""
    echo "Exporting IPA and uploading to App Store Connect..."
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
        || { echo "IPA export failed ($env)"; exit 1; }

    echo ""
    echo "  $env build #$build_num uploaded to TestFlight"
    echo "========================================"
    echo ""
}

# ===== Main =====

ENV="${1:-dev}"

if [ ! -f "$UNITY_PATH" ]; then
    echo "Unity not found at $UNITY_PATH"
    echo "Update UNITY_PATH in this script to match your Unity installation."
    exit 1
fi

BUILD_NUM=$(get_next_build_number)
START_TIME=$(date +%s)

if [ "$ENV" = "all" ]; then
    build_env "dev" "$BUILD_NUM"
    build_env "prod" "$BUILD_NUM"
else
    build_env "$ENV" "$BUILD_NUM"
fi

save_build_number "$BUILD_NUM"

END_TIME=$(date +%s)
ELAPSED=$(( END_TIME - START_TIME ))
echo "Done in $(( ELAPSED / 60 ))m $(( ELAPSED % 60 ))s — build #$BUILD_NUM"
