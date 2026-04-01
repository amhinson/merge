#!/bin/bash
# Build WebGL dev and serve locally.
# Usage:
#   ./serve-webgl.sh           # build + serve
#   ./serve-webgl.sh --skip-build  # serve existing build

set -e

PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$PROJECT_PATH/Build/WebGL/murge-dev"
PORT=8080
URL="http://localhost:$PORT"

# ===== Build =====

if [ "$1" != "--skip-build" ]; then
    "$PROJECT_PATH/build-webgl.sh" dev
fi

if [ ! -d "$BUILD_DIR" ]; then
    echo "No build found at $BUILD_DIR"
    exit 1
fi

# ===== Kill existing server on this port =====

lsof -ti :$PORT | xargs kill -9 2>/dev/null || true

# ===== Open browser (only if not already open) =====

# Check if any browser tab already has localhost:8080 open (macOS)
if ! osascript -e "
    tell application \"System Events\"
        set browserOpen to false
        if exists process \"Google Chrome\" then
            tell application \"Google Chrome\"
                repeat with w in windows
                    repeat with t in tabs of w
                        if URL of t contains \"localhost:$PORT\" then
                            set browserOpen to true
                            reload t
                        end if
                    end repeat
                end repeat
            end tell
        end if
        return browserOpen
    end tell
" 2>/dev/null | grep -q "true"; then
    # Not found in Chrome, try Safari, otherwise just open
    open "$URL"
fi

# ===== Serve =====

echo ""
echo "Serving at $URL"
echo "Press Ctrl+C to stop"
echo ""
cd "$BUILD_DIR"
python3 -m http.server $PORT
