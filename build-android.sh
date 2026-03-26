#!/bin/bash
# Build Android APK (dev) or AAB (prod).
# Usage:
#   ./build-android.sh dev      # Development APK
#   ./build-android.sh prod     # Release AAB (for Google Play)
#   ./build-android.sh prod-apk # Release APK (for sideloading/testing)
#   ./build-android.sh all      # Both dev + prod AAB
#
# Build number auto-increments from .build-number file.
# Override with: BUILD_NUMBER=42 ./build-android.sh prod
#
# For prod builds, set keystore passwords:
#   export MURGE_KEYSTORE_PASS=yourpass
#   export MURGE_KEY_ALIAS_PASS=yourpass
#
# Prerequisites:
#   - Unity installed with Android Build Support module
#   - Android SDK (installed via Unity Hub)
#   - For prod: keystore file at ./murge.keystore (run ./setup-keystore.sh to create)

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
    local format="${3:-auto}" # auto, apk, or aab

    # Determine output format
    local ext="apk"
    if [ "$format" = "aab" ] || { [ "$format" = "auto" ] && [ "$env" = "prod" ]; }; then
        ext="aab"
    fi

    echo "========================================"
    echo "  Android $env — build #$build_num ($ext)"
    echo "========================================"

    if [ ! -f "$UNITY_PATH" ]; then
        echo "Unity not found at $UNITY_PATH"
        exit 1
    fi

    # Check keystore for prod
    if [ "$env" = "prod" ] && [ ! -f "$PROJECT_PATH/murge.keystore" ]; then
        echo "Keystore not found. Run ./setup-keystore.sh first."
        exit 1
    fi

    local BUILD_OPTIONS="-buildTarget Android"
    if [ "$env" = "dev" ]; then
        BUILD_OPTIONS="$BUILD_OPTIONS -development"
    fi
    if [ "$ext" = "apk" ]; then
        BUILD_OPTIONS="$BUILD_OPTIONS -forceApk"
    fi

    echo ""
    echo "Building with Unity..."
    "$UNITY_PATH" \
        -batchmode \
        -nographics \
        -quit \
        -projectPath "$PROJECT_PATH" \
        $BUILD_OPTIONS \
        -executeMethod BuildScript.BuildAndroid \
        -buildNumber "$build_num" \
        -logFile "$PROJECT_PATH/Build/unity-build-android-${env}.log" \
        || { echo "Unity build failed ($env). Check Build/unity-build-android-${env}.log"; exit 1; }

    local OUTPUT="$PROJECT_PATH/Build/Android/murge-${env}.${ext}"
    if [ -f "$OUTPUT" ]; then
        echo ""
        echo "  Build: $OUTPUT"
        echo "  Size:  $(du -h "$OUTPUT" | cut -f1)"
    fi

    echo "========================================"
    echo ""
}

# ===== Main =====

ENV="${1:-dev}"
BUILD_NUM=$(get_next_build_number)
START_TIME=$(date +%s)

if [ "$ENV" = "all" ]; then
    build_env "dev" "$BUILD_NUM" "apk"
    build_env "prod" "$BUILD_NUM" "aab"
elif [ "$ENV" = "prod-apk" ]; then
    build_env "prod" "$BUILD_NUM" "apk"
else
    build_env "$ENV" "$BUILD_NUM" "auto"
fi

save_build_number "$BUILD_NUM"

END_TIME=$(date +%s)
ELAPSED=$(( END_TIME - START_TIME ))
echo "Done in $(( ELAPSED / 60 ))m $(( ELAPSED % 60 ))s — build #$BUILD_NUM"
