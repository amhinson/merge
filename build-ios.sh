#!/bin/bash
# Build iOS and optionally upload to TestFlight.
# Usage:
#   ./build-ios.sh dev           # Build + upload to TestFlight
#   ./build-ios.sh prod          # Build + upload to TestFlight
#   ./build-ios.sh all           # Both dev and prod
#   ./build-ios.sh dev --no-upload   # Build only (Xcode project + pods, no fastlane)
#   ./build-ios.sh prod --no-upload  # Build only
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
        -executeMethod MergeGame.Editor.BuildScript.BuildiOS \
        -buildNumber "$build_num" \
        -logFile "$PROJECT_PATH/Build/unity-build-${env}.log" \
        || { echo "Unity build failed ($env). Check Build/unity-build-${env}.log"; exit 1; }

    echo "Xcode project generated"

    # Install CocoaPods dependencies (Google Sign In SDK)
    if [ -f "$BUILD_DIR/Podfile" ]; then
        echo ""
        echo "Running pod install..."
        cd "$BUILD_DIR"
        pod install || { echo "pod install failed"; exit 1; }
        cd "$PROJECT_PATH"
        echo "Pods installed"
    fi

    if [ "$NO_UPLOAD" = "true" ]; then
        echo ""
        echo "  $env build #$build_num — Xcode project ready"
        echo "  Opening in Xcode..."
        echo "========================================"
        echo ""
        open "$BUILD_DIR/Unity-iPhone.xcworkspace"
    else
        # Archive, export, and upload via fastlane
        echo ""
        echo "Building and uploading via fastlane..."
        cd "$PROJECT_PATH"
        fastlane ios beta \
            || { echo "Fastlane failed ($env)"; exit 1; }

        echo ""
        echo "  $env build #$build_num — uploaded to TestFlight"
        echo "========================================"
        echo ""
    fi
}

# ===== Main =====

ENV="${1:-dev}"
NO_UPLOAD="false"
if [ "$2" = "--no-upload" ] || [ "$1" = "--no-upload" ]; then
    NO_UPLOAD="true"
    if [ "$1" = "--no-upload" ]; then ENV="dev"; fi
fi

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
