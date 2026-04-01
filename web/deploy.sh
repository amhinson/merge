#!/bin/bash
# Deploy the Murge website to Netlify.
# Usage:
#   ./deploy.sh          # Production deploy
#   ./deploy.sh preview  # Draft/preview deploy (unique URL, not live)

set -e

cd "$(dirname "$0")"

# Copy the latest WebGL prod build into web/play/ for hosting on murgegame.com
WEBGL_BUILD="../Build/WebGL/murge-prod"
if [ -d "$WEBGL_BUILD" ]; then
    echo "Copying WebGL build to play/..."
    rm -rf play
    cp -r "$WEBGL_BUILD" play
    echo "Done — $(du -sh play | cut -f1)"
else
    echo "Warning: No WebGL prod build found at $WEBGL_BUILD"
    echo "Run ./build-webgl.sh prod first, or the /play page won't be updated."
fi

if [ "$1" = "preview" ]; then
    echo "Deploying preview..."
    netlify deploy --dir .
else
    echo "Deploying to production..."
    netlify deploy --prod --dir .
fi
