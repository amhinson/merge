#!/bin/bash
# Deploy the Overtone website to Netlify.
# Usage:
#   ./deploy.sh          # Production deploy
#   ./deploy.sh preview  # Draft/preview deploy (unique URL, not live)

set -e

cd "$(dirname "$0")"

if [ "$1" = "preview" ]; then
    echo "Deploying preview..."
    netlify deploy --dir .
else
    echo "Deploying to production..."
    netlify deploy --prod --dir .
fi
