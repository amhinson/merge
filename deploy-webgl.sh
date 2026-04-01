#!/bin/bash
# Build WebGL prod and deploy to itch.io.
# Usage:
#   ./deploy-webgl.sh              # build + deploy
#   ./deploy-webgl.sh --skip-build # deploy existing build
#
# Prerequisites:
#   - butler installed: brew install itchio/tools/butler
#   - butler logged in: butler login (one-time)

set -e

PROJECT_PATH="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$PROJECT_PATH/Build/WebGL/murge-prod"
ITCH_TARGET="bavid-dowie/murge:web"

# ===== Check butler =====

# Pick up BUTLER_API_KEY from shell profile if not already set
if [ -z "$BUTLER_API_KEY" ] && [ -f "$HOME/.zshrc" ]; then
    BUTLER_API_KEY="$(grep 'BUTLER_API_KEY=' "$HOME/.zshrc" | tail -1 | sed "s/.*BUTLER_API_KEY=//" | tr -d "'" | tr -d '"')"
    export BUTLER_API_KEY
fi

BUTLER="$(command -v butler 2>/dev/null || echo "$HOME/bin/butler")"
if [ ! -x "$BUTLER" ]; then
    echo "butler not found. Install from https://itch.io/docs/butler/"
    echo "Then run: butler login"
    exit 1
fi

# ===== Build =====

if [ "$1" != "--skip-build" ]; then
    "$PROJECT_PATH/build-webgl.sh" prod
fi

if [ ! -d "$BUILD_DIR" ]; then
    echo "No build found at $BUILD_DIR"
    exit 1
fi

# ===== Deploy =====

echo ""
echo "========================================"
echo "  Deploying to itch.io"
echo "  Target: $ITCH_TARGET"
echo "========================================"
echo ""

"$BUTLER" push "$BUILD_DIR" "$ITCH_TARGET"

echo ""
echo "  Deployed: https://bavid-dowie.itch.io/murge"
echo "========================================"
