#!/bin/bash
# Deploy all Edge Functions to the linked Supabase project.
# Usage:
#   cd Supabase
#   ./deploy.sh          # Deploy to currently linked project
#   ./deploy.sh prod     # Link to prod first, then deploy
#   ./deploy.sh dev      # Link to dev first, then deploy

set -e

# Project refs — set these
DEV_REF="qbgrghcpvmsnoyzmglgv"
PROD_REF="negfbluxywxsadggnwwd"

# Link to a specific environment if specified
if [ "$1" = "prod" ]; then
    echo "🔗 Linking to PROD ($PROD_REF)..."
    supabase link --project-ref "$PROD_REF"
elif [ "$1" = "dev" ]; then
    echo "🔗 Linking to DEV ($DEV_REF)..."
    supabase link --project-ref "$DEV_REF"
fi

FUNCTIONS=(
    "submit-score"
    "get-leaderboard"
    "get-player-rank"
    "update-display-name"
    "get-player-profile"
    "register-player"
    "sync-streak"
)

echo "🚀 Deploying ${#FUNCTIONS[@]} functions..."
echo ""

for fn in "${FUNCTIONS[@]}"; do
    echo "  Deploying $fn..."
    supabase functions deploy "$fn" --no-verify-jwt
done

echo ""
echo "✅ All functions deployed."
