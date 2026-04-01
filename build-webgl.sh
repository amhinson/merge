#!/bin/bash
# Build WebGL.
# Usage:
#   ./build-webgl.sh dev      # Development build (uncompressed, faster)
#   ./build-webgl.sh prod     # Production build (Brotli compressed)
#
# Build number auto-increments from .build-number file.
# Override with: BUILD_NUMBER=42 ./build-webgl.sh prod
#
# Prerequisites:
#   - Unity installed with WebGL Build Support module

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

# ===== Main =====

ENV="${1:-dev}"
BUILD_NUM=$(get_next_build_number)
START_TIME=$(date +%s)

if [ ! -f "$UNITY_PATH" ]; then
    echo "Unity not found at $UNITY_PATH"
    echo "Update UNITY_PATH in this script to match your Unity installation."
    exit 1
fi

echo "========================================"
echo "  WebGL $ENV — build #$BUILD_NUM"
echo "========================================"

BUILD_OPTIONS="-buildTarget WebGL"
if [ "$ENV" = "dev" ]; then
    BUILD_OPTIONS="$BUILD_OPTIONS -development"
fi

echo ""
echo "Building with Unity..."
"$UNITY_PATH" \
    -batchmode \
    -nographics \
    -quit \
    -projectPath "$PROJECT_PATH" \
    $BUILD_OPTIONS \
    -executeMethod MergeGame.Editor.BuildScript.BuildWebGL \
    -buildNumber "$BUILD_NUM" \
    -logFile "$PROJECT_PATH/Build/unity-build-webgl-${ENV}.log" \
    || { echo "Unity build failed ($ENV). Check Build/unity-build-webgl-${ENV}.log"; exit 1; }

OUTPUT_DIR="$PROJECT_PATH/Build/WebGL/murge-${ENV}"
if [ -d "$OUTPUT_DIR" ]; then
    echo ""
    echo "  Build: $OUTPUT_DIR"
    echo "  Size:  $(du -sh "$OUTPUT_DIR" | cut -f1)"
    echo ""
    echo "  To test locally:"
    echo "    cd $OUTPUT_DIR && python3 -m http.server 8080"
    echo "    Open http://localhost:8080"
fi

echo "========================================"
echo ""

save_build_number "$BUILD_NUM"

END_TIME=$(date +%s)
ELAPSED=$(( END_TIME - START_TIME ))
echo "Done in $(( ELAPSED / 60 ))m $(( ELAPSED % 60 ))s — build #$BUILD_NUM"
